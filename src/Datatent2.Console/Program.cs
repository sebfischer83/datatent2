using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Datatent2.Core;
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
        static async Task Main(string[] args)
        {
            IntSkipList intSkipList = new IntSkipList();
            foreach (var i in Enumerable.Range(0,99))
            {
                intSkipList.Insert(i);
            }


            var logger = new LoggerConfiguration().Enrich.FromLogContext().WriteTo.Async(configuration => configuration.File("log.txt", outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level}] {Message} {NewLine}{Exception}").MinimumLevel.Is(LogEventLevel.Information)).Enrich.FromLogContext().CreateLogger();
            var factory = LoggerFactory.Create(builder =>
            {
                builder.AddSerilog(logger);
                builder.SetMinimumLevel(LogLevel.Trace);
            });
            var path = Path.Combine(Path.GetDirectoryName(typeof(Program).Assembly.Location)!, "test.db");
            var plugins = "C:\\Development\\Datatent2\\plugins";

            Datatent datatent = await Datatent.Create(new DatatentSettings() { InMemory = false, DatabasePath = path, PluginPath = plugins }, factory);

            var bogus = new Bogus.Randomizer();
         
            for (int i = 0; i < 1; i++)
            {
                TestObject testObject = new TestObject();
                testObject.IntProp = i;
                testObject.StringProp = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";
                try
                {
                    await datatent.Insert(testObject, testObject.IntProp);
                }
                catch (Exception e)
                {
                    System.Console.WriteLine(e);
                    throw;
                }
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
