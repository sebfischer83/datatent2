using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Datatent2.Contracts;
using Datatent2.Core;
using Datatent2.Core.Memory;
using Datatent2.Core.Page;
using Datatent2.Core.Page.AllocationInformation;
using Datatent2.Core.Services.Data;
using Datatent2.Core.Services.Disk;
using Datatent2.Core.Services.Page;
using Datatent2.Plugins.Compression;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Serilog;
using Serilog.Events;

namespace Datatent2.Console
{
    class Program
    {
        private const int PAGES = 64000;
        const int SIZE = 8192 * PAGES;
        const int READS = 10000;
        static int[] _pagesToRead = new int[READS];
        static int[] _pagesToReadLinear = new int[READS];



        static async Task Main(string[] args)
        {
            ////ReusableTaskCompletionSource<int> reusableTaskCompletionSource = new ReusableTaskCompletionSource<int>();
            ////Task task = Task.Run(() =>
            ////{
            ////    Thread.Sleep(2000);
            ////    reusableTaskCompletionSource.SetResult(50);
            ////});

            ////task = Task.Run(() =>
            ////{
            ////    Thread.Sleep(2000);
            ////    reusableTaskCompletionSource.SetResult(50);
            ////});

            ////var res = await reusableTaskCompletionSource.Task;

            ////System.Console.WriteLine(res);

            //DatatentSettings datatentSettingsMapRead = new DatatentSettings()
            //{
            //    DatabasePath = Path.Combine(Path.GetTempPath(), "readmap.file"),
            //    IOSettings = new DatatentSettings.IO()
            //    {
            //        IOSystem = DatatentSettings.IOSystem.FileStream,
            //        UseReadAheadCache = false
            //    }
            //};
            //BufferPoolFactory.Init(datatentSettingsMapRead, NullLogger.Instance);
            //var buffer = BufferPool.Shared.Rent(Constants.PAGE_SIZE);
            //buffer.Span.Fill(0xFF);
            //var memoryMappedDiskService =
            //    new MemoryMappedDiskService(datatentSettingsMapRead, NullLogger.Instance);
            //for (uint i = 0; i < PAGES; i++)
            //{

            //    await memoryMappedDiskService.WriteBuffer(new WriteRequest(buffer, i));
            //}
            //Random random = new Random();
            //for (int i = 0; i < READS; i++)
            //{
            //    _pagesToRead[i] = random.Next(1, 63999);
            //}

            //for (int i = 0; i < READS; i++)
            //{
            //    _pagesToReadLinear[i] = i;
            //    if (i > 63999)
            //        _pagesToReadLinear[i] = i - 63999;
            //}
            //await ReadAsync(memoryMappedDiskService, datatentSettingsMapRead);

            //    IntSkipList intSkipList = new IntSkipList();
            //    foreach (var i in Enumerable.Range(0,99))
            //    {
            //        intSkipList.Insert(i);
            //    }


            var logger = new LoggerConfiguration().Enrich.FromLogContext().WriteTo.Async(configuration => configuration.File("log.txt", outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level}] {Message} {NewLine}{Exception}").MinimumLevel.Is(LogEventLevel.Information)).Enrich.FromLogContext().CreateLogger();
            var factory = LoggerFactory.Create(builder =>
            {
                builder.AddSerilog(logger);
                builder.SetMinimumLevel(LogLevel.Trace);
            });
            var path = Path.Combine(Path.GetDirectoryName(typeof(Program).Assembly.Location)!, "test.db");
            var plugins = "C:\\Development\\Datatent2\\plugins";

            Datatent datatent = await Datatent.Create(new DatatentSettings() { DatabasePath = path, PluginPath = plugins }, factory);

            var bogus = new Bogus.Randomizer();
            var table = await datatent.GetTable<TestObject>("testTable");

            for (int i = 0; i < 1; i++)
            {
                TestObject testObject = new TestObject();
                testObject.IntProp = i;
                testObject.StringProp = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";
                try
                {
                    await table.InsertObject(testObject, testObject.IntProp);
                }
                catch (Exception e)
                {
                    System.Console.WriteLine(e);
                    throw;
                }
            }

            for (int i = 0; i < 1; i++)
            {
                var obj = await table.Get(i);
                
            }

            datatent.Dispose();
            //memoryMappedDiskService.Dispose();
            //File.Delete(datatentSettingsMapRead.DatabasePath);
        }

        private static async Task ReadAsync(MemoryMappedDiskService memoryMappedDiskService, DatatentSettings datatentSettingsMapRead)
        {
            for (int j = 0; j < READS; j++)
            {
                var page = _pagesToReadLinear[j];
                var res = await memoryMappedDiskService.GetBuffer(new ReadRequest((uint)page));
                res.BufferSegment.Dispose();
            }
        }
    }

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
