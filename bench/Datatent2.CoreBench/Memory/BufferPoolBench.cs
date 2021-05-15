using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Datatent2.Contracts;
using Datatent2.Core;
using Datatent2.Core.Memory;

namespace Datatent2.CoreBench.Memory
{
    [HtmlExporter, CsvExporter(), CsvMeasurementsExporter(),
     RankColumn(), KurtosisColumn, SkewnessColumn, StdDevColumn, MeanColumn, MedianColumn, BaselineColumn, MemoryDiagnoser, MediumRunJob]
    public class BufferPoolBench
    {
        const int Iterations = 8000;

        [GlobalSetup]
        public void Setup()
        {
            var rented = UnmanagedBufferPool.Shared.Rent();
            rented.Dispose();

            List<byte[]> list = new List<byte[]>(Iterations);
            for (int j = 0; j < Iterations; j++)
            {
                var a = ArrayPool<byte>.Shared.Rent(Constants.PAGE_SIZE);

                list.Add(a);
            }

            foreach (var b in list)
            {
                ArrayPool<byte>.Shared.Return(b, true);
            }

            List<IBufferSegment> list2 = new(Iterations);
            for (int j = 0; j < Iterations; j++)
            {
                var b = Core.Memory.BufferPool.Shared.Rent(Constants.PAGE_SIZE);
                
                list2.Add(b);
            }
            foreach (var b in list2)
            {
                b.Dispose();
            }
        }

        [Benchmark(Baseline = true, OperationsPerInvoke = Iterations)]
        public int ArrayPool()
        {
            int i = 0;
            List<byte[]> list = new List<byte[]>(Iterations);
            for (int j = 0; j < Iterations; j++)
            {
                var rented = ArrayPool<byte>.Shared.Rent(Constants.PAGE_SIZE);

                i = rented.Length;
                list.Add(rented);
            }

            foreach (var b in list)
            {
                ArrayPool<byte>.Shared.Return(b, true);
            }

            return i;
        }

        [Benchmark(OperationsPerInvoke = Iterations)]
        public uint BufferPool()
        {
            uint i = 0;
            List<IBufferSegment> list = new (Iterations);
            for (int j = 0; j < Iterations; j++)
            {
                var rented = Core.Memory.BufferPool.Shared.Rent(Constants.PAGE_SIZE);

                i = rented.Length;
                list.Add(rented);
            }
            foreach (var b in list)
            {
                b.Dispose();
            }

            return i;

        }

        [Benchmark(OperationsPerInvoke = Iterations)]
        public uint UnmanagedBufferedPool()
        {
            uint i = 0;
            List<IBufferSegment> list = new(Iterations);
            for (int j = 0; j < Iterations; j++)
            {
                var rented = UnmanagedBufferPool.Shared.Rent(Constants.PAGE_SIZE);

                i = rented.Length;
                list.Add(rented);
            }
            foreach (var b in list)
            {
                b.Dispose();
            }
            return i;
        }
    }
}
