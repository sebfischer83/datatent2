using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Datatent2.Contracts;
using Datatent2.Contracts.Exceptions;
using Datatent2.Core.Memory;
using Datatent2.Core.Page.GlobalAllocationMap;

namespace Datatent2.CoreBench.Page
{
    [HtmlExporter, CsvExporter(), CsvMeasurementsExporter(),
     RankColumn(), KurtosisColumn, SkewnessColumn, StdDevColumn, MeanColumn, MedianColumn, BaselineColumn, MediumRunJob, MemoryDiagnoser]
    public class GlobalAllocationMapBench
    {
        public uint Id = 1;
        internal IBufferSegment Buffer;
        private const int COUNT = 8000;

        [GlobalSetup]
        public void Setup()
        {
            IBufferSegment bufferSegment = new BufferSegment(Constants.PAGE_SIZE);
            GlobalAllocationMapPage globalAllocationMapPage = new GlobalAllocationMapPage(bufferSegment);
            Buffer = bufferSegment;
            for (int i = 0; i < COUNT; i++)
            {
                globalAllocationMapPage.AcquirePageId();
            }
        }

        [Benchmark(Baseline = true, OperationsPerInvoke = COUNT)]
        public long Test1()
        {
            long l = 0;
            for (int i = 1; i < COUNT; i++)
            {
                l += IsAllocated((uint) i) ? 2 : 1;
            }

            return l;
        }

        [Benchmark(Baseline = false, OperationsPerInvoke = COUNT)]
        public long Test2()
        {
            long l = 0;
            for (int i = 1; i < COUNT; i++)
            {
                l += IsAllocated2((uint)i) ? 2 : 1;
            }

            return l;
        }

        public bool IsAllocated(uint pageId)
        {
            int localId = (int)(pageId - Id);
            if (localId < 0)
                throw new InvalidPageException($"The page {pageId} is not part of the GAM {Id}!", pageId);

            // it's me
            if (localId == 0)
                return true;
            var dataBuffer = Buffer.Span[Constants.PAGE_HEADER_SIZE..];
            if (localId < 9)
            {
                ref byte b = ref dataBuffer[0];
                var res = b & (1 << (localId - 1));
                return res != 0;
            }

            var bytePos = Math.DivRem(localId, 8, out var remainder);
            if (remainder > 0)
            {
                ref byte b2 = ref dataBuffer[bytePos];
                var res = (b2 & (1 << remainder - 1));
                return res != 0;
            }
            else
            {
                // when remainder == 0, we need to set the last bit of the byte before
                ref byte b2 = ref dataBuffer[bytePos - 1];
                var res = (b2 & (1 << 7));
                return res != 0;
            }
        }

        public bool IsAllocated2(uint pageId)
        {
            int localId = (int)(pageId - Id);
            if (localId < 0)
                throw new InvalidPageException($"The page {pageId} is not part of the GAM {Id}!", pageId);
            var longBuffer = MemoryMarshal.Cast<byte, ulong>(Buffer.Span);

            // it's me
            if (localId == 0)
                return true;
            var dataBuffer = Buffer.Span[Constants.PAGE_HEADER_SIZE..];
            if (localId < 9)
            {
                ref byte b = ref dataBuffer[0];
                var res = b & (1 << (localId - 1));
                return res != 0;
            }

            var bytePos = Math.DivRem(localId, 64, out var remainder);
            if (remainder > 0)
            {
                ref ulong b2 = ref longBuffer[bytePos];
                return (remainder - 1) == BitOperations.PopCount(b2);
            }
            else
            {
                // when remainder == 0, we need to set the last bit of the byte before
                ref ulong b2 = ref longBuffer[bytePos];

                return (63) == BitOperations.PopCount(b2);
            }
        }
    }
}
