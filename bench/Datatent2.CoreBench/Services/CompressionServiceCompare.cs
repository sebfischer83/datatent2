using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Bogus.DataSets;
using Datatent2.Core;
using Datatent2.Core.Services.Compression;

namespace Datatent2.CoreBench.Services
{
    [HtmlExporter, CsvExporter(), CsvMeasurementsExporter(),
     RankColumn(), KurtosisColumn, SkewnessColumn, StdDevColumn, MeanColumn, MedianColumn, BaselineColumn, MediumRunJob, MemoryDiagnoser, Orderer(SummaryOrderPolicy.FastestToSlowest, MethodOrderPolicy.Declared)]
    public unsafe class CompressionServiceCompare
    {
        private BrotliCompressionService _brotliCompressionService;
        private byte[] _bytesOrg;
        private Lz4CompressionService _lz4CompressionService;
        private NopCompressionService _nopCompressionService;

        [GlobalSetup]
        public void GlobalSetup()
        {
            _brotliCompressionService = new BrotliCompressionService();
            _lz4CompressionService = new Lz4CompressionService();
            _nopCompressionService = new NopCompressionService();
            var bogus = new Bogus.Randomizer();
            _bytesOrg = Encoding.UTF8.GetBytes(new Lorem().Sentence(500));

            var buffer = ArrayPool<byte>.Shared.Rent(Constants.MAX_USABLE_BYTES_IN_PAGE + 500);
            ArrayPool<byte>.Shared.Return(buffer);
        }

        [Benchmark]
        public int BrotliServiceArrayPool()
        {
            var buffer = ArrayPool<byte>.Shared.Rent(Constants.MAX_USABLE_BYTES_IN_PAGE + 500);
            var res = _brotliCompressionService.Compress(_bytesOrg,
                buffer);

            var r = res.Length;
            ArrayPool<byte>.Shared.Return(buffer);
            return r;
        }

        [Benchmark]
        public int BrotliServiceStackalloc()
        {
            Span<byte> buffer = stackalloc byte[Constants.MAX_USABLE_BYTES_IN_PAGE + 500];
            var res = _brotliCompressionService.Compress(_bytesOrg,
                buffer);

            var r = res.Length;
            return r;
        }

        [Benchmark]
        [SkipLocalsInit]
        public int BrotliServiceStackallocNoLocalsInit()
        {
            Span<byte> buffer = stackalloc byte[Constants.MAX_USABLE_BYTES_IN_PAGE + 500];
            var res = _brotliCompressionService.Compress(_bytesOrg,
                buffer);

            var r = res.Length;
            return r;
        }

        [Benchmark]
        public int Lz4ServiceArrayPool()
        {
            var buffer = ArrayPool<byte>.Shared.Rent(Constants.MAX_USABLE_BYTES_IN_PAGE + 500);
            var res = _lz4CompressionService.Compress(_bytesOrg,
                buffer);

            var r = res.Length;
            ArrayPool<byte>.Shared.Return(buffer);
            return r;
        }

        [Benchmark]
        public int Lz4ServiceStackalloc()
        {
            Span<byte> buffer = stackalloc byte[Constants.MAX_USABLE_BYTES_IN_PAGE + 500];
            var res = _lz4CompressionService.Compress(_bytesOrg,
                buffer);

            var r = res.Length;
            return r;
        }

        [Benchmark]
        [SkipLocalsInit]
        public int Lz4ServiceStackallocNoLocalsInit()
        {
            Span<byte> buffer = stackalloc byte[Constants.MAX_USABLE_BYTES_IN_PAGE + 500];
            var res = _lz4CompressionService.Compress(_bytesOrg,
                buffer);

            var r = res.Length;
            return r;
        }

        [Benchmark]
        public int NopServiceArrayPool()
        {
            var buffer = ArrayPool<byte>.Shared.Rent(Constants.MAX_USABLE_BYTES_IN_PAGE + 500);
            var res = _nopCompressionService.Compress(_bytesOrg,
                buffer);

            var r = res.Length;
            ArrayPool<byte>.Shared.Return(buffer);
            return r;
        }


        [Benchmark]
        public int NopServiceStackalloc()
        {
            Span<byte> buffer = stackalloc byte[Constants.MAX_USABLE_BYTES_IN_PAGE + 500];
            var res = _nopCompressionService.Compress(_bytesOrg,
                buffer);

            var r = res.Length;
            return r;
        }

        [Benchmark]
        [SkipLocalsInit]
        public int NopServiceStackallocNoLocalsInit()
        {
            Span<byte> buffer = stackalloc byte[Constants.MAX_USABLE_BYTES_IN_PAGE + 500];
            var res = _nopCompressionService.Compress(_bytesOrg,
                buffer);

            var r = res.Length;
            return r;
        }
    }
}

/*
 *``` ini

BenchmarkDotNet=v0.12.1, OS=Windows 10.0.19042
AMD Ryzen 7 3700X, 1 CPU, 16 logical and 8 physical cores
.NET Core SDK=5.0.200-preview.21079.7
  [Host]    : .NET Core 5.0.3 (CoreCLR 5.0.321.7212, CoreFX 5.0.321.7212), X64 RyuJIT
  MediumRun : .NET Core 5.0.3 (CoreCLR 5.0.321.7212, CoreFX 5.0.321.7212), X64 RyuJIT

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
|                              Method |         Mean |        Error |       StdDev |       Median | Kurtosis | Skewness | Rank | Baseline |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|------------------------------------ |-------------:|-------------:|-------------:|-------------:|---------:|---------:|-----:|--------- |-------:|------:|------:|----------:|
|    NopServiceStackallocNoLocalsInit |     49.90 ns |     0.398 ns |     0.558 ns |     50.18 ns |    1.123 |   0.0189 |    1 |       No |      - |     - |     - |         - |
|                 NopServiceArrayPool |     72.21 ns |     0.240 ns |     0.359 ns |     72.30 ns |    1.961 |   0.0740 |    2 |       No |      - |     - |     - |         - |
|                NopServiceStackalloc |    308.24 ns |     1.526 ns |     2.237 ns |    307.16 ns |    1.934 |   0.2808 |    3 |       No |      - |     - |     - |         - |
|    Lz4ServiceStackallocNoLocalsInit | 12,928.29 ns |    76.923 ns |   115.134 ns | 12,911.35 ns |    2.042 |   0.4732 |    4 |       No | 0.2441 |     - |     - |    2072 B |
|                Lz4ServiceStackalloc | 13,203.40 ns |    80.540 ns |   120.549 ns | 13,175.16 ns |    1.936 |   0.3593 |    5 |       No | 0.2289 |     - |     - |    2000 B |
|                 Lz4ServiceArrayPool | 13,521.50 ns |   207.057 ns |   303.502 ns | 13,660.08 ns |    1.458 |  -0.0167 |    6 |       No | 0.2441 |     - |     - |    2104 B |
|             BrotliServiceStackalloc | 35,516.92 ns | 1,430.663 ns | 2,097.048 ns | 34,948.26 ns |    1.648 |  -0.1356 |    7 |       No |      - |     - |     - |         - |
|              BrotliServiceArrayPool | 37,060.84 ns |   497.874 ns |   714.036 ns | 37,046.68 ns |    1.505 |   0.2286 |    8 |       No |      - |     - |     - |         - |
| BrotliServiceStackallocNoLocalsInit | 37,549.58 ns | 1,445.926 ns | 2,164.194 ns | 37,702.27 ns |    1.185 |   0.0056 |    8 |       No |      - |     - |     - |         - |
 *
 */