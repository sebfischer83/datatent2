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
    public class SkipListNodeBench
    {
        private SkipListNode<int> _intNode = new SkipListNode<int>(35, new PageAddress(1, 1), 5);
        private SkipListNode<long> _longNode = new SkipListNode<long>((long)35, new PageAddress(1, 1), 5);
        private SkipListNode<Guid> _guidNode = new SkipListNode<Guid>(Guid.NewGuid(), new PageAddress(1, 1), 5);

        [Benchmark]
        public int IntNode()
        {
            int t = 0;
            for (int i = 0; i < 10000; i++)
            {
                var bytes = _intNode.ToBytes();

                var node2 = SkipListNode<int>.FromBytes(bytes);
                t = bytes.Length + node2.GetHashCode();

            }

            return t;
        }

        [Benchmark]
        public int LongNode()
        {
            int t = 0;
            for (int i = 0; i < 10000; i++)
            {
                var bytes = _longNode.ToBytes();

                var node2 = SkipListNode<long>.FromBytes(bytes);

                t = bytes.Length + node2.GetHashCode();
            }
            return t;
        }

        [Benchmark]
        public int GuidNode()
        {
            int t = 0;
            for (int i = 0; i < 10000; i++)
            {
                var bytes = _guidNode.ToBytes();

                var node2 = SkipListNode<Guid>.FromBytes(bytes);

                t = bytes.Length + node2.GetHashCode();

            }
            return t;
        }
    }
}

/*
 * 
 *
 * |   Method |     Mean |   Error |  StdDev |   Median | Kurtosis | Skewness | Rank | Baseline |    Gen 0 | Gen 1 | Gen 2 | Allocated |
   |--------- |---------:|--------:|--------:|---------:|---------:|---------:|-----:|--------- |---------:|------:|------:|----------:|
   |  IntNode | 399.4 us | 2.61 us | 3.83 us | 399.6 us |    3.125 |   0.0641 |    1 |       No | 209.9609 |     - |     - |      2 MB |
   | LongNode | 414.2 us | 4.27 us | 6.26 us | 412.8 us |    2.988 |   0.8554 |    2 |       No | 219.7266 |     - |     - |      2 MB |
   | GuidNode | 506.5 us | 3.23 us | 4.63 us | 507.7 us |    1.880 |   0.0825 |    3 |       No | 238.2813 |     - |     - |      2 MB |
 *
 * 
 */