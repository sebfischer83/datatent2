using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Diagnostics.Windows.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Order;
using Microsoft.Diagnostics.Runtime.Interop;

namespace Datatent2.AlgoBench.Find
{
    [HtmlExporter, CsvExporter(), CsvMeasurementsExporter(), MarkdownExporter,
     MeanColumn, MedianColumn, MediumRunJob(), BaselineColumn]

    public class FindFirstEmptyPageBench
    {
        [Params(1000)]
        public int Iterations { get; set; }
        byte[][] buffers;

        [GlobalSetup]
        public void Setup()
        {
            List<byte[]> list = new List<byte[]>(Iterations);
            Random random = new Random();

            for (int i = 0; i < Iterations; i++)
            {
                var l = random.Next(0, 8000);
                var b = new byte[8128];
                for (int j = 0; j < l; j++)
                {
                    b[j] = 0xFF;
                }
                list.Add(b);
            }

            buffers = list.ToArray();
        }

        [Benchmark(Baseline = true)]
        public long Loop()
        {
            long l = 0;
            for (int i = 0; i < Iterations; i++)
            {
                l += DefaultLoop(buffers[i]);
            }

            return l;
        }

        [Benchmark()]
        public long LoopUnrolled4()
        {
            long l = 0;
            for (int i = 0; i < Iterations; i++)
            {
                l += FindFirstEmptyUnroll4(buffers[i]);
            }

            return l;
        }

        [Benchmark()]
        public long LoopUnrolled2()
        {
            long l = 0;
            for (int i = 0; i < Iterations; i++)
            {
                l += FindFirstEmptyUnroll2(buffers[i]);
            }

            return l;
        }

        [Benchmark()]
        public long LoopUnrolled8()
        {
            long l = 0;
            for (int i = 0; i < Iterations; i++)
            {
                l += FindFirstEmptyUnroll8(buffers[i]);
            }

            return l;
        }

        [Benchmark()]
        public long BinarySearchLike()
        {
            long l = 0;
            for (int i = 0; i < Iterations; i++)
            {
                l += FindFirstSearchLong(buffers[i]);
            }

            return l;
        }

        [Benchmark()]
        public long BinarySearchLikeLookup()
        {
            long l = 0;
            for (int i = 0; i < Iterations; i++)
            {
                l += FindFirstSearchLong3(buffers[i]);
            }

            return l;
        }

        [Benchmark()]
        public long IndexOf()
        {
            long l = 0;
            for (int i = 0; i < Iterations; i++)
            {
                l += IndexOfSearch(buffers[i]);
            }

            return l;
        }

        [Benchmark()]
        public long WithVector()
        {
            long l = 0;
            for (int i = 0; i < Iterations; i++)
            {
                l += FindFirstEmptyVectorT(buffers[i]);
            }

            return l;
        }


        [Benchmark()]
        public long OctupleSearch()
        {
            long l = 0;
            for (int i = 0; i < Iterations; i++)
            {
                l += FindFirstOctupleSearchOnce(buffers[i]);
            }

            return l;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public int IndexOfSearch(Span<byte> spanByte)
        {
            Span<ulong> span = MemoryMarshal.Cast<byte, ulong>(spanByte);

            if (span[0] == 0)
                return 1;

            if (span[^1] == long.MaxValue)
                return -1;

            int index = -1;

            var firstZero = span.IndexOf(0u);
            if (firstZero > 0)
            {
                // check prev
                if (span[firstZero - 1] == ulong.MaxValue)
                    index = firstZero;
                else
                    index = firstZero - 1;
            }
            else
            {
                index = span.Length - 1;
            }

            if (index > -1)
            {
                int res = 0;
                ref var l = ref span[index];
                var count = BitOperations.LeadingZeroCount((ulong)l);
                res = (64 - count) + 1;
                if (index > 0 && res != -1)
                    res += (64 * index);
                return res;
            }

            return index;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public unsafe int OctupleSearchLowerBound(uint* spanPtr, Vector256<int> indexes, int sectionLength)
        {
            //Load 8 indexes at once into a Vector256
            var values = Avx2.GatherVector256(spanPtr, indexes, (byte)sizeof(int));

            //How many loaded values have all bits set?
            //If true then set to 0xffffffff else 0
            var isMaxValue = Avx2.CompareEqual(values, Vector256<uint>.AllBitsSet);

            //Take msb of each 32bit element and return them as an int.
            //Then count number of bits that are set and that is equals
            //to the number of loaded values that were all ones.
            var isMaxValueMask = Avx2.MoveMask(isMaxValue.AsSingle());
            var isMaxCount = BitOperations.PopCount((uint)isMaxValueMask);

            //For each loaded vaue that's all ones, a sectionLength
            //number of integers must also be all ones
            return isMaxCount * sectionLength;
        }


        /// <summary>
        /// https://www.reddit.com/r/csharp/comments/meem45/how_to_optimize_this_function/gsj33r9?utm_source=share&utm_medium=web2x&context=3
        /// </summary>
        /// <param name="spanByte"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public unsafe int FindFirstOctupleSearchOnce(Span<byte> spanByte)
        {
            Span<uint> span = MemoryMarshal.Cast<byte, uint>(spanByte);
            fixed (uint* spanPtr = span)
            {
                const int bitsPerInt = 32;
                const int ints = 8128 / 4;
                const int loadCount = 8;
                const int sections = loadCount + 1;
                const int sectionLength = (ints / sections) + 1; // 225.8 -> 226
                var indexes = Vector256.Create(
                    sectionLength * 1,
                    sectionLength * 2,
                    sectionLength * 3,
                    sectionLength * 4,
                    sectionLength * 5,
                    sectionLength * 6,
                    sectionLength * 7,
                    sectionLength * 8);

                int lowerBound = OctupleSearchLowerBound(spanPtr, indexes, sectionLength);
                int index = lowerBound * bitsPerInt;

                int upperBound = Math.Min(lowerBound + sectionLength + 1, span.Length);
                for (int i = lowerBound; i < upperBound; i++)
                {
                    int bitsSet = BitOperations.PopCount(span[i]);
                    index += bitsSet;
                    if (bitsSet != bitsPerInt)
                    {
                        break;
                    }
                }

                return index + 1;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public int FindFirstSearchLong(Span<byte> spanByte)
        {
            Span<ulong> span = MemoryMarshal.Cast<byte, ulong>(spanByte);

            if (span[0] == 0)
                return 1;

            if (span[^1] == long.MaxValue)
                return -1;

            int min = 0;
            int max = span.Length - 1;
            int index = -1;

            while (min <= max)
            {
                int mid = mid = (int)unchecked((uint)(min + max) >> 1);
                ref var b = ref span[mid];

                if (b != ulong.MaxValue)
                {
                    if (mid == 0)
                    {
                        index = 0;
                        break;
                    }

                    ref var b1 = ref span[mid - 1];
                    if (b1 == ulong.MaxValue)
                    {
                        index = mid;
                        break;
                    }

                    max = mid - 1;
                    continue;
                }

                min = mid + 1;
            }

            if (index > -1)
            {
                int res = 0;
                ref var l = ref span[index];
                var count = BitOperations.LeadingZeroCount((ulong)l);
                res = (64 - count) + 1;
                if (index > 0 && res != -1)
                    res += (64 * index);
                return res;
            }

            return index;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public int FindFirstSearchLong3(Span<byte> spanByte)
        {
            Span<ulong> span = MemoryMarshal.Cast<byte, ulong>(spanByte);

            if (span[0] == 0)
                return 1;

            if (span[^1] == long.MaxValue)
                return -1;

            int min = 0;
            int max = span.Length - 1;
            int index = -1;

            while (min <= max)
            {
                int mid = mid = (int)unchecked((uint)(min + max) >> 1);
                ref var b = ref span[mid];

                if (b != ulong.MaxValue)
                {
                    if (mid == 0)
                    {
                        index = 0;
                        break;
                    }

                    ref var b1 = ref span[mid - 1];
                    if (b1 == ulong.MaxValue)
                    {
                        index = mid;
                        break;
                    }

                    max = mid - 1;
                    continue;
                }

                min = mid + 1;
            }

            if (index > -1)
            {
                int res = 0;
                ref var l = ref span[index];
                res = _lookup[l];
                if (index > 0 && res != -1)
                    res += (64 * index);
                return res;
            }

            return index;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public int DefaultLoop(Span<byte> span)
        {
            Span<ulong> longSpan = MemoryMarshal.Cast<byte, ulong>(span);

            if (longSpan[0] == 0)
                return 1;

            if (longSpan[^1] == long.MaxValue)
                return -1;

            int iterCount = longSpan.Length / 4;
            for (int i = 0; i < iterCount; i++)
            {
                ref ulong l = ref longSpan[i];
                if (l == ulong.MaxValue)
                    continue;
                int res = 0;
                var count = BitOperations.LeadingZeroCount(l);
                res = (64 - count) + 1;
                if (i > 0 && res != -1)
                    res += (64 * i);
                return res;
            }

            return -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public int FindFirstEmptyUnroll8(Span<byte> span)
        {
            Span<ulong> longSpan = MemoryMarshal.Cast<byte, ulong>(span);

            if (longSpan[0] == 0)
                return 1;

            if (longSpan[^1] == long.MaxValue)
                return -1;

            int iterCount = longSpan.Length;
            for (int i = 0; i < iterCount; i += 8)
            {
                ref ulong l8 = ref longSpan[i + 7];
                // when l4 is max value all others before too
                if (l8 == ulong.MaxValue)
                    continue;

                ref ulong l1 = ref longSpan[i];
                ref ulong l2 = ref longSpan[i + 1];
                ref ulong l3 = ref longSpan[i + 2];
                ref ulong l4 = ref longSpan[i + 3];
                ref ulong l5 = ref longSpan[i + 4];
                ref ulong l6 = ref longSpan[i + 5];
                ref ulong l7 = ref longSpan[i + 6];

                int mult = i + 1;
                int res = -1;
                if (l1 != ulong.MaxValue)
                {
                    var count = BitOperations.LeadingZeroCount(l1);

                    res = (64 - count) + 1;
                }
                else if (l2 != ulong.MaxValue)
                {
                    var count = BitOperations.LeadingZeroCount(l2);
                    res = (64) - count + 64 + 1;
                }
                else if (l3 != ulong.MaxValue)
                {
                    var count = BitOperations.LeadingZeroCount(l3);
                    res = (64) - count + 128 + 1;
                }
                else if (l4 != ulong.MaxValue)
                {
                    var count = BitOperations.LeadingZeroCount(l4);
                    res = (64) - count + 192 + 1;
                }
                else if (l5 != ulong.MaxValue)
                {
                    var count = BitOperations.LeadingZeroCount(l4);
                    res = (64) - count + 256 + 1;
                }
                else if (l6 != ulong.MaxValue)
                {
                    var count = BitOperations.LeadingZeroCount(l4);
                    res = (64) - count + 320 + 1;
                }
                else if (l7 != ulong.MaxValue)
                {
                    var count = BitOperations.LeadingZeroCount(l4);
                    res = (64) - count + 384 + 1;
                }
                else if (l8 != ulong.MaxValue)
                {
                    var count = BitOperations.LeadingZeroCount(l4);
                    res = (64) - count + 448 + 1;
                }

                if (i > 0 && res != -1)
                    res += (64 * i);

                return res;
            }

            return -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public int FindFirstEmptyUnroll4(Span<byte> span)
        {
            Span<ulong> longSpan = MemoryMarshal.Cast<byte, ulong>(span);

            if (longSpan[0] == 0)
                return 1;

            if (longSpan[^1] == long.MaxValue)
                return -1;

            int iterCount = longSpan.Length;
            for (int i = 0; i < iterCount; i += 4)
            {
                ref ulong l4 = ref longSpan[i + 3];
                // when l4 is max value all others before too
                if (l4 == ulong.MaxValue)
                    continue;

                ref ulong l1 = ref longSpan[i];
                ref ulong l2 = ref longSpan[i + 1];
                ref ulong l3 = ref longSpan[i + 2];

                int mult = i + 1;
                int res = -1;
                if (l1 != ulong.MaxValue)
                {
                    var count = BitOperations.LeadingZeroCount(l1);

                    res = (64 - count) + 1;
                }
                else if (l2 != ulong.MaxValue)
                {
                    var count = BitOperations.LeadingZeroCount(l2);
                    res = (64) - count + 64 + 1;
                }
                else if (l3 != ulong.MaxValue)
                {
                    var count = BitOperations.LeadingZeroCount(l3);
                    res = (64) - count + 128 + 1;
                }
                else if (l4 != ulong.MaxValue)
                {
                    var count = BitOperations.LeadingZeroCount(l4);
                    res = (64) - count + 192 + 1;
                }

                if (i > 0 && res != -1)
                    res += (64 * i);

                return res;
            }

            return -1;
        }


        private static Vector<byte> _testVector;
        private static Dictionary<ulong, int> _lookup;

        static FindFirstEmptyPageBench()
        {
            List<byte> list = new List<byte>();
            for (int i = 0; i < Vector<byte>.Count; i++)
            {
                list.Add(0xFF);
            }

            _testVector = new Vector<byte>(list.ToArray());

            Dictionary<ulong, int> dictionary = new Dictionary<ulong, int>();
            for (int i = 0; i < 64; i++)
            {
                ulong n = 0;
                for (int j = 0; j < i; j++)
                {
                    n = (ulong)(n | (1ul << j));
                }

                Debug.WriteLine(i);
                Debug.WriteLine(n);

                if (!dictionary.ContainsKey(n))
                    dictionary.Add(i > 0 ? n : ulong.MaxValue, i > 0 ? 64 - i + 1 : 1);
            }
            dictionary.Add(0, 1);
            _lookup = dictionary;
        }

        /// <summary>
        /// https://www.reddit.com/r/csharp/comments/meem45/how_to_optimize_this_function/gsi79qf?utm_source=share&utm_medium=web2x&context=3
        /// </summary>
        /// <param name="span"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public int FindFirstEmptyVectorT(Span<byte> span)
        {
            int iterations = Math.DivRem(span.Length, Vector<byte>.Count, out int nonAligned);
            for (int i = 0; i < iterations; i++)
            {
                var vector = new Vector<byte>(span[(Vector<byte>.Count * i)..]);
                if (vector != _testVector)
                {
                    int bitIndex = Vector<byte>.Count * i * 8;
                    var u64vector = Vector.AsVectorUInt64(vector); // handle LZC with uiint here
                    for (int j = 0; j < Vector<ulong>.Count; j++)
                    {
                        var l = u64vector[j];
                        if (l == ulong.MaxValue)
                            continue;

                        int res = 0;
                        var count = BitOperations.LeadingZeroCount((ulong)l);
                        res = (64 - count) + 1;
                        if (j > 0 && res != -1)
                            res += (64 * j);
                        return res + bitIndex;
                    }
                }
            }

            return -1;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public int FindFirstEmptyUnroll2(Span<byte> span)
        {
            Span<ulong> longSpan = MemoryMarshal.Cast<byte, ulong>(span);

            if (longSpan[0] == 0)
                return 1;

            if (longSpan[^1] == long.MaxValue)
                return -1;

            int iterCount = longSpan.Length;
            for (int i = 0; i < iterCount; i += 2)
            {
                ref ulong l2 = ref longSpan[i + 1];
                // when l4 is max value all others before too
                if (l2 == ulong.MaxValue)
                    continue;

                ref ulong l1 = ref longSpan[i];

                int res = -1;
                if (l1 != ulong.MaxValue)
                {
                    var count = BitOperations.LeadingZeroCount(l1);

                    res = (64 - count) + 1;
                }
                else if (l2 != ulong.MaxValue)
                {
                    var count = BitOperations.LeadingZeroCount(l2);
                    res = (64) - count + 64 + 1;
                }

                if (i > 0 && res != -1)
                    res += (64 * i);

                return res;
            }

            return -1;
        }

    }
}
