// # SPDX-License-Identifier: MIT
// # Copyright 2021
// # Sebastian Fischer sebfischer@gmx.net

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Datatent2.Contracts.Exceptions;
using Datatent2.Core.Index.Heap;
using Datatent2.Core.Page;
using Datatent2.Core.Page.Index;
using Datatent2.Core.Services.Page;
using Microsoft.Extensions.Logging;

namespace Datatent2.Core.Services.Index.Heap
{
    internal class HeapIndexService : Services.Index.IndexService
    {
        protected IndexPage? IndexPage { get; set; }

        internal HeapIndexService(uint firstPageIndex, IPageService pageService, ILogger logger) : base(firstPageIndex, pageService, logger)
        {
        }

        public override IndexType Type => IndexType.Heap;
        public override async Task<PageAddress?> Find<T>(T key)
        {
            var firstPage = await GetFirstPage().ConfigureAwait(false);

            IndexPage page = firstPage;
            while (true)
            {
                // go through all entries
                var address = page.SearchHeapIndexKey(key);
                if (address != null)
                    return address;

                if (page.PageHeader.NextPageId == uint.MaxValue)
                    break;
                var pageTemp = await PageService.GetPage<IndexPage>(page.PageHeader.NextPageId).ConfigureAwait(false);
                if (pageTemp == null)
                    break;
                page = pageTemp;
            }

            return null;
        }

        public override async Task<PageAddress[]> FindMany<T>(T key)
        {
            var firstPage = await GetFirstPage().ConfigureAwait(false);
            List<PageAddress> list = new();

            IndexPage page = firstPage;
            while (true)
            {
                // go through all entries
                var address = page.SearchHeapIndexKeyMany(key);
                if (address.Length > 0)
                    list.AddRange(address);

                if (page.PageHeader.NextPageId == uint.MaxValue)
                    break;
                var pageTemp = await PageService.GetPage<IndexPage>(page.PageHeader.NextPageId).ConfigureAwait(false);
                if (pageTemp == null)
                    break;
                page = pageTemp;
            }

            return list.ToArray();
        }

        public override async Task Insert<T>(T key, PageAddress pageAddress)
        {
            // get first index page and look if there is enough space for this entry
            var firstPage = await GetFirstPage().ConfigureAwait(false);

            HeapKey heapKey;
            switch (key)
            {
                case string s:
                    heapKey = new HeapKey(pageAddress, s);
                    break;
                case sbyte sb:
                    heapKey = new HeapKey(pageAddress, sb);
                    break;
                case byte b:
                    heapKey = new HeapKey(pageAddress, b);
                    break;
                case short sh:
                    heapKey = new HeapKey(pageAddress, sh);
                    break;
                case int i:
                    heapKey = new HeapKey(pageAddress, i);
                    break;
                case long l:
                    heapKey = new HeapKey(pageAddress, l);
                    break;
                case ushort us:
                    heapKey = new HeapKey(pageAddress, us);
                    break;
                case uint ui:
                    heapKey = new HeapKey(pageAddress, ui);
                    break;
                case ulong ul:
                    heapKey = new HeapKey(pageAddress, ul);
                    break;
                case Guid g:
                    heapKey = new HeapKey(pageAddress, g);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(key));
            }

            var keyLength = heapKey.Length;

            IndexPage page = firstPage;

            while (true)
            {
                if (page.FreeContinuousBytes >= keyLength && page.AddHeapIndexKey(heapKey))
                {
                    break;
                }

                if (page.PageHeader.NextPageId != UInt32.MaxValue)
                {
                    var p = await PageService.GetPage<IndexPage>(page.PageHeader.NextPageId).ConfigureAwait(false);
                    if (p == null)
                    {
                        throw new InvalidPageException($"Linked index page doesn't exist anymore!",
                            page.PageHeader.NextPageId);
                    }
                    page = p;
                }
                else
                {
                    // new index page
                    var newPage = await PageService.CreateNewPage<IndexPage>().ConfigureAwait(false);
                    newPage.InitHeader(Type);
                    newPage.SetPreviousPage(page.Id);
                    page.SetNextPage(newPage.Id);
                    page = newPage;
                }
            }
        }

        public override Task Delete<T>(T key)
        {
            throw new NotImplementedException();
        }

        public override Task DeleteIndex()
        {
            throw new NotImplementedException();
        }

        public override IAsyncEnumerable<(T Key, PageAddress Address)> GetAll<T>()
        {
            throw new NotImplementedException();
        }

        private async ValueTask<IndexPage> GetFirstPage()
        {
            if (IndexPage == null)
            {
                var page = await PageService.GetPage<IndexPage>(FirstPageIndex).ConfigureAwait(false);
                if (page == null)
                    throw new PageNotFoundException($"IndexService page don't exist!", FirstPageIndex);
                IndexPage = page;
                return page;
            }

            return IndexPage;
        }

        public override Task Initialize()
        {
            var first = this.GetFirstPage();
            return Task.CompletedTask;
        }

        public override Task<string> Print(PrintStyle printStyle)
        {


            return base.Print(printStyle);
        }
    }

    internal enum HeapKeyType : byte
    {
        Numeric = 1,
        UnsignedNumeric = 2,
        String = 2,
        Guid = 3,
        Empty = 99
    }
}