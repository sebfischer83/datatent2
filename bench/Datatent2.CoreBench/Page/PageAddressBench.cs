using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Bogus.Extensions;
using Datatent2.Core;
using Datatent2.Core.Page;
using Microsoft.Diagnostics.Runtime.ICorDebug;

namespace Datatent2.CoreBench.Page
{
    [HtmlExporter, CsvExporter(), CsvMeasurementsExporter(),
     RankColumn(), KurtosisColumn, SkewnessColumn, StdDevColumn, MeanColumn, MedianColumn, BaselineColumn, MediumRunJob, MemoryDiagnoser, Orderer(SummaryOrderPolicy.FastestToSlowest, MethodOrderPolicy.Declared)]
    public class PageAddressBench
    {
        [Params(100, 1000, 5000)]
        public int Count;

        private PageAddress[] _pageAddresses;
        private byte[] _array;

        [GlobalSetup]
        public void GlobalSetup()
        {
            Span<byte> array = new byte[Constants.PAGE_ADDRESS_SIZE * Count];

            _pageAddresses = new PageAddress[Count];

            Random random = new Random();
            for (int i = 0; i < Count; i++)
            {
                PageAddress address = new PageAddress((uint) random.Next(0, int.MaxValue), (byte)random.Next(0, byte.MaxValue));
                _pageAddresses[i] = address;
                MemoryMarshal.Write(array.Slice((i * Constants.PAGE_ADDRESS_SIZE)), ref address);
            }

            _array = array.ToArray();
        }

        [Benchmark]
        public int ReadFromBytes()
        {
            int a = 0;
            for (int i = 0; i < Count; i++)
            {
                var page = PageAddress.FromBuffer(new Span<byte>(_array).Slice(i * Constants.PAGE_ADDRESS_SIZE));
                a += page.DirectoryEntryId;
            }

            return a;
        }

        [Benchmark]
        public int WriteToBytes()
        {
            int a = 0;
            for (int i = 0; i < Count; i++)
            {
                ref var address = ref _pageAddresses[i];
                address.ToBuffer(new Span<byte>(_array).Slice(i * Constants.PAGE_ADDRESS_SIZE));
                a += address.DirectoryEntryId;
            }

            return a;
        }
    }
}
/*
 ``` ini

BenchmarkDotNet=v0.12.1, OS=Windows 10.0.19042
AMD Ryzen 7 3700X, 1 CPU, 16 logical and 8 physical cores
.NET Core SDK=5.0.200-preview.21079.7
  [Host]    : .NET Core 5.0.3 (CoreCLR 5.0.321.7212, CoreFX 5.0.321.7212), X64 RyuJIT
  MediumRun : .NET Core 5.0.3 (CoreCLR 5.0.321.7212, CoreFX 5.0.321.7212), X64 RyuJIT

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
|        Method | Count |        Mean |     Error |      StdDev |      Median | Kurtosis | Skewness | Rank | Baseline | Gen 0 | Gen 1 | Gen 2 | Allocated |
|-------------- |------ |------------:|----------:|------------:|------------:|---------:|---------:|-----:|--------- |------:|------:|------:|----------:|
| ReadFromBytes |   100 |    106.9 ns |   0.83 ns |     1.24 ns |    106.8 ns |    2.720 |   0.3775 |    1 |       No |     - |     - |     - |         - |
|  WriteToBytes |   100 |    535.7 ns |   2.37 ns |     3.47 ns |    535.8 ns |    1.899 |   0.2822 |    2 |       No |     - |     - |     - |         - |
| ReadFromBytes |  1000 |    997.5 ns |   6.55 ns |     9.61 ns |    995.1 ns |    1.583 |   0.2541 |    3 |       No |     - |     - |     - |         - |
| ReadFromBytes |  5000 |  4,975.3 ns |  18.63 ns |    27.88 ns |  4,984.4 ns |    2.949 |  -0.9318 |    4 |       No |     - |     - |     - |         - |
|  WriteToBytes |  1000 |  5,264.7 ns |  21.84 ns |    31.32 ns |  5,261.4 ns |    2.029 |   0.3164 |    5 |       No |     - |     - |     - |         - |
|  WriteToBytes |  5000 | 25,280.8 ns | 706.56 ns | 1,035.67 ns | 26,025.0 ns |    1.075 |  -0.1242 |    6 |       No |     - |     - |     - |         - |

 



*/