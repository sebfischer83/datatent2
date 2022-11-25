// # SPDX-License-Identifier: MIT
// # Copyright 2021
// # Sebastian Fischer sebfischer@gmx.net

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Collections.Pooled;
using Datatent2.Contracts;
using Datatent2.Contracts.Exceptions;
using Datatent2.Core.Block;
using Datatent2.Core.Memory;
using Datatent2.Core.Page;
using Datatent2.Core.Page.AllocationInformation;
using Datatent2.Core.Page.Data;
using Datatent2.Core.Page.GlobalAllocationMap;
using Datatent2.Core.Page.Header;
using Datatent2.Core.Page.Index;
using Datatent2.Core.Page.Overflow;
using Datatent2.Core.Page.Table;
using Datatent2.Core.Services.Cache;
using Datatent2.Core.Services.Disk;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Datatent2.Core.Services.Page
{
    internal class PageService : IPageService
    {
        private readonly DatatentSettings _datatentSettings;
        private readonly DiskService _diskService;
        private readonly ILogger _logger;
        private readonly CacheService _cacheService;
        private HeaderPage? _headerPage;
        private GlobalAllocationMapPage? _globalAllocationMap;
        private AllocationInformationPage? _allocationInformationPage;
        private readonly SemaphoreSlim _semaphoreSlim;
        private readonly Task _backgroundFlushTask;

        public static async Task<PageService> Create(DatatentSettings datatentSettings, DiskService diskService, CacheService cacheService, ILogger logger)
        {
            var service = new PageService(datatentSettings, diskService, cacheService, logger);
            await service.Init().ConfigureAwait(false);
            return service;
        }

        public PageService(DatatentSettings datatentSettings, DiskService diskService, CacheService cacheService, ILogger logger)
        {
            _backgroundFlushTask = Task.Factory.StartNew(FlushBackgroundTaskMethodAsync, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
            _semaphoreSlim = new SemaphoreSlim(1, 1);
            _datatentSettings = datatentSettings;
            _diskService = diskService;
            _logger = logger;
            _cacheService = cacheService;
        }

        private async Task FlushBackgroundTaskMethodAsync()
        {
            while (true)
            {
                await Task.Delay(5000).ConfigureAwait(false);
                foreach (var item in _cacheService)
                {
                    if (item.Transaction == null && item.IsDirty)
                    {
                        await _diskService.WriteBuffer(new WriteRequest(item.PageBuffer, item.Id)).ConfigureAwait(false);
                        item.IsDirty = false;
                    }
                }
            }
        }

        public async Task RefreshSettingsFromHeader(DatatentSettings datatentSettings)
        {
            if (_headerPage == null)
                throw new InvalidEngineStateException($"{nameof(_headerPage)} can't be null!");

            var content = await LoadHeaderContent();

            datatentSettings.Plugins.CompressionAlgorithm = content.CompressionAlgo;

        }

        private async Task<HeaderContent> LoadHeaderContent()
        {
            if (_headerPage == null)
                throw new InvalidEngineStateException($"{nameof(_headerPage)} can't be null!");

            // buffer hat immer ein HeaderBlock als Inhalt
            using (PooledList<byte> bytes = new(Constants.PAGE_SIZE, ClearMode.Never))
            {
                BasePage? page;
                PageAddress address = new PageAddress(_headerPage.Id, 1);

                page = _headerPage;
                HeaderBlock headerBlock = new HeaderBlock(_headerPage, 1);
                bytes.AddRange(headerBlock.GetData());
                BlockHeader blockHeader = headerBlock.Header;

                while (!blockHeader.NextBlockAddress.IsEmpty())
                {
                    var overflowPage = await GetPage<OverflowPage>(blockHeader.NextBlockAddress);
                    if (overflowPage == null)
                    {
                        throw new InvalidPageException("GET", address.PageId);
                    }

                    OverflowBlock overflowBlock = new OverflowBlock(overflowPage, blockHeader.NextBlockAddress.SlotId);
                    bytes.AddRange(overflowBlock.GetData());

                    blockHeader = overflowBlock.Header;
                }

                return Utf8Json.JsonSerializer.Deserialize<HeaderContent>(bytes.ToArray());
            }
        }

        public async Task UpdateHeaderFromSettings(DatatentSettings datatentSettings)
        {
            if (_headerPage == null)
                throw new InvalidEngineStateException($"{nameof(_headerPage)} can't be null!");

            var content = await LoadHeaderContent();

            content.EncryptionAlgo = datatentSettings.Plugins.CompressionAlgorithm;


        }

        public async Task SaveHeaderContentAsync(HeaderContent headerContent)
        {
            if (_headerPage == null)
                throw new InvalidEngineStateException($"{nameof(_headerPage)} can't be null!");

            // get all addresses from the header and delete the old data
            BasePage? page;
            PageAddress address = new PageAddress(_headerPage.Id, 1);

            page = _headerPage;
            HeaderBlock headerBlock = new HeaderBlock(_headerPage, 1);
            bytes.AddRange(headerBlock.GetData());
            BlockHeader blockHeader = headerBlock.Header;
            page.Delete(1);

            while (!blockHeader.NextBlockAddress.IsEmpty())
            {
                var overflowPage = await GetPage<OverflowPage>(blockHeader.NextBlockAddress);
                if (overflowPage == null)
                {
                    throw new InvalidPageException("GET", address.PageId);
                }

                OverflowBlock overflowBlock = new OverflowBlock(overflowPage, blockHeader.NextBlockAddress.SlotId);
                overflowPage.Delete(overflowBlock.Position.SlotId);
                await UpdatePageStatistics(overflowPage).ConfigureAwait(false);

                blockHeader = overflowBlock.Header;
            }

            // now save the new data

        }

        /// <summary>
        /// Creates needed pages if this is a new database or load the needed pages from disk
        /// </summary>
        private async Task Init()
        {
            using var scope = _logger.BeginScope($"Init {nameof(PageService)}");
            // create the first GAM page => only when new database
            var firstGamBuffer = await _diskService.GetBuffer(new ReadRequest(1)).ConfigureAwait(false);
            var gamHeader = PageHeader.FromBuffer(firstGamBuffer.BufferSegment.Span);
            _logger.LogDebug($"First GAM has id of {gamHeader.PageId}");

            if (gamHeader.PageId == 0)
            {
                _logger.LogInformation($"New database, create first GAM and AIM page");

                // new database
                var headerPageBuffer = await _diskService.GetBuffer(new ReadRequest(0)).ConfigureAwait(false);
                _headerPage = HeaderPage.CreateHeaderPage(headerPageBuffer.BufferSegment);
                _cacheService.Add(_headerPage);
                await UpdateHeaderFromSettings(_datatentSettings);

                _globalAllocationMap = new GlobalAllocationMapPage(firstGamBuffer.BufferSegment, 1);
                _cacheService.Add(_globalAllocationMap);

                var nextId = _globalAllocationMap.AcquirePageId();
                var newInfo = await _diskService.GetBuffer(new ReadRequest(nextId)).ConfigureAwait(false);
                _allocationInformationPage = new AllocationInformationPage(newInfo.BufferSegment, nextId);
                _cacheService.Add(_allocationInformationPage);
            }
            else
            {
                _logger.LogInformation($"Existing database found");
                _headerPage = HeaderPage.CreateHeaderPage((await _diskService.GetBuffer(new ReadRequest(0))).BufferSegment);
                // read header data
                await RefreshSettingsFromHeader(_datatentSettings);

                _cacheService.Add(_headerPage);

                // existing database, search last GAM page in database
                while (gamHeader.NextPageId != uint.MaxValue)
                {
                    var res = await _diskService.GetBuffer(new ReadRequest(gamHeader.NextPageId)).ConfigureAwait(false);
                    gamHeader = PageHeader.FromBuffer(res.BufferSegment.Span);
                }
                _logger.LogDebug($"Last GAM found at id {gamHeader.PageId}");
                var gamBuffer = await _diskService.GetBuffer(new ReadRequest(gamHeader.PageId)).ConfigureAwait(false);
                _globalAllocationMap = new GlobalAllocationMapPage(gamBuffer.BufferSegment);
                _cacheService.Add(_globalAllocationMap);

                // get all possible indexes for the aim pages
                var possibleIndexes =
                    AllocationInformationPage.GetAllAllocationInformationPageIdsForGam(_globalAllocationMap.Id);

                PageHeader aimPageHeader;
                ReadResponse aimReadResponse;
                // search the last used aim page
                uint aimPageId = possibleIndexes[0];
                do
                {
                    aimReadResponse = await _diskService.GetBuffer(new ReadRequest(aimPageId)).ConfigureAwait(false);
                    aimPageHeader = PageHeader.FromBuffer(aimReadResponse.BufferSegment.Span, 0);
                    aimPageId = aimPageHeader.NextPageId;
                } while (aimPageHeader.NextPageId != uint.MaxValue);
                _allocationInformationPage = new AllocationInformationPage(aimReadResponse.BufferSegment);
                _logger.LogDebug($"Last AIM found at id {_allocationInformationPage.Id}");
                _cacheService.Add(_allocationInformationPage);
            }
        }

        public void SetHeaderPageData(HeaderPageData headerPageData)
        {
            var bytes = Utf8Json.JsonSerializer.Serialize(headerPageData, Utf8Json.Resolvers.StandardResolver.AllowPrivate);


        }

        public async ValueTask<T?> GetPage<T>(PageAddress address) where T : BasePage
        {
            return await GetPage<T>(address.PageId).ConfigureAwait(false);
        }

        public async ValueTask<T?> GetPage<T>(uint id) where T : BasePage
        {
            return await GetFromCacheOrRead<T>(id).ConfigureAwait(false);
        }

        /// <summary>
        /// Save all dirty pages to the underlying disk and clears all cached pages
        /// </summary>
        /// <returns></returns>
        public async Task CheckPoint()
        {
            var pages = _cacheService.ToList();
            foreach (var page in pages)
            {
                if (page.IsDirty)
                    await WritePage(page).ConfigureAwait(false);
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
#if DEBUG
            _logger.LogDebug($"{nameof(WritePage)} for id {page.Id} with type {page.Type.ToString()}");
#endif

            if (!page.IsDirty)
                return;

            page.SaveHeader();
            await _diskService.WriteBuffer(new WriteRequest(page.PageBuffer, page.Id)).ConfigureAwait(false);
            page.IsDirty = false;
        }

        public async Task UpdatePageStatistics(BasePage page)
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

        public async Task<TablePage> GetTablePageForTable(string name)
        {
            await _semaphoreSlim.WaitAsync().ConfigureAwait(false);
            try
            {
                List<uint> searchedIds = new();
                // search cached ones 
                foreach (var tablePage in _cacheService.GetAllPagesOfType<TablePage>(PageType.Table))
                {
                    searchedIds.Add(tablePage.Id);
                    if (tablePage.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase))
                        return tablePage;
                }

                // search all tablePages
                // search from current GAM to the previous and all aims if there is a table page with data for this table
                var gam = _globalAllocationMap!;
                do
                {
                    var possibleAimPages = AllocationInformationPage.GetAllAllocationInformationPageIdsForGam(gam.Id);

                    for (int i = 0; i < possibleAimPages.Length; i++)
                    {
                        var aimId = possibleAimPages[i];
                        var allocationInformationPage = await GetFromCacheOrRead<AllocationInformationPage>(aimId, false).ConfigureAwait(false);
                        if (allocationInformationPage == null)
                            continue;
                        var possibleTablePages = allocationInformationPage.FindPagesOfType(PageType.Table);
                        for (int t = 0; t < possibleTablePages.Length; t++)
                        {
                            var tableId = possibleTablePages[i];
                            var tablePage = await GetFromCacheOrRead<TablePage>(tableId, false).ConfigureAwait(false);
                            if (tablePage != null && tablePage.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase))
                            {
                                return tablePage;
                            }
                        }
                    }

                } while (gam.PageHeader.PrevPageId != UInt32.MaxValue);

                // not found we need a new one
                var newTablePage = await CreateNewPage<TablePage>(name).ConfigureAwait(false);

                return newTablePage;
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }

        private async ValueTask<T?> GetFromCacheOrRead<T>(uint pageId, bool throwIfNotExists = true) where T : BasePage
        {
            if (_cacheService.HasPage(pageId))
                return (T)(object)_cacheService.Get<T>(pageId)!;

            var response = await _diskService.GetBuffer(new ReadRequest(pageId)).ConfigureAwait(false);
            var page = BasePage.Create<T>(response.BufferSegment);
            if ((page == null || page.PageHeader.Type == PageType.Undefined) && throwIfNotExists)
            {
                throw new PageNotFoundException($"The requested page {pageId} not exists in the current database!", pageId);
            }
            if (page != null)
                _cacheService.Add(page);

            return page;
        }

        /// <summary>
        /// Search an existing data page with free space or creates a new one
        /// </summary>
        /// <returns></returns>
        public async ValueTask<DataPage> GetDataPageWithFreeSpace()
        {
            await _semaphoreSlim.WaitAsync().ConfigureAwait(false);
            try
            {
                var freePage = _cacheService.GetDataPage();
                if (freePage != null && freePage.FillFactor < PageFillFactor.NinetyFiveToNinetyNine)
                    return freePage;

                var freePageId = _allocationInformationPage!.FindPageWithFreeSpace(PageType.Data, PageFillFactor.SeventyToNinetyFive);

                if (freePageId == -1)
                {
                    var page = await CreateNewPage<DataPage>().ConfigureAwait(false);
                    return page;
                }

                var cachedPage = _cacheService.Get<DataPage>((uint)freePageId);
                if (cachedPage != null)
                    return cachedPage;

                var ioResponse = await _diskService.GetBuffer(new ReadRequest((uint)freePageId)).ConfigureAwait(false);
                var diskPage = new DataPage(ioResponse.BufferSegment);
                _cacheService.Add(diskPage);
                return diskPage;
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }

        public async Task<T> CreateNewPage<T>(string? strParam = null) where T : BasePage
        {
            if (_globalAllocationMap!.IsFull)
            {
                await AllocateNewGam().ConfigureAwait(false);
            }

            var nextId = _globalAllocationMap.AcquirePageId();
#if DEBUG
            _logger.LogInformation($"Create new page {typeof(T).Name} at {nextId}");
#endif
            if (_allocationInformationPage!.IsFull)
            {
#if DEBUG
                _logger.LogInformation($"AIM {_allocationInformationPage.Id} is full create new one {nextId}");
#endif
                _allocationInformationPage.SetNextPage(nextId);
                var prevId = _allocationInformationPage.Id;
                await _diskService.WriteBuffer(new WriteRequest(_allocationInformationPage.PageBuffer,
                    _allocationInformationPage.Id)).ConfigureAwait(false);
                _allocationInformationPage = new AllocationInformationPage(BufferPoolFactory.Get().Rent(), nextId);
                _allocationInformationPage.SetPreviousPage(prevId);
                _cacheService.Add(_allocationInformationPage);

                nextId = _globalAllocationMap.AcquirePageId();
                _logger.LogInformation($"Switch id for new page {typeof(T).Name} to {nextId}");
            }

            if (typeof(T) == typeof(DataPage))
            {
                var page = (T)(object)new DataPage(BufferPoolFactory.Get().Rent(), nextId);
                _allocationInformationPage.AddAllocationInformation(page);
                _cacheService.Add(page);

                return page;
            }
            if (typeof(T) == typeof(TablePage))
            {
                var page = (T)(object)new TablePage(BufferPoolFactory.Get().Rent(), nextId, strParam!);
                _allocationInformationPage.AddAllocationInformation(page);
                _cacheService.Add(page);

                return page;
            }
            if (typeof(T) == typeof(IndexPage))
            {
                var page = (T)(object)new IndexPage(BufferPoolFactory.Get().Rent(), nextId);
                _allocationInformationPage.AddAllocationInformation(page);
                _cacheService.Add(page);

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

            var newInfo = await _diskService.GetBuffer(new ReadRequest(nextId)).ConfigureAwait(false);
            _allocationInformationPage = new AllocationInformationPage(newInfo.BufferSegment, nextId);
            _allocationInformationPage.SetPreviousPage(prevId);
            _cacheService.Add(_allocationInformationPage);
        }
    }
}
