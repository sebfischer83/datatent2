using BenchmarkDotNet.Attributes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Datatent2.Contracts;
using Datatent2.Core;
using Datatent2.Core.Memory;
using Datatent2.Core.Services.Disk;
using Microsoft.Extensions.Logging.Abstractions;

namespace Datatent2.CoreBench.IO
{
    [HtmlExporter, CsvExporter(), CsvMeasurementsExporter(), RPlotExporter(),
         RankColumn(), KurtosisColumn, SkewnessColumn, StdDevColumn, MeanColumn, MedianColumn, BaselineColumn,
         MemoryDiagnoser, MediumRunJob]
    public class DiskServiceBenchmark
    {
        private string _pathWriteMap;
        private string _pathWriteStream;
        private string _pathReadMap;
        private string _pathReadStream;
        private FileDiskService _fileDiskServiceRead;
        private FileDiskService _fileDiskServiceWrite;
        private MemoryMappedDiskService _memoryMappedDiskServiceRead;
        private MemoryMappedDiskService _memoryMappedDiskServiceWrite;

        public enum SeekMethod
        {
            Random,
            Linear
        }

        [Params(ReadAheadCacheBench.SeekMethod.Random, ReadAheadCacheBench.SeekMethod.Linear)]
        public ReadAheadCacheBench.SeekMethod Seek;

        private const int PAGES = 64000;
        const int SIZE = 8192 * PAGES;
        const int READS = 100000;

        int[] _pagesToRead = new int[READS];
        int[] _pagesToReadLinear = new int[READS];
        private IBufferSegment _buffer;

        [GlobalSetup]
        public async Task SetupAsync()
        {
            _pathWriteMap = Path.Combine("F:\\bench", "writemap.file");
            _pathWriteStream = Path.Combine("F:\\bench", "writestream.file");
            _pathReadMap = Path.Combine("F:\\bench", "readmap.file");
            _pathReadStream = Path.Combine("F:\\bench", "readstream.file");

            //_pathWriteMap = Path.Combine(Path.GetTempPath(), "writemap.file");
            //_pathWriteStream = Path.Combine(Path.GetTempPath(), "writestream.file");
            //_pathReadMap = Path.Combine(Path.GetTempPath(), "readmap.file");
            //_pathReadStream = Path.Combine(Path.GetTempPath(), "readstream.file");

            DatatentSettings datatentSettingsStreamRead = new DatatentSettings()
            {
                DatabasePath = _pathReadStream,
                IOSettings = new DatatentSettings.IO()
                {
                    IOSystem = DatatentSettings.IOSystem.FileStream,
                    UseReadAheadCache = false
                }
            };
            DatatentSettings datatentSettingsStreamWrite = new DatatentSettings()
            {
                DatabasePath = _pathWriteStream,
                IOSettings = new DatatentSettings.IO()
                {
                    IOSystem = DatatentSettings.IOSystem.FileStream,
                    UseReadAheadCache = false
                }
            };
            DatatentSettings datatentSettingsMapRead = new DatatentSettings()
            {
                DatabasePath = _pathReadMap,
                IOSettings = new DatatentSettings.IO()
                {
                    IOSystem = DatatentSettings.IOSystem.FileStream,
                    UseReadAheadCache = false
                }
            };
            DatatentSettings datatentSettingsMapWrite = new DatatentSettings()
            {
                DatabasePath = _pathWriteMap,
                IOSettings = new DatatentSettings.IO()
                {
                    IOSystem = DatatentSettings.IOSystem.FileStream,
                    UseReadAheadCache = false
                }
            };
            BufferPoolFactory.Init(datatentSettingsStreamRead, NullLogger.Instance);
            _fileDiskServiceRead = new FileDiskService(datatentSettingsStreamRead);
            _fileDiskServiceWrite = new FileDiskService(datatentSettingsStreamWrite);

            _memoryMappedDiskServiceRead =
                new MemoryMappedDiskService(datatentSettingsMapRead, NullLogger.Instance);

            _memoryMappedDiskServiceWrite =
                new MemoryMappedDiskService(datatentSettingsMapWrite, NullLogger.Instance);

            Random random = new Random();

            // fill file for reading
            

            _buffer = BufferPool.Shared.Rent(Constants.PAGE_SIZE);
            _buffer.Span.Fill(0xFF);
            for (uint i = 0; i < PAGES; i++)
            {

                await _fileDiskServiceRead.WriteBuffer(new WriteRequest(_buffer, i));
                await _memoryMappedDiskServiceRead.WriteBuffer(new WriteRequest(_buffer, i));
            }
            
            for (int i = 0; i < READS; i++)
            {
                _pagesToRead[i] = random.Next(1, 63999);
            }

            for (int i = 0; i < READS; i++)
            {
                _pagesToReadLinear[i] = i;
                if (i > 63999)
                    _pagesToReadLinear[i] = i - 63999;
            }
        }

        [GlobalCleanup]
        public void Teardown()
        {
            _fileDiskServiceRead.Dispose();
            _fileDiskServiceWrite.Dispose();
            _memoryMappedDiskServiceRead.Dispose();
            _memoryMappedDiskServiceWrite.Dispose();
            File.Delete(_pathReadMap);
            File.Delete(_pathReadStream);
            File.Delete(_pathWriteMap);
            File.Delete(_pathWriteStream);
        }

        [Benchmark(OperationsPerInvoke = READS)]
        public async Task<uint> StreamDiskServiceRead()
        {
            uint i = 0;

            for (int j = 0; j < READS; j++)
            {
                var page = Seek == ReadAheadCacheBench.SeekMethod.Linear
                    ? _pagesToReadLinear[j]
                    : _pagesToRead[j];
                var res = await _fileDiskServiceRead.GetBuffer(new ReadRequest((uint)page));
                i += res.PageId;
                res.BufferSegment.Dispose();
            }

            return i;
        }

        [Benchmark(OperationsPerInvoke = READS)]
        public async Task<uint> MemMappedDiskServiceRead()
        {
            uint i = 0;

            for (int j = 0; j < READS; j++)
            {
                var page = Seek == ReadAheadCacheBench.SeekMethod.Linear
                    ? _pagesToReadLinear[j]
                    : _pagesToRead[j];
                var res = await _memoryMappedDiskServiceRead.GetBuffer(new ReadRequest((uint)page));
                i += res.PageId;
                res.BufferSegment.Dispose();
            }

            return i;
        }


        [Benchmark(OperationsPerInvoke = READS)]
        public async Task<uint> StreamDiskServiceWrite()
        {
            uint i = 0;

            for (int j = 0; j < READS; j++)
            {
                var page = Seek == ReadAheadCacheBench.SeekMethod.Linear
                    ? _pagesToReadLinear[j]
                    : _pagesToRead[j];
                await _fileDiskServiceWrite.WriteBuffer(new WriteRequest(_buffer, (uint)page));
            }

            return i;
        }


        [Benchmark(OperationsPerInvoke = READS)]
        public async Task<uint> MemMappedDiskServiceWrite()
        {
            uint i = 0;

            for (int j = 0; j < READS; j++)
            {
                var page = Seek == ReadAheadCacheBench.SeekMethod.Linear
                    ? _pagesToReadLinear[j]
                    : _pagesToRead[j];
                await _memoryMappedDiskServiceWrite.WriteBuffer(new WriteRequest(_buffer, (uint)page));
            }

            return i;
        }

    }
}
