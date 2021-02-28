using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using MessagePack.Resolvers;
using JsonSerializer = Utf8Json.JsonSerializer;

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
        private byte[][] _serializedObjectsTextJson;
        private MyTestClass[] _testClasses;

        public int Size => 16000;

        [GlobalSetup]
        public void Setup()
        {
            List<byte[]> bytesList = new List<byte[]>(Size);
            List<byte[]> bytesList2 = new List<byte[]>(Size);
            List<byte[]> bytesList3 = new List<byte[]>(Size);
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
                bytesList3.Add(System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(myTestClass));
                bytesList2.Add(MessagePack.MessagePackSerializer.Typeless.Serialize(myTestClass, ContractlessStandardResolver.Options));
                testClasses.Add(myTestClass);
            }

            _serializedObjectsUtf8Json = bytesList.ToArray();
            _serializedObjectsMessagePack = bytesList2.ToArray();
            _serializedObjectsTextJson = bytesList3.ToArray();
            _testClasses = testClasses.ToArray();
        }

        [Benchmark(OperationsPerInvoke = 16000)]
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

        [Benchmark(OperationsPerInvoke = 16000)]
        public int SerializeUtf8Json()
        {
            int i = 0;

            for (int j = 0; j < _testClasses.Length; j++)
            {
                var obj = _testClasses[j];
                var result = Utf8Json.JsonSerializer.Serialize(obj);
                i += result.Length;
            }

            return i;
        }

        [Benchmark(OperationsPerInvoke = 16000)]
        public int SerializeUtf8JsonUnsafe()
        {
            int i = 0;

            for (int j = 0; j < _testClasses.Length; j++)
            {
                var obj = _testClasses[j];
                var result = Utf8Json.JsonSerializer.SerializeUnsafe(obj);
                i += result.Count;
            }

            return i;
        }

        [Benchmark(OperationsPerInvoke = 16000)]
        public int SerializeTextJson()
        {
            int i = 0;

            for (int j = 0; j < _testClasses.Length; j++)
            {
                var obj = _testClasses[j];
                var result = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(obj);
                i += result.Length;
            }

            return i;
        }

        [Benchmark(OperationsPerInvoke = 16000)]
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

        [Benchmark(OperationsPerInvoke = 16000)]
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

|                 Method |      Mean |     Error |    StdDev |    Median | Kurtosis | Skewness | Rank | Baseline |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|----------------------- |----------:|----------:|----------:|----------:|---------:|---------:|-----:|--------- |-------:|------:|------:|----------:|
|   SerializeMessagePack |  2.192 us | 0.0236 us | 0.0346 us |  2.186 us |    3.369 |   1.1049 |    1 |       No | 0.1458 |     - |     - |   1.19 KB |
|      SerializeUtf8Json |  3.601 us | 0.0291 us | 0.0398 us |  3.587 us |    3.010 |   0.7546 |    2 |       No | 0.2014 |     - |     - |   1.65 KB |
| DeserializeMessagePack |  3.640 us | 0.0373 us | 0.0534 us |  3.659 us |    1.499 |  -0.3469 |    2 |       No | 0.4861 |     - |     - |   3.97 KB |
|    DeserializeUtf8Json | 21.457 us | 0.2111 us | 0.2959 us | 21.390 us |    2.226 |   0.7251 |    3 |       No | 0.6250 |     - |     - |   5.44 KB |
*/