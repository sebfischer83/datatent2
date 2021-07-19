// # SPDX-License-Identifier: MIT
// # Copyright 2021
// # Sebastian Fischer sebfischer@gmx.net

using System;
using System.Threading.Tasks;
using Datatent2.Contracts.Exceptions;
using Datatent2.Core.Page;
using Datatent2.Core.Page.Index;
using Datatent2.Core.Services.Page;
using Microsoft.Extensions.Logging;

namespace Datatent2.Core.Index.Heap
{
    internal class HeapIndex : Index
    {
        protected IndexPage? IndexPage { get; set; }

        internal HeapIndex(uint firstIndexPage, PageService pageService, ILogger logger) : base(firstIndexPage, pageService, logger)
        {
        }

        public override IndexType Type => IndexType.Heap;
        public override async Task<PageAddress?> Find<T>(T key)
        {
            var firstPage = await GetFirstPage();

            IndexPage page = firstPage;
            while (true)
            {
                // go through all entries
                var address = page.SearchHeapIndexKey(key);
                if (address != null)
                    return address;

                if (page.PageHeader.NextPageId == uint.MaxValue)
                    break;
                var pageTemp = await PageService.GetPage<IndexPage>(page.PageHeader.NextPageId);
                if (pageTemp == null)
                    break;
                page = pageTemp;
            }

            return null;
        }

        public override async Task Insert<T>(T key, PageAddress pageAddress)
        {
            // get first index page and look if there is enough space for this entry
            var firstPage = await GetFirstPage();

            HeapKey heapKey;
            switch (key)
            {
                case string s:
                    heapKey = new HeapKey(pageAddress, s);
                    break;
                case sbyte:
                case byte:
                case short:
                case int:
                case long:
                    heapKey = new HeapKey(pageAddress, (long) System.Convert.ChangeType(key, TypeCode.Int64));
                    break;
                case ushort:
                case uint:
                case ulong:
                    heapKey = new HeapKey(pageAddress, (ulong)System.Convert.ChangeType(key, TypeCode.UInt64));
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
                if (page.FreeContinuousBytes >= keyLength)
                {
                    page.AddHeapIndexKey(heapKey);
                    break;
                }

                if (page.PageHeader.NextPageId != UInt32.MaxValue)
                {
                    var p = await PageService.GetPage<IndexPage>(page.PageHeader.NextPageId);
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
                    var newPage = await PageService.CreateNewPage<IndexPage>();
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

        private async ValueTask<IndexPage> GetFirstPage()
        {
            if (IndexPage == null)
            {
                var page = await PageService.GetPage<IndexPage>(FirstIndexPage);
                if (page == null)
                    throw new PageNotFoundException($"Index page don't exist!", FirstIndexPage);
                IndexPage = page;
                return page;
            }

            return IndexPage;
        }
    }

    internal enum HeapKeyType : byte
    {
        Numeric = 1,
        UnsignedNumeric = 2,
        String = 2,
        Guid = 3
    }
}