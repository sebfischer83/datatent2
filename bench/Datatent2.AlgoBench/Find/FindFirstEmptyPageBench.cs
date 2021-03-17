using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Diagnostics.Windows.Configs;
using BenchmarkDotNet.Order;

namespace Datatent2.AlgoBench.Find
{
    [HtmlExporter, CsvExporter(), CsvMeasurementsExporter(), MarkdownExporter, RPlotExporter, 
     MeanColumn, MedianColumn, MediumRunJob, EtwProfiler(), DisassemblyDiagnoser(1, true, true, true,
         true, true, true), EventPipeProfiler(EventPipeProfile.CpuSampling)]
    public class FindFirstEmptyPageBench
    {
        [Params(250, 500, 1000, 2500, 5000)]
        public int Iterations { get; set; }
        private static readonly object _lock = new object();
        private static SpinLock _spinlock = new SpinLock();
        byte[][] buffers;

        [GlobalSetup]
        public void Setup()
        {
            List<byte[]> list = new List<byte[]>(Iterations);
            Random random = new Random();
            for (int i = 0; i < Iterations; i++)
            {
                var b = new byte[8128];
                for (int j = 0; j < random.Next(0, 8000); j++)
                {
                    b[j] = 0xFF;
                }
                list.Add(b);
            }

            buffers = list.ToArray();
        }

        [Benchmark()]
        public long FindDefault()
        {
            long l = 0;
            for (int i = 0; i < Iterations; i++)
            {
                l += FindFirstEmptyDefault(buffers[i]);
            }

            return l;
        }

        //[Benchmark()]
        //public long FindDefaultParallel()
        //{
        //    long l = 0;
        //    Parallel.For(0, Iterations, i =>
        //    {
        //        l += FindFirstEmptyDefault(buffers[i]);
        //    });

        //    return l;
        //}

        [Benchmark()]
        public long FindUnrolled4()
        {
            long l = 0;
            for (int i = 0; i < Iterations; i++)
            {
                l += FindFirstEmptyUnroll4(buffers[i]);
            }

            return l;
        }

        [Benchmark()]
        public long FindUnrolled2()
        {
            long l = 0;
            for (int i = 0; i < Iterations; i++)
            {
                l += FindFirstEmptyUnroll2(buffers[i]);
            }

            return l;
        }

        [Benchmark()]
        public long FindUnrolled8()
        {
            long l = 0;
            for (int i = 0; i < Iterations; i++)
            {
                l += FindFirstEmptyUnroll8(buffers[i]);
            }

            return l;
        }

        //[Benchmark()]
        //public long FindUnrolledParallel()
        //{
        //    long l = 0;
        //    Parallel.For(0, Iterations, i =>
        //    {
        //        l += FindFirstEmptyUnroll4(buffers[i]);
        //    });

        //    return l;
        //}

        //[Benchmark()]
        //public long FindDefaultLock()
        //{
        //    lock (_lock)
        //    {
        //        long l = 0;
        //        for (int i = 0; i < Iterations; i++)
        //        {
        //            l += FindFirstEmptyDefault(buffers[i]);
        //        }

        //        return l;
        //    }
        //}

        //[Benchmark()]
        //public long FindUnrolledLock()
        //{
        //    lock (_lock)
        //    {
        //        long l = 0;
        //        for (int i = 0; i < Iterations; i++)
        //        {
        //            l += FindFirstEmptyUnroll4(buffers[i]);
        //        }

        //        return l;
        //    }
        //}



        //[Benchmark()]
        //public long FindDefaultSpinlock()
        //{
        //    bool lockTaken = false;
        //    try
        //    {
        //        _spinlock.Enter(ref lockTaken);
        //        long l = 0;
        //        for (int i = 0; i < Iterations; i++)
        //        {
        //            l += FindFirstEmptyDefault(buffers[i]);
        //        }

        //        return l;
        //    }
        //    finally
        //    {
        //        _spinlock.Exit();
        //    }
        //}

        //[Benchmark()]
        //public long FindUnrolledSpinlock()
        //{
        //    bool lockTaken = false;
        //    try
        //    {
        //        _spinlock.Enter(ref lockTaken);
        //        long l = 0;
        //        for (int i = 0; i < Iterations; i++)
        //        {
        //            l += FindFirstEmptyUnroll4(buffers[i]);
        //        }

        //        return l;
        //    }
        //    finally
        //    {
        //        _spinlock.Exit();
        //    }

        //}


        //[Benchmark()]
        //public long FindDefaultSpinlockParallel()
        //{
        //    bool lockTaken = false;
        //    try
        //    {
        //        _spinlock.Enter(ref lockTaken);
        //        long l = 0;
        //        Parallel.For(0, Iterations, i =>
        //        {
        //            l += FindFirstEmptyDefault(buffers[i]);
        //        });

        //        return l;
        //    }
        //    finally
        //    {
        //        _spinlock.Exit();
        //    }
        //}

        //[Benchmark()]
        //public long FindUnrolledSpinlockParallel()
        //{
        //    bool lockTaken = false;
        //    try
        //    {
        //        _spinlock.Enter(ref lockTaken);
        //        long l = 0;
        //        Parallel.For(0, Iterations, i =>
        //        {
        //            l += FindFirstEmptyUnroll4(buffers[i]);
        //        });

        //        return l;
        //    }
        //    finally
        //    {
        //        _spinlock.Exit();
        //    }

        //}

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public int FindFirstEmptyDefault(Span<byte> span)
        {
            Span<ulong> longSpan = MemoryMarshal.Cast<byte, ulong>(span);

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

            var lastItem = longSpan[^1];
            if (lastItem == ulong.MaxValue)
            {
                return -1;
            }

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

            var lastItem = longSpan[^1];
            if (lastItem == ulong.MaxValue)
            {
                return -1;
            }

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

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public int FindFirstEmptyUnroll2(Span<byte> span)
        {
            Span<ulong> longSpan = MemoryMarshal.Cast<byte, ulong>(span);

            var lastItem = longSpan[^1];
            if (lastItem == ulong.MaxValue)
            {
                return -1;
            }

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
