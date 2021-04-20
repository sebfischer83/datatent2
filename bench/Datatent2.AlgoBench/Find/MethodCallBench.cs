using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace Datatent2.AlgoBench.Find
{
    [HtmlExporter, CsvExporter(), CsvMeasurementsExporter(), MarkdownExporter,
     MeanColumn, MedianColumn, MediumRunJob(RuntimeMoniker.NetCoreApp50), BaselineColumn]
    public class MethodCallBench
    {
        private static Dictionary<ulong, int> _dictionary;

        [GlobalSetup]
        public void Setup()
        {
            _dictionary = new Dictionary<ulong, int>();

            for (int i = 0; i < 100000; i++)
            {
                var count = BitOperations.LeadingZeroCount((ulong)i);
                var res = (64 - count) + 1;
                _dictionary.Add((ulong)i, res);
            }
        }

        [Benchmark]
        public ulong Computation()
        {
            ulong l = 0;

            for (ulong i = 0; i < 100000; i++)
            {
                var count = BitOperations.LeadingZeroCount((ulong) i);
                var res = (64 - count) + 1;
                l += (ulong)res;
            }

            return l;
        }

        [Benchmark]
        public ulong Lookup()
        {
            ulong l = 0;

            for (ulong i = 0; i < 100000; i++)
            {
                var res = _dictionary[i];
                l += (ulong)res;
            }

            return l;
        }
    }
}
