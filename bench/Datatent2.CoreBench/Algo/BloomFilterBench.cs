using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Bogus.DataSets;
using Datatent2.Core.Algo;
using Datatent2.Core.Algo.Bloom;

namespace Datatent2.CoreBench.Algo
{
    [HtmlExporter, CsvExporter(), CsvMeasurementsExporter(),
     RankColumn(), KurtosisColumn, SkewnessColumn, StdDevColumn, MeanColumn, MedianColumn, BaselineColumn, MediumRunJob, MemoryDiagnoser]
    public class BloomFilterBench
    {
        private string[] _text;

        private int[] _numbers;

        [GlobalSetup]
        public void Setup()
        {
            var lorem = new Lorem();
            _text = lorem.Words(5000000);
            _numbers = new int[500000];
            Random random = new Random();
            for (int i = 0; i < _numbers.Length; i++)
            {
                _numbers[i] = random.Next(0, 5000);
            }
        }

        //[Benchmark]
        //public long BenchHashSet()
        //{
        //    long l = 0;
        //    HashSet<string> hashSet = new HashSet<string>(_text.Length);

        //    for (int i = 0; i < _text.Length; i++)
        //    {
        //        ref var s = ref _text[i];
        //        if (hashSet.Contains(s))
        //            l++;
        //        else
        //            hashSet.Add(s);
        //    }

        //    return l;
        //}

        //[Benchmark]
        //public long BenchBloom()
        //{
        //    long l = 0;
        //    InMemoryBloomFilter<string> bloomFilter = new InMemoryBloomFilter<string>(_text.Length);

        //    for (int i = 0; i < _text.Length; i++)
        //    {
        //        ref var s = ref _text[i];
        //        if (bloomFilter.Contains(s))
        //            l++;
        //        else
        //            bloomFilter.Add(s);
        //    }

        //    return l;
        //}

        [Benchmark]
        public long BenchHashSet()
        {
            long l = 0;
            HashSet<int> hashSet = new HashSet<int>(_numbers.Length);

            for (int i = 0; i < _numbers.Length; i++)
            {
                ref var s = ref _numbers[i];
                if (hashSet.Contains(s))
                    l++;
                else
                    hashSet.Add(s);
            }

            return l;
        }

        [Benchmark]
        public long BenchBloom()
        {
            long l = 0;
            InMemoryBloomFilter<int> inMemoryBloomFilter = new InMemoryBloomFilter<int>(_numbers.Length);

            for (int i = 0; i < _numbers.Length; i++)
            {
                ref var s = ref _numbers[i];
                if (inMemoryBloomFilter.Contains(s))
                    l++;
                else
                    inMemoryBloomFilter.Add(s);
            }

            return l;
        }
    }
}
