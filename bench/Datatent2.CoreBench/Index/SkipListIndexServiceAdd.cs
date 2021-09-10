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
    //[InProcess()]
    public class SkipListIndexServiceAdd
    {
        Random _random = new Random();

        HashSet<int> toInsert = new HashSet<int>();

        //[Params(100, 500, 1000, 2500, 5000, 7500, 10000)]
        public int Count = 1000;

        [GlobalSetup]
        public void Setup()
        {
            toInsert.Clear();

            int i = 0;
            while (i < Count)
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
        public async Task AddBenchmark()
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


/*
    Always level 1
   |       Method |     Mean |    Error |   StdDev |   Median | Kurtosis | Skewness | Rank | Baseline |      Gen 0 | Allocated |
   |------------- |---------:|---------:|---------:|---------:|---------:|---------:|-----:|--------- |-----------:|----------:|
   | AddBenchmark | 71.53 ms | 5.442 ms | 8.145 ms | 69.44 ms |    5.176 |    1.578 |    1 |       No | 23625.0000 |     95 MB |
 */