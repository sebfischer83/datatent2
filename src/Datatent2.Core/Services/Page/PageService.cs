// # SPDX-License-Identifier: MIT
// # Copyright 2021
// # Sebastian Fischer sebfischer@gmx.net

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Datatent2.Core.Memory;
using Datatent2.Core.Page;
using Datatent2.Core.Page.AllocationInformation;
using Datatent2.Core.Page.GlobalAllocationMap;
using Datatent2.Core.Services.Cache;
using Datatent2.Core.Services.Disk;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Datatent2.Core.Services.Page
{
    internal class PageService
    {
        private readonly DiskService _diskService;
        private readonly ILogger _logger;
        private readonly CacheService _cacheService;
        private GlobalAllocationMapPage? _globalAllocationMap;
        private AllocationInformationPage? _allocationInformationPage;
        private readonly SemaphoreSlim _semaphoreSlim;

        public static async Task<PageService> Create(DiskService diskService, ILogger logger)
        {
            var service = new PageService(diskService, logger);
            await service.Init();
            return service;
        }

        public PageService(DiskService diskService, ILogger logger)
        {
            _semaphoreSlim = new SemaphoreSlim(1, 1);
            _diskService = diskService;
            _logger = logger;
            _cacheService = new CacheService();
        }

        private async Task Init()
        {
            // create the first GAM page => only when new database
            var firstGamBuffer = await _diskService.GetBuffer(new ReadRequest(1));
            var header = PageHeader.FromBuffer(firstGamBuffer.BufferSegment.Span);
            _logger.LogInformation($"Init {nameof(PageService)} ");

            if (header.PageId == 0)
            {
                // new database
                _globalAllocationMap = new GlobalAllocationMapPage(firstGamBuffer.BufferSegment, 1);
                _cacheService.Add(_globalAllocationMap);

                var nextId = _globalAllocationMap.AcquirePageId();
                var newInfo = await _diskService.GetBuffer(new ReadRequest(nextId));
                _allocationInformationPage = new AllocationInformationPage(newInfo.BufferSegment, nextId);
                _cacheService.Add(_allocationInformationPage);

            }
            else
            {
                // existing database, search last GAM page in database
                while (header.NextPageId != uint.MaxValue)
                {
                    var res = await _diskService.GetBuffer(new ReadRequest(header.NextPageId));
                    header = PageHeader.FromBuffer(res.BufferSegment.Span);
                }
                var gamBuffer = await _diskService.GetBuffer(new ReadRequest(header.PageId));
                _globalAllocationMap = new GlobalAllocationMapPage(gamBuffer.BufferSegment);
                _cacheService.Add(_globalAllocationMap);

                
                // get all possible indexes for the aim pages
                var possibleIndexes =
                    AllocationInformationPage.GetAllAllocationInformationPageIdsForGam(_globalAllocationMap.Id);

                PageHeader aimPageHeader;
                ReadResponse aimReadResponse;
                uint aimPageId = possibleIndexes[0];
                do
                {
                    aimReadResponse = await _diskService.GetBuffer(new ReadRequest(aimPageId));
                    aimPageHeader = PageHeader.FromBuffer(aimReadResponse.BufferSegment.Span, 0);
                    aimPageId = aimPageHeader.NextPageId;
                } while (aimPageHeader.NextPageId != uint.MaxValue);

                _allocationInformationPage = new AllocationInformationPage(aimReadResponse.BufferSegment);
                _cacheService.Add(_allocationInformationPage);
            }
        }

        public async Task<T?> GetPage<T>(uint id) where T : BasePage
        {
            if (_cacheService.HasPage(id))
            {
                return _cacheService.Get<T>(id);
            }

            var response = await _diskService.GetBuffer(new ReadRequest(id));

            var page = BasePage.Create<T>(response.BufferSegment);
            if (page != null)
            {
                _cacheService.Add(page);
                return page;
            }

            return null;
        }

        /// <summary>
        /// Save all dirty pages to the underlying disk and clears all cached pages
        /// </summary>
        /// <returns></returns>
        public async Task CheckPoint()
        {
            foreach (var page in _cacheService)
            {
                if (page.IsDirty)
                    await WritePage(page);
            }
            await _diskService.WriteBuffer(new WriteRequest(_globalAllocationMap!.PageBuffer, _globalAllocationMap.Id)).ConfigureAwait(false);
            await _diskService.WriteBuffer(new WriteRequest(_allocationInformationPage!.PageBuffer, _allocationInformationPage.Id)).ConfigureAwait(false);
            _cacheService.Clear();
        }

        /// <summary>
        /// Write a page to the disk
        /// </summary>
        /// <param name="page"></param>
        /// <returns></returns>
        public async Task WritePage(BasePage page)
        {
            if (!page.IsDirty)
                return;

            page.SaveHeader();
            await _diskService.WriteBuffer(new WriteRequest(page.PageBuffer, page.Id));
            page.IsDirty = false;
        }

        public async Task UpdatePageStatisticsAsync(BasePage page)
        {
            var gam = (uint)Math.DivRem(page.Id, GlobalAllocationMapPage.PAGES_PER_GAM, out var posInGam) + 1;
            var aims = AllocationInformationPage.GetAllAllocationInformationPageIdsForGam(gam);

            var val = 0u;
            int count = 1;
            do
            {
                val = aims[count];
                if (val > page.Id)
                {
                    val = (uint)(count - 1);
                    break;
                }
                count++;
            } while (count < aims.Length);

            if (gam == _globalAllocationMap!.Id && aims[val] == _allocationInformationPage!.Id)
            {
                _allocationInformationPage!.UpdateAllocationInformation(page);
            }
            else
            {
                var alloc = _cacheService.Get<AllocationInformationPage>(aims[val]);
                if (alloc == null)
                {
                    var buffer = await _diskService.GetBuffer(new ReadRequest(aims[count])).ConfigureAwait(false);
                    alloc = new AllocationInformationPage(buffer.BufferSegment);
                    _cacheService.Add(alloc);
                }
                alloc.UpdateAllocationInformation(page);
            }
        }

        /// <summary>
        /// Search an existing data page with free space or creates a new one
        /// </summary>
        /// <returns></returns>
        public async ValueTask<DataPage> GetDataPageWithFreeSpace()
        {
            await _semaphoreSlim.WaitAsync();
            try
            {
                var freePage = _cacheService.GetDataPage();
                if (freePage != null && freePage.FillFactor < PageFillFactor.NinetyFiveToNinetyNine)
                    return freePage;

                var freePageId = _allocationInformationPage!.FindPageWithFreeSpace(PageType.Data, PageFillFactor.SeventyToNinetyFive);

                if (freePageId == -1)
                {
                    var page = await CreateNewPageAsync<DataPage>();
                    _cacheService.Add(page);
                    return page;
                }

                var cachedPage = _cacheService.Get<DataPage>((uint)freePageId);
                if (cachedPage != null)
                    return cachedPage;

                var ioResponse = await _diskService.GetBuffer(new ReadRequest((uint)freePageId));
                var diskPage = new DataPage(ioResponse.BufferSegment, ioResponse.PageId);
                _cacheService.Add(diskPage);
                return diskPage;
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }

        private async Task<T> CreateNewPageAsync<T>() where T : BasePage
        {
            if (_globalAllocationMap!.IsFull)
            {
                await AllocateNewGam().ConfigureAwait(false);
            }

            var nextId = _globalAllocationMap.AcquirePageId();
            if (_allocationInformationPage!.IsFull)
            {
#if DEBUG
                _logger.LogInformation($"AIM {_allocationInformationPage.Id} is full create new one {nextId}");
#endif
                _allocationInformationPage.SetNextPage(nextId);
                var prevId = _allocationInformationPage.Id;
                await _diskService.WriteBuffer(new WriteRequest(_allocationInformationPage.PageBuffer,
                    _allocationInformationPage.Id));
                _allocationInformationPage = new AllocationInformationPage(BufferPoolFactory.Get().Rent(), nextId);
                _allocationInformationPage.SetPreviousPage(prevId);
                _cacheService.Add(_allocationInformationPage);

                nextId = _globalAllocationMap.AcquirePageId();
            }

            if (typeof(T) == typeof(DataPage))
            {
                var page = (T)(object)new DataPage(BufferPoolFactory.Get().Rent(), nextId);
                _allocationInformationPage.AddAllocationInformation(page);

                return page;
            }

            throw new NotSupportedException(nameof(T));
        }

        private async Task AllocateNewGam()
        {
            var nextGamId = _globalAllocationMap!.Id + GlobalAllocationMapPage.PAGES_PER_GAM + 1;
#if DEBUG
            _logger.LogInformation($"GAM {_globalAllocationMap!.Id} is full, create new one {nextGamId}");
#endif
            _globalAllocationMap.SetNextPage(nextGamId);
            // save the current pages to disk
            await _diskService.WriteBuffer(new WriteRequest(_globalAllocationMap!.PageBuffer, _globalAllocationMap.Id)).ConfigureAwait(false);
            var prevId = _globalAllocationMap.Id;
            var gamBuffer = BufferPoolFactory.Get().Rent();
            _globalAllocationMap = new GlobalAllocationMapPage(gamBuffer, nextGamId);
            _globalAllocationMap.SetPreviousPage(prevId);

            _cacheService.Add(_globalAllocationMap);

            var nextId = _globalAllocationMap.AcquirePageId();

            _allocationInformationPage!.SetNextPage(nextId);
            prevId = _allocationInformationPage.Id;
            await _diskService.WriteBuffer(new WriteRequest(_allocationInformationPage!.PageBuffer, _allocationInformationPage.Id)).ConfigureAwait(false);

            var newInfo = await _diskService.GetBuffer(new ReadRequest(nextId));
            _allocationInformationPage = new AllocationInformationPage(newInfo.BufferSegment, nextId);
            _allocationInformationPage.SetPreviousPage(prevId);
            _cacheService.Add(_allocationInformationPage);
        }
    }
}
