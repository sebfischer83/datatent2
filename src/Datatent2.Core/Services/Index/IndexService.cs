using System;
using System.Threading.Tasks;
using Datatent2.Contracts.Exceptions;
using Datatent2.Core.Index.Heap;
using Datatent2.Core.Page;
using Datatent2.Core.Page.Index;
using Datatent2.Core.Services.Index.Heap;
using Datatent2.Core.Services.Page;
using Microsoft.Extensions.Logging;

namespace Datatent2.Core.Services.Index
{
    internal abstract class IndexService
    {
        public uint PageIndex => FirstPageIndex;

        protected readonly uint FirstPageIndex;
        protected readonly PageService PageService;
        protected readonly ILogger Logger;

        public abstract IndexType Type { get; }

        protected IndexService(uint firstPageIndex, PageService pageService, ILogger logger)
        {
            FirstPageIndex = firstPageIndex;
            PageService = pageService;
            Logger = logger;
        }

        public abstract Task<PageAddress?> Find<T>(T key);

        public abstract Task<PageAddress[]> FindMany<T>(T key);

        public abstract Task Insert<T>(T key, PageAddress pageAddress);

        public abstract Task Delete<T>(T key);
        
        public static async Task<IndexService> CreateIndex(PageService pageService, IndexType indexType, ILogger logger)
        {
            var indexPage = await pageService.CreateNewPage<IndexPage>();
            indexPage.InitHeader(indexType);
            await pageService.WritePage(indexPage);

            return await LoadIndex(indexPage.Id, pageService, logger);
        }
        
        public static async Task<IndexService> LoadIndex(uint firstIndexPage, PageService pageService, ILogger logger)
        {
            var index = await pageService.GetPage<IndexPage>(firstIndexPage);

            if (index == null)
                throw new InvalidPageException($"The page {firstIndexPage} doesn't contain an index!", firstIndexPage);

            if (!index.IsStartPage)
                throw new InvalidPageException($"The page {firstIndexPage} isn't the first page of the index!", firstIndexPage);

            IndexService returnIndexService = index.IndexPageHeader.Type switch
            {
                IndexType.Heap => new HeapIndexService(firstIndexPage, pageService, logger),
                _ => throw new ArgumentOutOfRangeException(
                    $"{nameof(index.IndexPageHeader.Type)} {Enum.GetName(typeof(IndexType), index.IndexPageHeader.Type)}")
            };

            return returnIndexService;
        }

        public override string ToString()
        {
            return $"{Enum.GetName(typeof(IndexType), Type)}:{PageIndex}";
        }
    }

    internal enum IndexType : byte
    {
        Heap = 1,
        HeapUnique = 2,
        SkipList = 3,
        Bloom = 4
    }
}
