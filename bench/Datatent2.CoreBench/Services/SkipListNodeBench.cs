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

                //var node2 = SkipListNode<int>.FromBytes(bytes);
                t = bytes.Length;

            }

            return t;
        }

        //[Benchmark]
        //public int LongNode()
        //{
        //    int t = 0;
        //    for (int i = 0; i < 10000; i++)
        //    {
        //        var bytes = _longNode.ToBytes();

        //        //var node2 = SkipListNode<long>.FromBytes(bytes);

        //        t = bytes.Length;
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

        //        //var node2 = SkipListNode<Guid>.FromBytes(bytes);

        //        t = bytes.Length;

        //    }
        //    return t;
        //}
    }
}
