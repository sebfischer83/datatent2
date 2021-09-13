using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Datatent2.Contracts.Exceptions;
using Datatent2.Core.Index.Heap;
using Datatent2.Core.Page;
using Datatent2.Core.Page.Index;
using Datatent2.Core.Services.Index.Heap;
using Datatent2.Core.Services.Index.SkipList;
using Datatent2.Core.Services.Page;
using Microsoft.CodeAnalysis.Scripting.Hosting;
using Microsoft.Extensions.Logging;

namespace Datatent2.Core.Services.Index
{
    internal abstract class IndexService
    {
        protected readonly SemaphoreSlim SemaphoreSlim = new SemaphoreSlim(1, 1);
        public uint PageIndex => FirstPageIndex;

        protected readonly uint FirstPageIndex;
        protected readonly IPageService PageService;
        protected readonly ILogger Logger;

        public abstract IndexType Type { get; }

        protected IndexService(uint firstPageIndex, IPageService pageService, ILogger logger)
        {
            FirstPageIndex = firstPageIndex;
            PageService = pageService;
            Logger = logger;
        }

        public virtual Task Initialize()
        {
            return Task.CompletedTask;
        }

        public virtual Task<string> Print(PrintStyle printStyle)
        {
            return Task.FromResult(string.Empty);
        }

        public abstract Task<PageAddress?> Find<T>(T key);

        public abstract Task<PageAddress[]> FindMany<T>(T key);

        public abstract Task Insert<T>(T key, PageAddress pageAddress);

        public abstract Task Delete<T>(T key);

        public abstract Task DeleteIndex();

        public abstract IAsyncEnumerable<(T Key, PageAddress Address)> GetAll<T>();

        public static async Task<IndexService> CreateIndex(IPageService pageService, IndexType indexType, ILogger logger)
        {
            var indexPage = await pageService.CreateNewPage<IndexPage>().ConfigureAwait(false);
            indexPage.InitHeader(indexType);
            
            await pageService.WritePage(indexPage).ConfigureAwait(false);

            return await LoadIndex(indexPage.Id, pageService, logger).ConfigureAwait(false);
        }
        
        public static async Task<IndexService> LoadIndex(uint firstIndexPage, IPageService pageService, ILogger logger)
        {
            var index = await pageService.GetPage<IndexPage>(firstIndexPage).ConfigureAwait(false);

            if (index == null)
                throw new InvalidPageException($"The page {firstIndexPage} doesn't contain an index!", firstIndexPage);

            if (!index.IsStartPage)
                throw new InvalidPageException($"The page {firstIndexPage} isn't the first page of the index!", firstIndexPage);

            IndexService returnIndexService = index.IndexPageHeader.Type switch
            {
                IndexType.Heap => new HeapIndexService(firstIndexPage, pageService, logger),
                IndexType.SkipList => new SkipListIndexService(firstIndexPage, pageService, logger),
                _ => throw new ArgumentOutOfRangeException(
                    $"{nameof(index.IndexPageHeader.Type)} {Enum.GetName(typeof(IndexType), index.IndexPageHeader.Type)}")
            };

            await returnIndexService.Initialize().ConfigureAwait(false);

            return returnIndexService;
        }

        public override string ToString()
        {
            return $"{Enum.GetName(typeof(IndexType), Type)}:{PageIndex}";
        }
    }

    internal class PrintStyle
    {
        public bool AttachIndexAddresses { get; set; }
    }

    internal enum IndexType : byte
    {
        Undefined = 0,
        Heap = 1,
        HeapUnique = 2,
        SkipList = 3,
        Bloom = 4
    }
}
