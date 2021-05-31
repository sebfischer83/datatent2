using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Datatent2.Core.Page.Index;
using Datatent2.Core.Services.Page;
using Microsoft.Extensions.Logging;

namespace Datatent2.Core.Index
{
    internal abstract class Index
    {
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

        public static Index CreateIndex(PageService pageService, ILogger logger)
        {

        }

        public static Index LoadIndex(uint firstIndexPage, PageService pageService, ILogger logger)
        {

        }
    }

    internal class HeapIndex : Index
    {
        protected HeapIndex(PageService pageService, ILogger logger) : base(pageService, logger)
        {
        }

        public override IndexType Type => IndexType.Heap;

    }

    internal enum IndexType : byte
    {
        Heap = 1,
        SkipList = 2,
        Bloom = 4
    }
}
