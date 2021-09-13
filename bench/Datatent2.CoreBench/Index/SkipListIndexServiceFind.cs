using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;
using BenchmarkDotNet.Attributes;
using Bogus;
using Datatent2.Contracts;
using Datatent2.Core.Memory;
using Datatent2.Core.Page;
using Datatent2.Core.Page.Data;
using Datatent2.Core.Page.Index;
using Datatent2.Core.Page.Table;
using Datatent2.Core.Services.Index;
using Datatent2.Core.Services.Page;
using Microsoft.Extensions.Logging.Abstractions;
# nullable enable

namespace Datatent2.CoreBench.Index
{
    [HtmlExporter, CsvExporter(), CsvMeasurementsExporter(),
     RankColumn(), KurtosisColumn, SkewnessColumn, StdDevColumn, MeanColumn, MedianColumn, BaselineColumn, MediumRunJob, MemoryDiagnoser]
    //[InProcess()]
    public class SkipListIndexServiceFind
    {
        Random _random = new Random();

        HashSet<int> toInsert = new HashSet<int>();

        [Params(1, 100, 500, 1000, 2500, 5000, 7500, 10000, 25000, 50000)]
        public int Count = 50000;
        private IndexService _indexSkipList;
        private int toSearch;
        private IndexService _indexHeap;

        [GlobalSetup]
        public async Task Setup()
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
            IPageService pageService = new FakePageService();
            IPageService pageService2 = new FakePageService();

            _indexSkipList = await IndexService.CreateIndex(pageService, IndexType.SkipList, NullLogger.Instance);
            _indexHeap = await IndexService.CreateIndex(pageService2, IndexType.Heap, NullLogger.Instance);

            foreach (var a in toInsert)
            {
                await _indexSkipList.Insert(a, PageAddress.Empty).ConfigureAwait(false);
                await _indexHeap.Insert(a, PageAddress.Empty).ConfigureAwait(false);
            }

            var ordered = toInsert.OrderBy(i1 => i1).ToArray();
            
            toSearch = ordered[ordered.Length / 2];
        }


        [Benchmark]
        public async Task<long> FindSkipList()
        {
            long a = 0;
            var res = await _indexSkipList.Find(toSearch).ConfigureAwait(false);
            a += res!.Value.SlotId;
            return a;
        }

        [Benchmark]
        public async Task<long> FindHeap()
        {
            long a = 0;
            var res = await _indexHeap.Find(toSearch).ConfigureAwait(false);
            a += res!.Value.SlotId;
            return a;
        }

    }
}

/*
|       Method | Count |         Mean |        Error |        StdDev |       Median | Kurtosis | Skewness | Rank | Baseline |   Gen 0 | Allocated |
|------------- |------ |-------------:|-------------:|--------------:|-------------:|---------:|---------:|-----:|--------- |--------:|----------:|
| FindSkipList |     1 |     532.4 ns |     64.54 ns |      96.61 ns |     532.6 ns |   0.9445 |  -0.0001 |    2 |       No |  0.0553 |     464 B |
|     FindHeap |     1 |     119.5 ns |      0.97 ns |       1.45 ns |     119.3 ns |   2.4210 |   0.5462 |    1 |       No |  0.0172 |     144 B |
| FindSkipList |   100 |   3,150.9 ns |    289.93 ns |     415.81 ns |   3,183.9 ns |   0.9579 |  -0.0163 |    3 |       No |  0.5875 |   4,928 B |
|     FindHeap |   100 |     573.8 ns |     18.11 ns |      26.54 ns |     559.1 ns |   1.0523 |   0.0650 |    2 |       No |  0.1278 |   1,072 B |
| FindSkipList |   500 |   3,500.2 ns |    357.53 ns |     524.06 ns |   3,778.4 ns |   1.3536 |   0.1460 |    4 |       No |  0.4845 |   4,064 B |
|     FindHeap |   500 |   7,117.1 ns |    382.78 ns |     548.98 ns |   7,165.0 ns |   0.9937 |  -0.0208 |    9 |       No |  1.4572 |  12,208 B |
| FindSkipList |  1000 |   4,269.1 ns |    186.96 ns |     279.83 ns |   4,307.5 ns |   2.3906 |  -0.0935 |    5 |       No |  0.6332 |   5,328 B |
|     FindHeap |  1000 |   5,555.2 ns |  3,406.14 ns |   5,098.15 ns |   5,465.0 ns |   0.9360 |   0.0011 |    8 |       No |  0.1087 |     912 B |
| FindSkipList |  2500 |   4,432.5 ns |     75.72 ns |     108.60 ns |   4,437.6 ns |   1.6008 |  -0.1133 |    6 |       No |  0.7706 |   6,480 B |
|     FindHeap |  2500 |  22,786.0 ns |  9,802.89 ns |  14,368.95 ns |  10,011.7 ns |   0.9460 |   0.0701 |   10 |       No |  8.1787 |  68,912 B |
| FindSkipList |  5000 |   5,640.0 ns |     94.82 ns |     132.93 ns |   5,608.7 ns |   2.7098 |   0.0893 |    8 |       No |  1.0529 |   8,816 B |
|     FindHeap |  5000 |  63,669.9 ns | 15,629.29 ns |  22,909.21 ns |  72,775.0 ns |   1.3791 |   0.2516 |   11 |       No | 16.6016 | 139,024 B |
| FindSkipList |  7500 |   5,146.0 ns |    150.27 ns |     200.61 ns |   5,158.7 ns |   1.3450 |   0.0728 |    7 |       No |  0.8163 |   6,864 B |
|     FindHeap |  7500 |  76,158.2 ns | 14,496.41 ns |  21,248.64 ns |  57,590.7 ns |   0.9421 |   0.0647 |   12 |       No | 22.2168 | 186,832 B |
| FindSkipList | 10000 |   5,499.1 ns |    265.32 ns |     388.90 ns |   5,199.9 ns |   0.9703 |   0.0639 |    8 |       No |  0.8621 |   7,248 B |
|     FindHeap | 10000 | 124,275.6 ns |  5,785.76 ns |   8,480.69 ns | 130,548.2 ns |   0.9795 |  -0.0386 |   13 |       No | 26.6113 | 223,024 B |
| FindSkipList | 25000 |   6,536.1 ns |    603.23 ns |     884.20 ns |   5,769.2 ns |   0.9444 |   0.0620 |    9 |       No |  0.9842 |   8,240 B |
|     FindHeap | 25000 | 271,767.1 ns | 88,444.03 ns | 129,640.11 ns | 388,378.0 ns |   0.9381 |  -0.0644 |   14 |       No | 89.8438 | 754,576 B |
| FindSkipList | 50000 |   6,954.1 ns |     38.14 ns |      55.91 ns |   6,953.7 ns |   2.0499 |   0.0746 |    9 |       No |  1.1368 |   9,568 B |
|     FindHeap | 50000 | 474,376.5 ns | 96,070.55 ns | 140,818.97 ns | 600,416.4 ns |   0.9415 |  -0.0656 |   15 |       No | 70.3125 | 590,544 B |
 */