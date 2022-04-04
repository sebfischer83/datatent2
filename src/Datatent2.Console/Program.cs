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

            var logger = new LoggerConfiguration().MinimumLevel.Verbose().Enrich.FromLogContext().WriteTo.Async(configuration => configuration.File("log.txt", outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level}] {Message} {NewLine}{Exception}").MinimumLevel.Is(LogEventLevel.Verbose)).Enrich.FromLogContext().CreateLogger();
            var factory = LoggerFactory.Create(builder =>
            {
                builder.AddSerilog(logger);
                builder.SetMinimumLevel(LogLevel.Trace);
            });
            var path = Path.GetDirectoryName(typeof(Program).Assembly.Location)!;
            var plugins = Path.Combine(@"C:\Development\datatent2", "plugins");
            path = Path.Combine(path, "database");
            path = Path.Combine(path, "test.db");

            Datatent datatent = await Datatent.Create(new DatatentSettings() 
            { 
                DatabasePath = path, 
                PluginPath = plugins                
            }, factory);

            var bogus = new Bogus.Randomizer();
            var table = await datatent.GetTable<TestObject, int>("testTable");

            //var t = await table.Get(1);

            for (int i = 0; i < 500; i++)
            {
                TestObject testObject = new TestObject();
                testObject.IntProp = i;
                testObject.StringProp = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";
                try
                {
                    await table.Insert(testObject, testObject.IntProp);
                }
                catch (Exception e)
                {
                    System.Console.WriteLine(e);
                    throw;
                }
            }

            for (int i = 400; i < 500; i++)
            {
                var obj = await table.Get(i);
                
            }

            await datatent.DisposeAsync();
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
