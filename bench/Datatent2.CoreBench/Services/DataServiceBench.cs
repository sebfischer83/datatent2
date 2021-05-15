using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Datatent2.Contracts;
using Datatent2.Core;
using Datatent2.Core.Memory;
using Datatent2.Core.Page;
using Datatent2.Core.Page.Header;
using Datatent2.Core.Services.Cache;
using Datatent2.Core.Services.Data;
using Datatent2.Core.Services.Disk;
using Datatent2.Core.Services.Page;
using Datatent2.Plugins.Compression;
using Microsoft.Extensions.Logging.Abstractions;

namespace Datatent2.CoreBench.Services
{
    [HtmlExporter, CsvExporter(), CsvMeasurementsExporter(),
     RankColumn(), KurtosisColumn, SkewnessColumn, StdDevColumn, MeanColumn, MedianColumn, BaselineColumn, MemoryDiagnoser, MediumRunJob]
    public class DataServiceBench
    {
        private DataService _dataService;
        private List<TestObject> _objects;

        [GlobalSetup]
        public void Setup()
        {
            var bogus = new Bogus.Randomizer();
            BufferSegment headerBufferSegment = new BufferSegment(Constants.PAGE_SIZE);
            var headerPage = HeaderPage.CreateHeaderPage(headerBufferSegment);
            BufferSegment bufferSegment = new BufferSegment(Constants.PAGE_SIZE);
            PageHeader header = new PageHeader(1, PageType.Data);
            header.ToBuffer(bufferSegment.Span, 0);
            File.Delete(@"C:\Neuer Ordner\test.db");
            CacheService cacheService = new CacheService();
            PageService pageService = new PageService(DiskService.Create(new DatatentSettings() { InMemory = false, DatabasePath = @"C:\Neuer Ordner\test.db" }), cacheService, NullLogger.Instance);
            _dataService = new DataService(new NopCompressionService(), pageService, NullLogger<DataService>.Instance);

            _objects = new List<TestObject>(50);
            foreach (var i in Enumerable.Range(0, 50))
            {
                TestObject testObject = new TestObject();
                testObject.IntProp = bogus.Int();
                testObject.StringProp = bogus.String2(1000);
                _objects.Add(testObject);
            }
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            
        }

        [Benchmark(OperationsPerInvoke = 50)]
        public async Task<int> InsertBench()
        {
            int i = 0;
            foreach (var testObject in _objects)
            {
                var address = await _dataService.Insert(testObject);
                i += (int)address.PageId;
            }

            return i;
        }

        //[Benchmark(OperationsPerInvoke = 50)]
        //public async Task<int> InsertBulkBench()
        //{
        //   var res = await _dataService.BulkInsert(new[] {_objects});

        //    return res.Count;
        //}

        public class TestObject
        {
            public int IntProp { get; set; }
            public string StringProp { get; set; }

            protected bool Equals(TestObject other)
            {
                return IntProp == other.IntProp && StringProp == other.StringProp;
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != this.GetType()) return false;
                return Equals((TestObject)obj);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(IntProp, StringProp);
            }
        }
    }
}

/*
HDD:
BenchmarkDotNet=v0.12.1, OS=Windows 10.0.19042
AMD Ryzen 7 3700X, 1 CPU, 16 logical and 8 physical cores
.NET Core SDK=5.0.200
  [Host]    : .NET Core 5.0.3 (CoreCLR 5.0.321.7212, CoreFX 5.0.321.7212), X64 RyuJIT
  MediumRun : .NET Core 5.0.3 (CoreCLR 5.0.321.7212, CoreFX 5.0.321.7212), X64 RyuJIT

Job=MediumRun  IterationCount=15  LaunchCount=2
WarmupCount=10

|              Method |     Mean |     Error |    StdDev |   Median | Kurtosis | Skewness | Rank | Baseline |  Gen 0 |  Gen 1 | Gen 2 | Allocated |
|-------------------- |---------:|----------:|----------:|---------:|---------:|---------:|-----:|--------- |-------:|-------:|------:|----------:|
| InsertInMemoryBench | 7.833 us | 0.0914 us | 0.1340 us | 7.839 us |    3.047 |   0.5944 |    1 |       No | 0.3809 | 0.1172 |     - |   3.14 KB |

 */