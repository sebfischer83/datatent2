using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Datatent2.Contracts;
using Datatent2.Core.Memory;
using Datatent2.Core.Page;
using Datatent2.Core.Page.Data;
using Datatent2.Core.Page.Index;
using Datatent2.Core.Page.Table;
using Datatent2.Core.Services.Index;
using Datatent2.Core.Services.Page;
using Microsoft.Extensions.Logging.Abstractions;

namespace Datatent2.CoreBench.Index
{
    [HtmlExporter, CsvExporter(), CsvMeasurementsExporter(),
     RankColumn(), KurtosisColumn, SkewnessColumn, StdDevColumn, MeanColumn, MedianColumn, BaselineColumn, MediumRunJob, MemoryDiagnoser]
    public class SkipListIndexServiceAdd
    {
        Random _random = new Random();

        HashSet<int> toInsert = new HashSet<int>();

        [GlobalSetup]
        public void Setup()
        {
            toInsert.Clear();

            int i = 0;
            while (i < 10000)
            {
                var a = _random.Next();
                if (!toInsert.Contains(a))
                {
                    toInsert.Add(a);
                    i++;
                }
            }
        }

        [Benchmark]
        public async Task Add()
        {
            IPageService pageService = new FakePageService();

            var index = await IndexService.CreateIndex(pageService, IndexType.SkipList, NullLogger.Instance);

            foreach (var i in toInsert)
            {
                await index.Insert(i, PageAddress.Empty);
            }
        }
    }

    internal class FakePageService : IPageService
    {
        private Dictionary<uint, BasePage> _pages = new();

        public Task<T?> GetPage<T>(uint id) where T : BasePage
        {
            if (_pages.ContainsKey(id))
            {
                return Task.FromResult((T?)_pages[id]);
            }

            return Task.FromResult((T?)null);
        }

        public Task CheckPoint()
        {
            return Task.CompletedTask;
        }

        public Task WritePage(BasePage page)
        {
            return Task.CompletedTask;
        }

        public Task UpdatePageStatistics(BasePage page)
        {
            return Task.CompletedTask;
        }

        public Task<TablePage> GetTablePageForTable(string name)
        {
            throw new NotImplementedException();
        }

        public ValueTask<DataPage> GetDataPageWithFreeSpace()
        {
            throw new NotImplementedException();
        }

        public Task<T> CreateNewPage<T>(string? strParam = null) where T : BasePage
        {
            var id = (uint)_pages.Count + 1;
            if (typeof(T) == typeof(DataPage))
            {
                var page = (T)(object)new DataPage(new BufferSegment(Constants.PAGE_SIZE), id);
                _pages.Add(id, page);

                return Task.FromResult((T)page);
            }
            if (typeof(T) == typeof(TablePage))
            {
                var page = (T)(object)new TablePage(new BufferSegment(Constants.PAGE_SIZE), id, strParam!);
                _pages.Add(id, page);

                return Task.FromResult((T)page);
            }
            if (typeof(T) == typeof(IndexPage))
            {
                var page = (T)(object)new IndexPage(new BufferSegment(Constants.PAGE_SIZE), id);
                _pages.Add(id, page);

                return Task.FromResult((T)page);
            }

            throw new Exception();
        }
    }
}
