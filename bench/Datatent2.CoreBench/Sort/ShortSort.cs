using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Advanced.Algorithms.Distributed;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Order;

namespace Datatent2.AlgoBench.Sort
{
    [HtmlExporter, CsvExporter(), CsvMeasurementsExporter(),
     RankColumn(), KurtosisColumn, SkewnessColumn, StdDevColumn, MeanColumn, MedianColumn, BaselineColumn, MediumRunJob, MemoryDiagnoser, Orderer(SummaryOrderPolicy.FastestToSlowest, MethodOrderPolicy.Declared)]
    public class ShortSort
    {
        [Params(256, 8192, 65536, 4194304, 16000000)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "<Pending>")]
        public int ArraySize;

        private readonly Consumer _consumer = new Consumer();

        private short[] _globalArray;
        private short[] _array;

        [GlobalSetup]
        public void GlobalSetup()
        {
            _globalArray = new short[ArraySize];

            Random random = new Random();
            for (int i = 0; i < ArraySize; i++)
            {
                _globalArray[i] = (short)random.Next(0, short.MaxValue);
            }
        }

        [IterationSetup]
        public void Setup()
        {
            _array = new short[ArraySize];
            Buffer.BlockCopy(_globalArray, 0, _array, 0, _globalArray.Length);
        }

        [Benchmark(Baseline = true)]
        public int Sort()
        {
            Array.Sort(_array);

            return _array.Length;
        }

        [Benchmark(Baseline = false)]
        public int Linq()
        {
            _array.OrderBy(u => u).Consume(_consumer);

            return _array.Length;
        }

        [Benchmark(Baseline = false)]
        public int ParallelLinq()
        {
            _array.AsParallel().OrderBy(u => u).Consume(_consumer);

            return _array.Length;
        }

        //[Benchmark(Baseline = false)]
        //public int CountingSort()
        //{
        //    var sorted = _array.SortCounting();
        //    return sorted.Length;
        //}

        //[Benchmark(Baseline = false)]
        //public int CountingSortInPlace()
        //{
        //    _array.SortCountingInPlace();
        //    return _array.Length;
        //}

        //[Benchmark(Baseline = false)]
        //public int MergeSort()
        //{
        //    var sorted = _array.SortMerge();
        //    return sorted.Length;
        //}

        //[Benchmark(Baseline = false)]
        //public int SortMergeHybridWithRadixPar()
        //{
        //    _array.SortMergeHybridWithRadixPar(s => (uint)s);
        //    return _array.Length;
        //}

        //[Benchmark(Baseline = false)]
        //public int SortMergeInPlace()
        //{
        //    _array.SortMergeInPlace();
        //    return _array.Length;
        //}

        //[Benchmark(Baseline = false)]
        //public int SortMergeInPlacePar()
        //{
        //    _array.SortMergeInPlacePar();
        //    return _array.Length;
        //}

        //[Benchmark(Baseline = false)]
        //public int SortMergeInPlacePurePar()
        //{
        //    _array.SortMergeInPlacePurePar();
        //    return _array.Length;
        //}

        //[Benchmark(Baseline = false)]
        //public int SortMergeInPlaceStablePar()
        //{
        //    _array.SortMergeInPlaceStablePar();
        //    return _array.Length;
        //}

        //[Benchmark(Baseline = false)]
        //public int SortMergeStablePar()
        //{
        //    var sorted = _array.SortMergeStablePar();
        //    return sorted.Length;
        //}

        //[Benchmark(Baseline = false)]
        //public int SortRadixFasterNew()
        //{
        //    var sorted = _array.SortRadixFasterNew(s => (uint) s);
        //    return sorted.Length;
        //}

        //[Benchmark(Baseline = false)]
        //public int SortRadixMsd()
        //{
        //    _array.SortRadixMsd();
        //    return _array.Length;
        //}
    }
}
