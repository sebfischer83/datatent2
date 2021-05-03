using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Spreads;

namespace Datatent2.AlgoBench.Find
{
    [HtmlExporter, CsvExporter(), CsvMeasurementsExporter(), MarkdownExporter,
     MeanColumn, MedianColumn, MediumRunJob, BaselineColumn]
    public class BinarySearchBench
    {
        private long[] _array;
        private long[] _toSearch;

        [GlobalSetup]
        public void Setup()
        {
            _array = Enumerable.Range(0, 1600).Select(i => (long)i).ToArray();

            _toSearch = Enumerable.Range(0, 1000).Select(i =>
            {
                Random random = new Random();
                return (long) random.Next(5, 1500);
            }).ToArray();
        }

        [Benchmark(Baseline = true)]
        public long Classic()
        {
            long i = 0;

            for (int j = 0; j < _toSearch.Length; j++)
            {
                i += (long) BinarySearchClassic(ref _array[0], _array.Length, _toSearch[j]);
            }

            return i;
        }

        [Benchmark(Baseline = false)]
        public long Avx2Search()
        {
            long i = 0;

            for (int j = 0; j < _toSearch.Length; j++)
            {
                i += (long)BinarySearchAvx2(ref _array[0], _array.Length, _toSearch[j]);
            }

            return i;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining
                    | MethodImplOptions.AggressiveOptimization
        )]
        internal static int BinarySearchAvx2(ref long vecStart, int length, long value)
        {
            unchecked
            {
                int c;
                int lo = 0;
                int hi = length - 1;
                Vector256<long> vec;
                Vector256<long> gt;
                int mask;

                if (hi < Vector256<long>.Count)
                {
                    goto LINEAR;
                }

                var valVec = Vector256.Create(value);
                while (hi - lo >= Vector256<long>.Count * 2)
                {
                    var i = (int)(((uint)hi + (uint)lo - Vector256<long>.Count) >> 1);

                    vec = Unsafe.ReadUnaligned<Vector256<long>>(ref Unsafe.As<long, byte>(ref Unsafe.Add(ref vecStart, i)));
                    gt = Avx2.CompareGreaterThan(valVec, vec);
                    mask = Avx2.MoveMask(gt.AsByte());

                    if (mask != -1)
                    {
                        if (mask != 0)
                        {
                            int clz = (int)Lzcnt.LeadingZeroCount((uint)mask);
                            int index = (32 - clz) / Unsafe.SizeOf<long>();
                            lo = i + index;
                            c = value.CompareTo(UnsafeEx.ReadUnaligned<long>(ref Unsafe.Add<long>(ref vecStart, lo)));
                            goto RETURN;
                        }

                        // val is not greater than all in vec
                        // not i-1, i could equal;
                        hi = i;
                    }
                    else
                    {
                        // val is larger than all in vec
                        lo = i + Vector256<long>.Count;
                    }
                }

                {
                    vec = Unsafe.ReadUnaligned<Vector256<long>>(ref Unsafe.As<long, byte>(ref Unsafe.Add(ref vecStart, lo)));
                    gt = Avx2.CompareGreaterThan(valVec, vec); // _mm256_cmpgt_epi64
                    mask = Avx2.MoveMask(gt.AsByte());

                    var clz = (int)Lzcnt.LeadingZeroCount((uint)mask);
                    var index = (32 - clz) / Unsafe.SizeOf<long>();
                    lo += index;
                }
                while (mask == -1 & hi - lo >= Vector256<long>.Count) ;

                LINEAR:
                while ((c = value.CompareTo(UnsafeEx.ReadUnaligned(ref Unsafe.Add(ref vecStart, lo)))) > 0
                       && ++lo <= hi)
                {
                }

                RETURN:
                var ceq1 = -UnsafeEx.Ceq(c, 0);
                return (ceq1 & lo) | (~ceq1 & ~lo);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining
            | MethodImplOptions.AggressiveOptimization
        )]
        internal static int BinarySearchClassic<T>(ref T vecStart, int length, T value, KeyComparer<T> comparer = default)
        {
            unchecked
            {
                int lo = 0;
                int hi = length - 1;
                // If length == 0, hi == -1, and loop will not be entered
                while (lo <= hi)
                {
                    // PERF: `lo` or `hi` will never be negative inside the loop,
                    //       so computing median using uints is safe since we know
                    //       `length <= int.MaxValue`, and indices are >= 0
                    //       and thus cannot overflow an uint.
                    //       Saves one subtraction per loop compared to
                    //       `int i = lo + ((hi - lo) >> 1);`
                    int i = (int)(((uint)hi + (uint)lo) >> 1);

                    int c = comparer.Compare(value, UnsafeEx.ReadUnaligned(ref Unsafe.Add(ref vecStart, i)));

                    if (c == 0)
                    {
                        return i;
                    }

                    if (c > 0)
                    {
                        lo = i + 1;
                    }
                    else
                    {
                        hi = i - 1;
                    }
                }

                // If none found, then a negative number that is the bitwise complement
                // of the index of the next element that is larger than or, if there is
                // no larger element, the bitwise complement of `length`, which
                // is `lo` at this point.
                return ~lo;
            }
        }
    }
}
