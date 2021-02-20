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
     RankColumn(), KurtosisColumn, SkewnessColumn, StdDevColumn, MeanColumn, MedianColumn, BaselineColumn, MediumRunJob, MemoryDiagnoser]
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
|   SerializeMessagePack |  128 |     240.7 us |     0.72 us |     1.05 us |     240.7 us |    2.204 |   0.2046 |    1 |       No |   18.5547 |        - |     - |    152.4 KB |
|      SerializeUtf8Json |  128 |     745.4 us |    25.10 us |    35.19 us |     724.8 us |    1.082 |   0.1121 |    3 |       No |  139.6484 |   1.9531 |     - |  1142.42 KB |
| DeserializeMessagePack |  128 |     479.1 us |     8.48 us |    12.44 us |     475.8 us |    2.432 |   0.5899 |    2 |       No |   62.0117 |   0.4883 |     - |    507.3 KB |
|    DeserializeUtf8Json |  128 |   2,756.3 us |    26.85 us |    37.63 us |   2,754.6 us |    1.721 |   0.2348 |    5 |       No |   82.0313 |        - |     - |   696.37 KB |
|   SerializeMessagePack | 1024 |   2,007.7 us |    24.27 us |    34.02 us |   2,008.5 us |    1.786 |   0.3025 |    4 |       No |  148.4375 |        - |     - |  1228.11 KB |
|      SerializeUtf8Json | 1024 |   6,045.1 us |   269.34 us |   394.79 us |   5,813.9 us |    1.324 |   0.1502 |    7 |       No | 1109.3750 |  15.6250 |     - |  9121.99 KB |
| DeserializeMessagePack | 1024 |   3,950.3 us |    82.58 us |   118.44 us |   4,006.4 us |    1.520 |  -0.3927 |    6 |       No |  492.1875 |        - |     - |  4070.88 KB |
|    DeserializeUtf8Json | 1024 |  22,273.4 us |   353.40 us |   518.01 us |  22,130.4 us |    2.722 |   0.7961 |    9 |       No |  656.2500 |        - |     - |  5578.12 KB |
|   SerializeMessagePack | 8096 |  17,884.2 us |   295.97 us |   414.90 us |  17,735.9 us |    2.325 |   0.6955 |    8 |       No | 1156.2500 |        - |     - |  9671.68 KB |
|      SerializeUtf8Json | 8096 |  54,029.0 us | 1,184.84 us | 1,699.26 us |  53,517.4 us |    4.109 |   1.3444 |   11 |       No | 8777.7778 | 111.1111 |     - | 72089.28 KB |
| DeserializeMessagePack | 8096 |  30,872.3 us |   428.20 us |   640.91 us |  30,984.8 us |    1.863 |  -0.4604 |   10 |       No | 3937.5000 |  31.2500 |     - | 32178.24 KB |
|    DeserializeUtf8Json | 8096 | 174,806.5 us | 1,138.83 us | 1,596.48 us | 175,017.2 us |    1.701 |  -0.2298 |   12 |       No | 5333.3333 |        - |     - | 44071.32 KB |

*/