using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Datatent2.Contracts;
using Datatent2.Core;
using Datatent2.Core.IO;
using Datatent2.Core.Memory;
using Datatent2.Core.Page;
using DotNext;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;

namespace Datatent2.CoreBench.IO
{
    [HtmlExporter, CsvExporter(), CsvMeasurementsExporter(),
     RankColumn(), KurtosisColumn, SkewnessColumn, StdDevColumn, MeanColumn, MedianColumn, BaselineColumn,
     MemoryDiagnoser, MediumRunJob]
    public class ReadAheadCacheBench
    {
        public enum SeekMethod
        {
            Random,
            Linear
        }

        [Params(SeekMethod.Random, SeekMethod.Linear)]
        public SeekMethod Seek;

        const int Size = 8192 * 64000;
        const int Reads = 250000;

        int[] pagesToRead = new int[Reads];
        int[] pagesToReadLinear = new int[Reads];

        protected IMemoryCache MemoryCache;

        string _path;
        string _path2;
        FileStream _readStreamDefault;
        FileStream _readStreamDiskPageCache;
        FileStream _readStreamCached;
        private DiskPageCache _diskPageCache;


        [GlobalSetup]
        public void Setup()
        {
            _path = System.IO.Path.GetTempFileName();
            _path2 = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "mmf.file");
            MemoryCache = new MemoryCache(new MemoryCacheOptions() { SizeLimit = 32000 });
            using FileStream stream =
                new FileStream(_path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);

            for (int i = 0; i < Size; i++)
            {
                stream.WriteByte(0xFF);
            }

            Random random = new Random();
            for (int i = 0; i < Reads; i++)
            {
                pagesToRead[i] = random.Next(1, 63999);
            }

            for (int i = 0; i < Reads; i++)
            {
                pagesToReadLinear[i] = i;
                if (i > 63999)
                    pagesToReadLinear[i] = i - 63999;
            }
            if (!File.Exists(_path2))
                File.Copy(_path, _path2);

            _readStreamCached = new FileStream(_path, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite, 8196);
            _readStreamDiskPageCache = new FileStream(_path, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite,
                8196, FileOptions.RandomAccess);
            _readStreamDefault = new FileStream(_path, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite, 8196,
                FileOptions.RandomAccess);
            var settings = new DatatentSettings()
            {
                BufferPoolImplementation = BufferPoolImplementation.Unmanaged,
                IOSettings = new DatatentSettings.IO()
                {                    
                    MaxPageReadAheadCacheSize = 8192
                },
                EngineSettings = new DatatentSettings.Engine()
                {
                    MaxPageCacheSize = 64000
                }
            };
            BufferPoolFactory.Init(settings, NullLogger.Instance);
            _diskPageCache = new DiskPageCache(settings, NullLogger.Instance);
            using var test = BufferPoolFactory.Get().Rent();
            ArrayPool<byte> pool = ArrayPool<byte>.Shared;
            cacheSize = Constants.PAGE_SIZE * Constants.MAX_AMOUNT_OF_READ_AHEAD_PAGES;
            cacheBytes = pool.Rent(cacheSize);

            _mapped = MemoryMappedFile.CreateFromFile(_path2, FileMode.Open);
            _mappedStream = _mapped.CreateViewStream();
        }

        [GlobalCleanup]
        public void Teardown()
        {
            _readStreamDiskPageCache.Close();
            _readStreamCached.Close();
            _readStreamDefault.Close();
            _mappedStream.Close();
            _mapped.Dispose();

            System.IO.File.Delete(_path);
            System.IO.File.Delete(_path2);
        }

        [Benchmark(Baseline = true)]
        public uint NotCachedRandom()
        {
            uint a = 0;
            for (int i = 0; i < Reads; i++)
            {
                int page = 0;
                if (Seek == SeekMethod.Random)
                    page = pagesToRead[i];
                else if (Seek == SeekMethod.Linear)
                    page = pagesToReadLinear[i];
                using var buffer = Read((uint)page);
                a++;
            }

            return a;
        }

        private IBufferSegment Read(uint pageId)
        {
            var bufferSegment = BufferPoolFactory.Get().Rent(Constants.PAGE_SIZE);

            _readStreamCached.Seek(BasePage.PageOffset(pageId), SeekOrigin.Begin);
            _readStreamCached.Read(bufferSegment.Span.Slice(0, Constants.PAGE_SIZE));


            return bufferSegment;
        }

        private IBufferSegment ReadCached(uint pageId)
        {
            var bufferSegment = BufferPoolFactory.Get().Rent(Constants.PAGE_SIZE);

            _mappedStream.Seek(BasePage.PageOffset(pageId), SeekOrigin.Begin);
            _mappedStream.Read(bufferSegment.Span);
            
            return bufferSegment;
        }

        private void CacheEntryEvicted(object key, object value, EvictionReason reason, object state)
        {
            // manuel delete means we use this object now, so not dispose only when cache overflow remove item

            if (reason == EvictionReason.Capacity)
            {
                ((IBufferSegment)value).Dispose();
            }
        }

        [Benchmark()]
        public uint MemoryMappedCacheRandom()
        {
            uint a = 0;
            for (int i = 0; i < Reads; i++)
            {
                int page = 0;
                if (Seek == SeekMethod.Random)
                    page = pagesToRead[i];
                else if (Seek == SeekMethod.Linear)
                    page = pagesToReadLinear[i];
                using var buffer = ReadCached((uint)page);
                a++;
            }

            return a;

        }

        [Benchmark()]
        public uint DiskPageCacheRandom()
        {
            uint a = 0;
            for (int i = 0; i < Reads; i++)
            {
                int page = 0;
                if (Seek == SeekMethod.Random)
                    page = pagesToRead[i];
                else if (Seek == SeekMethod.Linear)
                    page = pagesToReadLinear[i];
                using var buffer = ReadDiskPageCache((uint)page);
                a++;
            }

            return a;
        }

        private static byte[] cacheBytes;
        private MemoryMappedFile _mapped;
        private MemoryMappedViewStream _mappedStream;
        private int cacheSize;

        private IBufferSegment ReadDiskPageCache(uint pageId)
        {
            var cachedBuffer = _diskPageCache.GetIfExists(pageId);
            if (cachedBuffer != null)
            {
                _diskPageCache.Remove(pageId);
                return cachedBuffer;
            }

            var bufferSegment = BufferPoolFactory.Get().Rent(Constants.PAGE_SIZE);
            var tempSpan = (Span<byte>)cacheBytes;

            _readStreamDiskPageCache.Seek(BasePage.PageOffset(pageId), SeekOrigin.Begin);
            _readStreamDiskPageCache.Read(cacheBytes, 0, cacheSize);
            var span = bufferSegment.Span;
            span.WriteBytes(0, tempSpan.Slice(0, Constants.PAGE_SIZE));

            uint nextPageId = pageId + 1;
            for (int i = 1; i < Constants.MAX_AMOUNT_OF_READ_AHEAD_PAGES; i++)
            {
                if (!_diskPageCache.Contains(nextPageId))
                {
                    var bufferCacheSegment = BufferPoolFactory.Get().Rent(Constants.PAGE_SIZE);
                    var spanCache = bufferCacheSegment.Span;
                    spanCache.WriteBytes(0, tempSpan.Slice(i * Constants.PAGE_SIZE, Constants.PAGE_SIZE));
                    _diskPageCache.Add(nextPageId, bufferCacheSegment);
                }
                nextPageId++;
            }
            return bufferSegment;
        }
    }
}


/*
 
|                  Method |   Seek |     Mean |   Error |   StdDev |   Median | Kurtosis | Skewness | Ratio | RatioSD | Rank | Baseline |     Gen 0 | Gen 1 | Gen 2 | Allocated |
|------------------------ |------- |---------:|--------:|---------:|---------:|---------:|---------:|------:|--------:|-----:|--------- |----------:|------:|------:|----------:|
|         NotCachedRandom | Random | 832.5 ms | 5.61 ms |  8.23 ms | 832.3 ms |    2.312 |   0.1633 |  1.00 |    0.00 |    3 |      Yes | 1000.0000 |     - |     - |  11.45 MB |
| MemoryMappedCacheRandom | Random | 277.5 ms | 3.20 ms |  4.79 ms | 277.2 ms |    2.526 |   0.3065 |  0.33 |    0.01 |    1 |       No | 1000.0000 |     - |     - |  11.45 MB |
|     DiskPageCacheRandom | Random | 435.3 ms | 5.35 ms |  7.84 ms | 433.8 ms |    3.072 |   0.5452 |  0.52 |    0.01 |    2 |       No | 1000.0000 |     - |     - |  11.45 MB |
|                         |        |          |         |          |          |          |          |       |         |      |          |           |       |       |           |
|         NotCachedRandom | Linear | 650.6 ms | 8.00 ms | 11.97 ms | 649.2 ms |    2.270 |   0.3128 |  1.00 |    0.00 |    2 |      Yes | 1000.0000 |     - |     - |  11.45 MB |
| MemoryMappedCacheRandom | Linear | 535.5 ms | 2.57 ms |  3.61 ms | 536.5 ms |    2.196 |  -0.5751 |  0.82 |    0.02 |    1 |       No | 1000.0000 |     - |     - |  11.45 MB |
|     DiskPageCacheRandom | Linear | 972.9 ms | 8.00 ms | 11.97 ms | 972.9 ms |    2.518 |  -0.2199 |  1.50 |    0.03 |    3 |       No | 1000.0000 |     - |     - |  11.45 MB | */