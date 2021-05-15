using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Datatent2.Contracts;
using Datatent2.Core;
using Datatent2.Core.Memory;
using Datatent2.Core.Page;
using Datatent2.Core.Page.AllocationInformation;
using Datatent2.Core.Page.Data;
using Moq;

namespace Datatent2.CoreBench.Page
{
    [HtmlExporter, CsvExporter(), CsvMeasurementsExporter(),
     AllStatisticsColumn(), MediumRunJob, MemoryDiagnoser]
    public class AllocationInformationPageBench
    {
        private IPage[] _pages;
        private AllocationInformationPage _allocationInformationPage;

        [GlobalSetup]
        public void Setup()
        {
            IBufferSegment segment = new BufferSegment(Constants.PAGE_SIZE);
            IPage[] pages = new IPage[AllocationInformationPage.ENTRIES_PER_PAGE];
            for (int i = 1; i < AllocationInformationPage.ENTRIES_PER_PAGE; i++)
            {
                var page = new DataPage(segment, (uint)(2 + i));
                pages[i] = (page);
            }

            _pages = pages;

            IBufferSegment bufferSegment = new BufferSegment(Constants.PAGE_SIZE);
            _allocationInformationPage = new AllocationInformationPage(bufferSegment, 2);
        }

        [IterationSetup]
        public void TestSetup()
        {
            _allocationInformationPage.PageBuffer.Span.Slice(Constants.PAGE_HEADER_SIZE).Clear();
        }
        
        [Benchmark(OperationsPerInvoke = AllocationInformationPage.ENTRIES_PER_PAGE)]
        public uint AddAllocationsBench()
        {
            uint a = 0;
            for (int i = 1; i < AllocationInformationPage.ENTRIES_PER_PAGE; i++)
            {
                _allocationInformationPage.AddAllocationInformation(_pages[i]);
                a = _pages[1].Id;
            }

            return a;
        }
    }
}

/*
 *
 BenchmarkDotNet=v0.12.1, OS=Windows 10.0.19042
AMD Ryzen 7 3700X, 1 CPU, 16 logical and 8 physical cores
.NET Core SDK=5.0.201
  [Host]    : .NET Core 5.0.4 (CoreCLR 5.0.421.11614, CoreFX 5.0.421.11614), X64 RyuJIT
  MediumRun : .NET Core 5.0.4 (CoreCLR 5.0.421.11614, CoreFX 5.0.421.11614), X64 RyuJIT

Job=MediumRun  InvocationCount=1  IterationCount=15
LaunchCount=2  UnrollFactor=1  WarmupCount=10

|              Method |     Mean |    Error |   StdDev |   StdErr |      Min |       Q1 |   Median |       Q3 |      Max |         Op/s | Gen 0 | Gen 1 | Gen 2 | Allocated |
|-------------------- |---------:|---------:|---------:|---------:|---------:|---------:|---------:|---------:|---------:|-------------:|------:|------:|------:|----------:|
| AddAllocationsBench | 80.73 ns | 1.430 ns | 2.140 ns | 0.391 ns | 77.85 ns | 78.72 ns | 80.46 ns | 81.67 ns | 85.83 ns | 12,387,726.1 |     - |     - |     - |         - |
 */