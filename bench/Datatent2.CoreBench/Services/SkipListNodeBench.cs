using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Datatent2.Core.Page;
using Datatent2.Core.Services.Index.SkipList;

namespace Datatent2.CoreBench.Services
{
    [HtmlExporter, CsvExporter(), CsvMeasurementsExporter(),
     RankColumn(), KurtosisColumn, SkewnessColumn, StdDevColumn, MeanColumn, MedianColumn, BaselineColumn, MediumRunJob, MemoryDiagnoser]
    [InProcess()]
    public class SkipListNodeBench
    {
        private SkipListNode _intNode = new SkipListNode(35, new PageAddress(1, 1), 5);
        private SkipListNode _longNode = new SkipListNode((long)35, new PageAddress(1, 1), 5);
        //private SkipListNode _guidNode = new SkipListNode(Guid.NewGuid(), new PageAddress(1, 1), 5);

        [Benchmark]
        public int IntNode()
        {
            int t = 0;
            var bytes = _intNode.ToBytes();

            var node2 = SkipListNode.FromBytes(bytes);
            t = bytes.Length + node2.GetHashCode();

            return t;
        }

        //[Benchmark]
        //public int LongNode()
        //{
        //    int t = 0;
        //    for (int i = 0; i < 10000; i++)
        //    {
        //        var bytes = _longNode.ToBytes();

        //        //var node2 = SkipListNode.FromBytes(bytes);

        //        //t = bytes.Length + node2.GetHashCode();
        //    }
        //    return t;
        //}

        //[Benchmark]
        //public int GuidNode()
        //{
        //    int t = 0;
        //    for (int i = 0; i < 10000; i++)
        //    {
        //        var bytes = _guidNode.ToBytes();

        //        var node2 = SkipListNode<Guid>.FromBytes(bytes);

        //        t = bytes.Length + node2.GetHashCode();

        //    }
        //    return t;
        //}
    }
}

/*
 * 
 *
|  Method |     Mean |    Error |   StdDev |   Median | Kurtosis | Skewness | Rank | Baseline |  Gen 0 | Allocated |
|-------- |---------:|---------:|---------:|---------:|---------:|---------:|-----:|--------- |-------:|----------:|
| IntNode | 236.3 ns | 12.45 ns | 18.25 ns | 235.9 ns |    2.382 |   0.3977 |    1 |       No | 0.0801 |     336 B |
 *
 * 
 */