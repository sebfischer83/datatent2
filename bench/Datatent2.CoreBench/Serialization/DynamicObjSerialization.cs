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

|              Method | Size |         Mean |     Error |    StdDev |       Median | Kurtosis | Skewness | Rank | Baseline |     Gen 0 |   Gen 1 | Gen 2 |  Allocated |
|-------------------- |----- |-------------:|----------:|----------:|-------------:|---------:|---------:|-----:|--------- |----------:|--------:|------:|-----------:|
|      DeserializeJil |  128 |           NA |        NA |        NA |           NA |       NA |       NA |    ? |       No |         - |       - |     - |          - |
|      DeserializeJil | 1024 |           NA |        NA |        NA |           NA |       NA |       NA |    ? |       No |         - |       - |     - |          - |
|      DeserializeJil | 8096 |           NA |        NA |        NA |           NA |       NA |       NA |    ? |       No |         - |       - |     - |          - |
|        SerializeJil |  128 |     696.8 us |   3.58 us |   4.78 us |     697.7 us |   2.7610 |  -0.2609 |    1 |       No |  138.6719 |  1.9531 |     - |  1164768 B |
|   SerializeUtf8Json |  128 |     701.2 us |   7.04 us |  10.32 us |     705.3 us |   1.9145 |  -0.6772 |    1 |       No |  139.6484 |  1.9531 |     - |  1170768 B |
| DeserializeUtf8Json |  128 |   2,658.5 us |  11.61 us |  17.02 us |   2,657.1 us |   2.2265 |   0.1537 |    2 |       No |   82.0313 |       - |     - |   713009 B |
|        SerializeJil | 1024 |   5,584.9 us |  15.00 us |  21.51 us |   5,580.5 us |   3.2200 |   0.8048 |    3 |       No | 1109.3750 | 15.6250 |     - |  9337434 B |
|   SerializeUtf8Json | 1024 |   6,043.8 us | 322.62 us | 462.69 us |   6,024.5 us |   0.9506 |   0.0117 |    3 |       No | 1109.3750 | 15.6250 |     - |  9339066 B |
| DeserializeUtf8Json | 1024 |  21,314.1 us | 114.04 us | 163.56 us |  21,323.3 us |   1.6568 |   0.1160 |    4 |       No |  656.2500 |       - |     - |  5708073 B |
|        SerializeJil | 8096 |  45,361.6 us | 594.58 us | 871.52 us |  45,741.8 us |   1.2273 |   0.0538 |    5 |       No | 8818.1818 | 90.9091 |     - | 73842482 B |
|   SerializeUtf8Json | 8096 |  46,365.4 us | 414.34 us | 620.16 us |  46,200.4 us |   3.7778 |   1.1868 |    5 |       No | 8818.1818 | 90.9091 |     - | 73823314 B |
| DeserializeUtf8Json | 8096 | 170,343.5 us | 658.54 us | 965.28 us | 170,356.3 us |   3.0002 |  -0.2912 |    6 |       No | 5333.3333 |       - |     - | 45137928 B | 


*/