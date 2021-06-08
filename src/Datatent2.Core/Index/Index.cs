using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Datatent2.Contracts.Exceptions;
using Datatent2.Core.Index.Heap;
using Datatent2.Core.Page;
using Datatent2.Core.Page.Index;
using Datatent2.Core.Services.Index;
using Datatent2.Core.Services.Page;
using Microsoft.Extensions.Logging;

namespace Datatent2.Core.Index
{
    internal abstract class Index
    {
        public uint IndexPage => FirstIndexPage;

        protected readonly uint FirstIndexPage;
        protected readonly PageService PageService;
        protected readonly ILogger Logger;

        public abstract IndexType Type { get; }

        protected Index(uint firstIndexPage, PageService pageService, ILogger logger)
        {
            FirstIndexPage = firstIndexPage;
            PageService = pageService;
            Logger = logger;
        }

        public abstract Task<PageAddress?> Find<T>(T key);

        public abstract Task Insert<T>(T key, PageAddress pageAddress);

        public abstract Task Delete<T>(T key);
        
        public static async Task<Index> CreateIndex(PageService pageService, IndexType indexType, ILogger logger)
        {
            var indexPage = await pageService.CreateNewPage<IndexPage>();
            indexPage.InitHeader(indexType);
            await pageService.WritePage(indexPage);

            return await LoadIndex(indexPage.Id, pageService, logger);
        }
        
        public static async Task<Index> LoadIndex(uint firstIndexPage, PageService pageService, ILogger logger)
        {
            var index = await pageService.GetPage<IndexPage>(firstIndexPage);

            if (index == null)
                throw new InvalidPageException($"The page {firstIndexPage} doesn't contain an index!", firstIndexPage);

            if (!index.IsStartPage)
                throw new InvalidPageException($"The page {firstIndexPage} isn't the first page of the index!", firstIndexPage);

            Index returnIndex = index.IndexPageHeader.Type switch
            {
                IndexType.Heap => new HeapIndex(firstIndexPage, pageService, logger),
                _ => throw new ArgumentOutOfRangeException(
                    $"{nameof(index.IndexPageHeader.Type)} {Enum.GetName(typeof(IndexType), index.IndexPageHeader.Type)}")
            };

            return returnIndex;
        }
    }

    internal enum IndexType : byte
    {
        Heap = 1,
        SkipList = 2,
        Bloom = 4
    }
}
