using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        private readonly CacheService _cacheService;
        private GlobalAllocationMapPage? _globalAllocationMap;
        private AllocationInformationPage? _allocationInformationPage;

        public PageService(DiskService diskService)
        {
            _diskService = diskService;
            _cacheService = new CacheService();
        }

        public async Task Init()
        {
            // create the first GAM page => only when new database
            var firstGamBuffer = await _diskService.GetBuffer(new ReadRequest(1));
            var header = PageHeader.FromBuffer(firstGamBuffer.BufferSegment.Span);
            if (header.PageId == 0)
            {
                // new database
                _globalAllocationMap = new GlobalAllocationMapPage(firstGamBuffer.BufferSegment, 1);

                var nextId = _globalAllocationMap.AcquirePageId();
                var newInfo = await _diskService.GetBuffer(new ReadRequest(nextId));
                _allocationInformationPage = new AllocationInformationPage(newInfo.BufferSegment, nextId);
            }
            else
            {
                // existing database, search last GAM page in database
                if (header.NextPageId == 0)
                    _globalAllocationMap = new GlobalAllocationMapPage(firstGamBuffer.BufferSegment, 1);
                else
                {
                    while (header.NextPageId > 0)
                    {
                        var res = await _diskService.GetBuffer(new ReadRequest(header.NextPageId));
                        header = PageHeader.FromBuffer(res.BufferSegment.Span);
                    }
                }
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
                await WritePage(page);
            }
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

            await _diskService.WriteBuffer(new WriteRequest(page.PageBuffer, page.Id));
            //HeaderPage.Instance.SetHighestPageId(page.Id);
            page.IsDirty = false;
        }

        /// <summary>
        /// Search an existing data page with free space or creates a new one
        /// </summary>
        /// <returns></returns>
        public async ValueTask<DataPage> GetDataPageWithFreeSpace()
        {
            var freePage = _cacheService.GetDataPage();
            if (freePage != null)
                return freePage;

            //for (uint i = 1; i <= HeaderPage.Instance.HighestPageId; i++)
            //{
            //    if (_cacheService.HasPage(i))
            //        continue;

            //    var response = await _diskService.GetBuffer(new ReadRequest(i));
            //    var header = PageHeader.FromBuffer(response.BufferSegment.Span);

            //    // new page at this id, not saved back to disk
            //    if (header.PageId == 0)
            //        continue;
            //    if (header.Type != PageType.Data)
            //    {
            //        continue;
            //    }

            //    var pageFromDisk = new DataPage(response.BufferSegment);
            //    if (!pageFromDisk.IsFull)
            //    {
            //        _cacheService.Add(pageFromDisk);
            //        return pageFromDisk;
            //    }
            //    else
            //    {
            //        pageFromDisk.Dispose();
            //    }
            //}

            var page = CreateNewPage<DataPage>();
            _cacheService.Add(page);

            return page;
        }

        private T CreateNewPage<T>() where T: BasePage
        {
            
            if (typeof(T) == typeof(DataPage))
            {

            }
            return default;
        }
    }
}
