using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using MessagePack.Resolvers;
using Utf8Json;

namespace Datatent2.CoreBench.Serialization
{
    [HtmlExporter, CsvExporter(), CsvMeasurementsExporter(),
     RankColumn(), KurtosisColumn, SkewnessColumn, StdDevColumn, MeanColumn, MedianColumn, BaselineColumn, MediumRunJob, MemoryDiagnoser, Orderer(SummaryOrderPolicy.FastestToSlowest, MethodOrderPolicy.Declared)]
    public class DynamicObjSerialization
    {
        public class MyTestClass
        {
            public string StringProp { get; set; }

            public List<int> IntList { get; set; }

            public bool BoolProp { get; set; }

            public string StringProp2 { get; set; }
        }

        private byte[][] _serializedObjectsMessagePack;
        private byte[][] _serializedObjectsUtf8Json;
        private MyTestClass[] _testClasses;

        [Params(128, 1024, 8096)]
        public int Size { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            List<byte[]> bytesList = new List<byte[]>(Size);
            List<byte[]> bytesList2 = new List<byte[]>(Size);
            List<MyTestClass> testClasses = new List<MyTestClass>(Size);

            var bogus = new Bogus.Randomizer();
            for (int i = 0; i < Size; i++)
            {
                MyTestClass myTestClass = new MyTestClass();
                myTestClass.BoolProp = bogus.Bool();
                myTestClass.IntList = new List<int>(100);
                for (int j = 0; j < 100; j++)
                {
                    myTestClass.IntList.Add(bogus.Int());
                }

                myTestClass.StringProp = bogus.String(10, 100);
                myTestClass.StringProp2 = bogus.String(10, 100);

                bytesList.Add(JsonSerializer.Serialize(myTestClass));
                bytesList2.Add(MessagePack.MessagePackSerializer.Typeless.Serialize(myTestClass, ContractlessStandardResolver.Options));
                testClasses.Add(myTestClass);
            }

            _serializedObjectsUtf8Json = bytesList.ToArray();
            _serializedObjectsMessagePack = bytesList2.ToArray();
            _testClasses = testClasses.ToArray();
        }

        [Benchmark]
        public int SerializeMessagePack()
        {
            int i = 0;

            for (int j = 0; j < _testClasses.Length; j++)
            {
                var obj = _testClasses[j];
                var result = MessagePack.MessagePackSerializer.Typeless.Serialize(obj);
                i += result.Length;
            }

            return i;
        }

        [Benchmark]
        public int SerializeUtf8Json()
        {
            int i = 0;

            for (int j = 0; j < _testClasses.Length; j++)
            {
                var obj = _testClasses[j];
                var result = Encoding.UTF8.GetBytes(Jil.JSON.Serialize(obj));
                i += result.Length;
            }

            return i;
        }

        [Benchmark]
        public int DeserializeMessagePack()
        {
            int i = 0;

            for (int j = 0; j < _serializedObjectsMessagePack.Length; j++)
            {
                var dyn = (dynamic)MessagePack.MessagePackSerializer.Deserialize<dynamic>(_serializedObjectsMessagePack[j]);
                i += dyn["StringProp2"].Length;
            }

            return i;
        }

        [Benchmark]
        public int DeserializeUtf8Json()
        {
            int i = 0;

            for (int j = 0; j < _serializedObjectsUtf8Json.Length; j++)
            {
                var dyn = Utf8Json.JsonSerializer.Deserialize<dynamic>(_serializedObjectsUtf8Json[j]);
                i += dyn["StringProp2"].Length;
            }

            return i;
        }
    }
}

/*
BenchmarkDotNet=v0.12.1, OS=Windows 10.0.19042
AMD Ryzen 7 3700X, 1 CPU, 16 logical and 8 physical cores
.NET Core SDK=5.0.200-preview.21079.7
  [Host]    : .NET Core 5.0.3 (CoreCLR 5.0.321.7212, CoreFX 5.0.321.7212), X64 RyuJIT
  MediumRun : .NET Core 5.0.3 (CoreCLR 5.0.321.7212, CoreFX 5.0.321.7212), X64 RyuJIT

Job=MediumRun  IterationCount=15  LaunchCount=2
WarmupCount=10

|                 Method | Size |         Mean |       Error |      StdDev |       Median | Kurtosis | Skewness | Rank | Baseline |     Gen 0 |    Gen 1 | Gen 2 |   Allocated |
|----------------------- |----- |-------------:|------------:|------------:|-------------:|---------:|---------:|-----:|--------- |----------:|---------:|------:|------------:|
|   SerializeMessagePack |  128 |     244.2 us |     2.13 us |     3.13 us |     243.5 us |    2.578 |   0.7941 |    1 |       No |   18.5547 |        - |     - |   152.97 KB |
| DeserializeMessagePack |  128 |     474.1 us |     4.84 us |     7.10 us |     473.9 us |    2.178 |   0.2250 |    2 |       No |   62.0117 |   0.4883 |     - |   507.53 KB |
|      SerializeUtf8Json |  128 |     693.5 us |     9.53 us |    13.67 us |     691.2 us |    2.872 |   0.6298 |    3 |       No |  138.6719 |   1.9531 |     - |  1138.84 KB |
|   SerializeMessagePack | 1024 |   2,001.6 us |    14.03 us |    20.56 us |   1,996.6 us |    3.053 |   0.9084 |    4 |       No |  148.4375 |        - |     - |  1223.79 KB |
|    DeserializeUtf8Json |  128 |   2,687.6 us |    11.74 us |    16.83 us |   2,686.6 us |    2.236 |   0.0494 |    5 |       No |   82.0313 |        - |     - |   696.73 KB |
| DeserializeMessagePack | 1024 |   3,996.2 us |    40.02 us |    58.66 us |   4,000.0 us |    2.633 |   0.2503 |    6 |       No |  492.1875 |        - |     - |  4071.61 KB |
|      SerializeUtf8Json | 1024 |   5,700.1 us |    78.82 us |   117.97 us |   5,657.2 us |    2.230 |   0.5764 |    7 |       No | 1109.3750 |  15.6250 |     - |   9119.4 KB |
|   SerializeMessagePack | 8096 |  17,839.6 us |   129.80 us |   181.97 us |  17,828.5 us |    4.419 |   0.8939 |    8 |       No | 1156.2500 |        - |     - |   9670.8 KB |
|    DeserializeUtf8Json | 1024 |  21,958.2 us |    84.39 us |   121.03 us |  21,967.0 us |    2.164 |  -0.1596 |    9 |       No |  656.2500 |        - |     - |  5575.15 KB |
| DeserializeMessagePack | 8096 |  31,145.9 us |   349.84 us |   512.79 us |  31,016.0 us |    2.628 |   0.6797 |   10 |       No | 3937.5000 |        - |     - | 32176.29 KB |
|      SerializeUtf8Json | 8096 |  48,994.6 us | 1,890.73 us | 2,771.41 us |  49,843.5 us |    1.342 |  -0.0856 |   11 |       No | 8800.0000 | 100.0000 |     - | 72055.58 KB |
|    DeserializeUtf8Json | 8096 | 171,733.0 us |   927.08 us | 1,387.62 us | 171,814.6 us |    1.837 |   0.1423 |   12 |       No | 5333.3333 |        - |     - | 44075.88 KB |

*/