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
using BenchmarkDotNet.Diagnostics.Windows.Configs;
using BenchmarkDotNet.Order;

namespace Datatent2.AlgoBench.Find
{
    [HtmlExporter, CsvExporter(), CsvMeasurementsExporter(),
     RankColumn(), KurtosisColumn, SkewnessColumn, StdDevColumn, MeanColumn, MedianColumn, BaselineColumn, MediumRunJob, MemoryDiagnoser]
    public class FindFirstEmptyPageBench
    {
        const int Iterations = 80000;
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

        [Benchmark()]
        public long FindDefaultParallel()
        {
            long l = 0;
            Parallel.For(0, Iterations, i =>
            {
                l += FindFirstEmptyDefault(buffers[i]);
            });

            return l;
        }

        [Benchmark()]
        public long FindUnrolled()
        {
            long l = 0;
            for (int i = 0; i < Iterations; i++)
            {
                l += FindFirstEmptyUnroll4(buffers[i]);
            }

            return l;
        }

        [Benchmark()]
        public long FindUnrolledParallel()
        {
            long l = 0;
            Parallel.For(0, Iterations, i =>
            {
                l += FindFirstEmptyUnroll4(buffers[i]);
            });

            return l;
        }

        [Benchmark()]
        public long FindDefaultLock()
        {
            lock (_lock)
            {
                long l = 0;
                for (int i = 0; i < Iterations; i++)
                {
                    l += FindFirstEmptyDefault(buffers[i]);
                }

                return l;
            }
        }

        [Benchmark()]
        public long FindUnrolledLock()
        {
            lock (_lock)
            {
                long l = 0;
                for (int i = 0; i < Iterations; i++)
                {
                    l += FindFirstEmptyUnroll4(buffers[i]);
                }

                return l;
            }
        }



        [Benchmark()]
        public long FindDefaultSpinlock()
        {
            bool lockTaken = false;
            try
            {
                _spinlock.Enter(ref lockTaken);
                long l = 0;
                for (int i = 0; i < Iterations; i++)
                {
                    l += FindFirstEmptyDefault(buffers[i]);
                }

                return l;
            }
            finally
            {
                _spinlock.Exit();
            }
        }

        [Benchmark()]
        public long FindUnrolledSpinlock()
        {
            bool lockTaken = false;
            try
            {
                _spinlock.Enter(ref lockTaken);
                long l = 0;
                for (int i = 0; i < Iterations; i++)
                {
                    l += FindFirstEmptyUnroll4(buffers[i]);
                }

                return l;
            }
            finally
            {
                _spinlock.Exit();
            }

        }


        [Benchmark()]
        public long FindDefaultSpinlockParallel()
        {
            bool lockTaken = false;
            try
            {
                _spinlock.Enter(ref lockTaken);
                long l = 0;
                Parallel.For(0, Iterations, i =>
                {
                    l += FindFirstEmptyDefault(buffers[i]);
                });

                return l;
            }
            finally
            {
                _spinlock.Exit();
            }
        }

        [Benchmark()]
        public long FindUnrolledSpinlockParallel()
        {
            bool lockTaken = false;
            try
            {
                _spinlock.Enter(ref lockTaken);
                long l = 0;
                Parallel.For(0, Iterations, i =>
                {
                    l += FindFirstEmptyUnroll4(buffers[i]);
                });

                return l;
            }
            finally
            {
                _spinlock.Exit();
            }

        }

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
                int mult = i + 1;
                int res = -1;
                var count = BitOperations.LeadingZeroCount(l);
                res = (64 - count) * (mult) + 1;
                if (i > 0)
                    res += (64 * i);
                return res;
            }

            return -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public int FindFirstEmptyUnroll4(Span<byte> span)
        {
            Span<ulong> longSpan = MemoryMarshal.Cast<byte, ulong>(span);

            int iterCount = longSpan.Length / 4;
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
                    res = (64 - count) * (mult) + 1;
                }
                else if (l2 != ulong.MaxValue)
                {
                    var count = BitOperations.LeadingZeroCount(l2);
                    res = (64 * mult) - count * (mult) + 64 + 1;
                }
                else if (l3 != ulong.MaxValue)
                {
                    var count = BitOperations.LeadingZeroCount(l3);
                    res = (64 * mult) - count * (mult) + 128 + 1;
                }
                else if (l4 != ulong.MaxValue)
                {
                    var count = BitOperations.LeadingZeroCount(l4);
                    res = (64 * mult) - count * (mult) + 192 + 1;
                }

                if (i > 0)
                    res += (64 * i);

                return res;
            }

            return -1;
        }

    }
}
