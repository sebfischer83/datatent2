using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Datatent2.Core;
using Datatent2.Core.Page.AllocationInformation;
using Datatent2.Core.Services.Compression;
using Datatent2.Core.Services.Data;
using Datatent2.Core.Services.Disk;
using Datatent2.Core.Services.Page;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Serilog;
using Serilog.Events;

namespace Datatent2.Console
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var logger = new LoggerConfiguration().Enrich.FromLogContext().WriteTo.Async(configuration => configuration.File("log.txt").MinimumLevel.Is(LogEventLevel.Information)).CreateLogger();
            var factory = LoggerFactory.Create(builder =>
            {
                builder.AddSerilog(logger);
                builder.SetMinimumLevel(LogLevel.Trace);
            });
            var path = Path.Combine(Path.GetDirectoryName(typeof(Program).Assembly.Location)!, "test.db");
            var bogus = new Bogus.Randomizer();
            PageService pageService = await
                PageService.Create(FileDiskService.Create(new DatatentSettings() {InMemory = false, Path = path}),
                    factory.CreateLogger<PageService>());
           
            DataService dataService = new DataService(new NopCompressionService(), pageService, factory.CreateLogger<DataService>());
            for (int i = 0; i < 1; i++)
            {
                TestObject testObject = new TestObject();
                testObject.IntProp = bogus.Int();
                testObject.StringProp = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";
                try
                {
                    var address = await dataService.Insert(testObject);
                }
                catch (Exception e)
                {
                    System.Console.WriteLine(e);
                    throw;
                }
            }

            await pageService.CheckPoint();
            var aim = await pageService.GetPage<AllocationInformationPage>(2u);
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
