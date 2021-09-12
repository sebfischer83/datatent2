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

        [Params(100, 500, 1000, 2500, 5000, 7500, 10000)]
        public int Count;

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
|       Method | Count |        Mean |     Error |    StdDev |      Median | Kurtosis | Skewness | Rank | Baseline |      Gen 0 |    Gen 1 |  Allocated |
|------------- |------ |------------:|----------:|----------:|------------:|---------:|---------:|-----:|--------- |-----------:|---------:|-----------:|
| AddBenchmark |   100 |    261.2 us |   2.70 us |   4.04 us |    261.2 us |    2.088 |  -0.0964 |    1 |       No |    78.1250 |   2.9297 |     639 KB |
| AddBenchmark |   500 |  1,720.8 us |  12.21 us |  18.27 us |  1,721.0 us |    2.087 |  -0.0642 |    2 |       No |   511.7188 |  44.9219 |   4,189 KB |
| AddBenchmark |  1000 |  3,786.0 us |  28.78 us |  41.27 us |  3,771.4 us |    1.600 |   0.4965 |    3 |       No |  1148.4375 | 160.1563 |   9,388 KB |
| AddBenchmark |  2500 | 10,552.8 us |  65.62 us |  92.00 us | 10,582.3 us |    2.763 |  -0.8859 |    4 |       No |  3296.8750 |  31.2500 |  26,988 KB |
| AddBenchmark |  5000 | 23,183.8 us | 277.25 us | 406.39 us | 23,065.1 us |    3.737 |   1.0937 |    5 |       No |  7187.5000 |  62.5000 |  58,743 KB |
| AddBenchmark |  7500 | 36,208.0 us | 447.07 us | 669.16 us | 36,138.6 us |    2.172 |  -0.1529 |    6 |       No | 11357.1429 |  71.4286 |  92,822 KB |
| AddBenchmark | 10000 | 50,210.7 us | 478.14 us | 685.73 us | 50,092.0 us |    2.940 |  -0.0535 |    7 |       No | 15400.0000 | 200.0000 | 126,023 KB |


|       Method | Count |        Mean |     Error |      StdDev |      Median | Kurtosis | Skewness | Rank | Baseline |      Gen 0 |    Gen 1 |    Gen 2 |  Allocated |
|------------- |------ |------------:|----------:|------------:|------------:|---------:|---------:|-----:|--------- |-----------:|---------:|---------:|-----------:|
| AddBenchmark |   100 |    264.5 us |   4.77 us |     6.99 us |    264.1 us |    2.368 |   0.4102 |    1 |       No |    78.1250 |   2.9297 |        - |     640 KB |
| AddBenchmark |   500 |  1,716.7 us |  12.38 us |    17.76 us |  1,716.5 us |    1.515 |   0.1309 |    2 |       No |   517.5781 |  46.8750 |        - |   4,241 KB |
| AddBenchmark |  1000 |  3,797.3 us |  23.57 us |    35.29 us |  3,791.9 us |    3.344 |   1.0541 |    3 |       No |  1152.3438 | 164.0625 |        - |   9,444 KB |
| AddBenchmark |  2500 | 10,798.5 us | 122.67 us |   179.81 us | 10,761.2 us |    2.861 |   0.3374 |    4 |       No |  3218.7500 |  31.2500 |        - |  26,368 KB |
| AddBenchmark |  5000 | 23,438.8 us | 266.97 us |   399.58 us | 23,397.2 us |    2.891 |   0.2598 |    5 |       No |  7093.7500 |  62.5000 |        - |  58,111 KB |
| AddBenchmark |  7500 | 36,781.5 us | 530.37 us |   793.83 us | 36,609.4 us |    2.357 |   0.4726 |    6 |       No | 11428.5714 |  71.4286 |        - |  93,792 KB |
| AddBenchmark | 10000 | 50,754.8 us | 826.90 us | 1,212.05 us | 50,571.6 us |    2.275 |   0.4325 |    7 |       No | 15800.0000 | 200.0000 | 100.0000 | 129,581 KB |

 */