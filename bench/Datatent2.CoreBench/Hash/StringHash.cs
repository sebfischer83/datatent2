using System;
using System.Collections.Generic;
using System.Data.HashFunction.Blake2;
using System.Data.HashFunction.CityHash;
using System.Data.HashFunction.CRC;
using System.Data.HashFunction.FNV;
using System.Data.HashFunction.HashAlgorithm;
using System.Data.HashFunction.Jenkins;
using System.Data.HashFunction.MurmurHash;
using System.Data.HashFunction.Pearson;
using System.Data.HashFunction.xxHash;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using CsvHelper;
using Force.Crc32;

namespace Datatent2.AlgoBench.Hash
{
    [HtmlExporter, CsvExporter(), CsvMeasurementsExporter(),
     RankColumn(), KurtosisColumn, SkewnessColumn, StdDevColumn, MeanColumn, MedianColumn, BaselineColumn, MediumRunJob, MemoryDiagnoser]
    public class StringHash
    {
        public enum Types
        {
            Word,
            ShortSentence,
            LongText,
            Guid
        }

        [Params(Types.Word, Types.LongText, Types.ShortSentence)]
        public Types TestType;

        private byte[][] _sentences;
        private byte[][] _words;
        private byte[] _longText;

        private IHashAlgorithmWrapper _sha1;
        private IHashAlgorithmWrapper _md5;
        private IHashAlgorithmWrapper _sha512;
        private IBlake2B _blake2;
        private ICityHash _city;
        private IFNV1 _fnv1;
        private IFNV1a _fnv1A;
        private IJenkinsLookup3 _jenkins3;
        private IMurmurHash3 _murmur3;
        private IPearson _pearson;
        private IxxHash _xxHash;
        private ICRC _crc8;
        private ICRC _crc16;
        private Crc32HardwareAlgorithm _hardwareCrc32;


        [GlobalSetup]
        public void GlobalSetup()
        {
            var path = Path.GetDirectoryName(typeof(StringHash).GetTypeInfo().Assembly.Location);
            path = Path.Combine(path, "Files", "JEOPARDY_CSV.csv");

            List<byte[]> list = new();
            using var reader = new StreamReader(path);
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
            csv.Read();
            csv.ReadHeader();
            StringBuilder stringBuilder = new StringBuilder();
            while (csv.Read())
            {
                var str = csv.GetField<string>(6);
                stringBuilder.Append(" " + str);
                list.Add(Encoding.UTF8.GetBytes(str));
            }
            _sentences = list.ToArray();
            _longText = Encoding.UTF8.GetBytes(stringBuilder.ToString());

            list.Clear();
            path = Path.GetDirectoryName(typeof(StringHash).GetTypeInfo().Assembly.Location);
            using var streamReader = new StreamReader(new FileStream(Path.Combine(path, "Files", "words.txt"), FileMode.Open));
            while (!streamReader.EndOfStream)
            {
                list.Add(Encoding.UTF8.GetBytes(streamReader.ReadLine() ?? ""));
            }

            _words = list.ToArray();

            _sha1 = System.Data.HashFunction.HashAlgorithm.HashAlgorithmWrapperFactory.Instance.Create(
                new HashAlgorithmWrapperConfig() { InstanceFactory = SHA1.Create });
            _md5 = System.Data.HashFunction.HashAlgorithm.HashAlgorithmWrapperFactory.Instance.Create(
                new HashAlgorithmWrapperConfig() { InstanceFactory = MD5.Create });
            _sha512 = System.Data.HashFunction.HashAlgorithm.HashAlgorithmWrapperFactory.Instance.Create(
                new HashAlgorithmWrapperConfig() { InstanceFactory = SHA512.Create });

            _blake2 = System.Data.HashFunction.Blake2.Blake2BFactory.Instance.Create();
            _city = System.Data.HashFunction.CityHash.CityHashFactory.Instance.Create();
            _fnv1 = System.Data.HashFunction.FNV.FNV1Factory.Instance.Create();
            _fnv1A = System.Data.HashFunction.FNV.FNV1aFactory.Instance.Create();
            _jenkins3 = System.Data.HashFunction.Jenkins.JenkinsLookup3Factory.Instance.Create();
            _murmur3 = System.Data.HashFunction.MurmurHash.MurmurHash3Factory.Instance.Create();
            _pearson = System.Data.HashFunction.Pearson.PearsonFactory.Instance.Create();
            _xxHash = System.Data.HashFunction.xxHash.xxHashFactory.Instance.Create();
            _crc8 = System.Data.HashFunction.CRC.CRCFactory.Instance.Create(new CRCConfig() { HashSizeInBits = 8 });
            _crc16 = System.Data.HashFunction.CRC.CRCFactory.Instance.Create(new CRCConfig() { HashSizeInBits = 16 });
            _hardwareCrc32 = new Crc32HardwareAlgorithm();
            _hardwareCrc32.Initialize();
        }

        [Benchmark]
        public int Sha1()
        {
            int a = 0;

            if (TestType == Types.ShortSentence)
                for (int i = 0; i < _sentences.GetLength(0); i++)
                {
                    _sha1.ComputeHash(_sentences[i]);
                    a++;
                }
            if (TestType == Types.Word)
                for (int i = 0; i < _words.GetLength(0); i++)
                {
                    _sha1.ComputeHash(_words[i]);
                    a++;
                }

            if (TestType == Types.LongText)
                _sha1.ComputeHash(_longText);
            return a;
        }


        [Benchmark]
        public int Md5()
        {
            int a = 0;
            if (TestType == Types.ShortSentence)
                for (int i = 0; i < _sentences.GetLength(0); i++)
                {
                    _md5.ComputeHash(_sentences[i]);
                    a++;
                }
            if (TestType == Types.Word)
                for (int i = 0; i < _words.GetLength(0); i++)
                {
                    _md5.ComputeHash(_words[i]);
                    a++;
                }
            if (TestType == Types.LongText)
                _md5.ComputeHash(_longText);
            return a;
        }

        [Benchmark]
        public int Sha512()
        {
            int a = 0;
            if (TestType == Types.ShortSentence)
                for (int i = 0; i < _sentences.GetLength(0); i++)
                {
                    _sha512.ComputeHash(_sentences[i]);
                    a++;
                }
            if (TestType == Types.Word)
                for (int i = 0; i < _words.GetLength(0); i++)
                {
                    _sha512.ComputeHash(_words[i]);
                    a++;
                }
            if (TestType == Types.LongText)
                _sha512.ComputeHash(_longText);
            return a;
        }

        [Benchmark]
        public int Blake2()
        {
            int a = 0;
            if (TestType == Types.ShortSentence)
                for (int i = 0; i < _sentences.GetLength(0); i++)
                {
                    _blake2.ComputeHash(_sentences[i]);
                    a++;
                }
            if (TestType == Types.Word)
                for (int i = 0; i < _words.GetLength(0); i++)
                {
                    _blake2.ComputeHash(_words[i]);
                    a++;
                }
            if (TestType == Types.LongText)
                _blake2.ComputeHash(_longText);
            return a;
        }

        [Benchmark]
        public int City()
        {
            int a = 0;
            if (TestType == Types.ShortSentence)
                for (int i = 0; i < _sentences.GetLength(0); i++)
                {
                    _city.ComputeHash(_sentences[i]);
                    a++;
                }
            if (TestType == Types.Word)
                for (int i = 0; i < _words.GetLength(0); i++)
                {
                    _city.ComputeHash(_words[i]);
                    a++;
                }
            if (TestType == Types.LongText)
                _city.ComputeHash(_longText);
            return a;
        }

        [Benchmark]
        public int Fnv1()
        {
            int a = 0;
            if (TestType == Types.ShortSentence)
                for (int i = 0; i < _sentences.GetLength(0); i++)
                {
                    _fnv1.ComputeHash(_sentences[i]);
                    a++;
                }
            if (TestType == Types.Word)
                for (int i = 0; i < _words.GetLength(0); i++)
                {
                    _fnv1.ComputeHash(_words[i]);
                    a++;
                }
            if (TestType == Types.LongText)
                _fnv1.ComputeHash(_longText);
            return a;
        }

        [Benchmark]
        public int Fnv1A()
        {
            int a = 0;
            if (TestType == Types.ShortSentence)
                for (int i = 0; i < _sentences.GetLength(0); i++)
                {
                    _fnv1A.ComputeHash(_sentences[i]);
                    a++;
                }
            if (TestType == Types.Word)
                for (int i = 0; i < _words.GetLength(0); i++)
                {
                    _fnv1A.ComputeHash(_words[i]);
                    a++;
                }
            if (TestType == Types.LongText)
                _fnv1A.ComputeHash(_longText);
            return a;
        }

        [Benchmark]
        public int Jenkins3()
        {
            int a = 0;
            if (TestType == Types.ShortSentence)
                for (int i = 0; i < _sentences.GetLength(0); i++)
                {
                    _jenkins3.ComputeHash(_sentences[i]);
                    a++;
                }
            if (TestType == Types.Word)
                for (int i = 0; i < _words.GetLength(0); i++)
                {
                    _jenkins3.ComputeHash(_words[i]);
                    a++;
                }
            if (TestType == Types.LongText)
                _jenkins3.ComputeHash(_longText);
            return a;
        }

        [Benchmark]
        public int Murmur3()
        {
            int a = 0;
            if (TestType == Types.ShortSentence)
                for (int i = 0; i < _sentences.GetLength(0); i++)
                {
                    _murmur3.ComputeHash(_sentences[i]);
                    a++;
                }
            if (TestType == Types.Word)
                for (int i = 0; i < _words.GetLength(0); i++)
                {
                    _murmur3.ComputeHash(_words[i]);
                    a++;
                }
            if (TestType == Types.LongText)
                _murmur3.ComputeHash(_longText);
            return a;
        }

        [Benchmark]
        public int Pearson()
        {
            int a = 0;
            if (TestType == Types.ShortSentence)
                for (int i = 0; i < _sentences.GetLength(0); i++)
                {
                    _pearson.ComputeHash(_sentences[i]);
                    a++;
                }
            if (TestType == Types.Word)
                for (int i = 0; i < _words.GetLength(0); i++)
                {
                    _pearson.ComputeHash(_words[i]);
                    a++;
                }
            if (TestType == Types.LongText)
                _pearson.ComputeHash(_longText);
            return a;
        }

        [Benchmark]
        public int XxHash()
        {
            int a = 0;
            if (TestType == Types.ShortSentence)
                for (int i = 0; i < _sentences.GetLength(0); i++)
                {
                    _xxHash.ComputeHash(_sentences[i]);
                    a++;
                }
            if (TestType == Types.Word)
                for (int i = 0; i < _words.GetLength(0); i++)
                {
                    _xxHash.ComputeHash(_words[i]);
                    a++;
                }
            if (TestType == Types.LongText)
                _xxHash.ComputeHash(_longText);
            return a;
        }

        [Benchmark]
        public int Crc8Hash()
        {
            int a = 0;
            if (TestType == Types.ShortSentence)
                for (int i = 0; i < _sentences.GetLength(0); i++)
                {
                    _crc8.ComputeHash(_sentences[i]);
                    a++;
                }
            if (TestType == Types.Word)
                for (int i = 0; i < _words.GetLength(0); i++)
                {
                    _crc8.ComputeHash(_words[i]);
                    a++;
                }
            if (TestType == Types.LongText)
                _crc8.ComputeHash(_longText);
            return a;
        }

        [Benchmark]
        public int Crc16Hash()
        {
            int a = 0;
            if (TestType == Types.ShortSentence)
                for (int i = 0; i < _sentences.GetLength(0); i++)
                {
                    _crc16.ComputeHash(_sentences[i]);
                    a++;
                }
            if (TestType == Types.Word)
                for (int i = 0; i < _words.GetLength(0); i++)
                {
                    _crc16.ComputeHash(_words[i]);
                    a++;
                }
            if (TestType == Types.LongText)
                _crc16.ComputeHash(_longText);
            return a;
        }

        [Benchmark]
        public int Crc32Force()
        {
            int a = 0;
            if (TestType == Types.ShortSentence)
                for (int i = 0; i < _sentences.GetLength(0); i++)
                {
                    Force.Crc32.Crc32Algorithm.Compute(_sentences[i]);
                    a++;
                }
            if (TestType == Types.Word)
                for (int i = 0; i < _words.GetLength(0); i++)
                {
                    Force.Crc32.Crc32Algorithm.Compute(_words[i]);
                    a++;
                }
            if (TestType == Types.LongText)
                Force.Crc32.Crc32Algorithm.Compute(_longText);
            return a;
        }

        [Benchmark]
        public int Crc32CForce()
        {
            int a = 0;
            if (TestType == Types.ShortSentence)
                for (int i = 0; i < _sentences.GetLength(0); i++)
                {
                    Force.Crc32.Crc32CAlgorithm.Compute(_sentences[i]);
                    a++;
                }
            if (TestType == Types.Word)
                for (int i = 0; i < _words.GetLength(0); i++)
                {
                    Force.Crc32.Crc32CAlgorithm.Compute(_words[i]);
                    a++;
                }
            if (TestType == Types.LongText)
                Force.Crc32.Crc32CAlgorithm.Compute(_longText);
            return a;
        }

        [Benchmark]
        public int HardwareCrc32()
        {
            int a = 0;
            if (TestType == Types.ShortSentence)
                for (int i = 0; i < _sentences.GetLength(0); i++)
                {
                    _hardwareCrc32.ComputeHash((_sentences[i]));
                    a++;
                }
            if (TestType == Types.Word)
                for (int i = 0; i < _words.GetLength(0); i++)
                {
                    _hardwareCrc32.ComputeHash(_words[i]);
                    a++;
                }
            if (TestType == Types.LongText)
                _hardwareCrc32.ComputeHash(_longText);
            return a;
        }
    }
}

/*
 BenchmarkDotNet=v0.12.1, OS=Windows 10.0.19042
AMD Ryzen 7 3700X, 1 CPU, 16 logical and 8 physical cores
.NET Core SDK=5.0.201
  [Host]    : .NET Core 5.0.4 (CoreCLR 5.0.421.11614, CoreFX 5.0.421.11614), X64 RyuJIT
  MediumRun : .NET Core 5.0.4 (CoreCLR 5.0.421.11614, CoreFX 5.0.421.11614), X64 RyuJIT

Job=MediumRun  IterationCount=15  LaunchCount=2
WarmupCount=10

|      Method |      TestType |       Mean |      Error |     StdDev |     Median | Kurtosis | Skewness | Rank | Baseline |      Gen 0 | Gen 1 | Gen 2 |   Allocated |
|------------ |-------------- |-----------:|-----------:|-----------:|-----------:|---------:|---------:|-----:|--------- |-----------:|------:|------:|------------:|
|        Sha1 |          Word | 297.964 ms |  3.9637 ms |  5.8099 ms | 298.940 ms |    2.758 |   0.6036 |   33 |       No | 16000.0000 |     - |     - | 141832488 B |
|         Md5 |          Word | 303.087 ms |  2.7507 ms |  3.9450 ms | 301.594 ms |    2.858 |   1.0571 |   33 |       No | 15000.0000 |     - |     - | 130635288 B |
|      Sha512 |          Word | 392.388 ms |  4.4923 ms |  6.5848 ms | 392.433 ms |    1.720 |   0.0057 |   35 |       No | 23000.0000 |     - |     - | 197818488 B |
|      Blake2 |          Word | 678.490 ms |  7.7336 ms | 11.5752 ms | 678.755 ms |    2.489 |   0.2373 |   36 |       No | 72000.0000 |     - |     - | 608382480 B |
|        City |          Word |  51.802 ms |  0.9255 ms |  1.3853 ms |  51.343 ms |    4.405 |   1.1634 |   21 |       No |  7100.0000 |     - |     - |  59718400 B |
|        Fnv1 |          Word |  57.707 ms |  0.8250 ms |  1.1832 ms |  57.767 ms |    2.248 |   0.1289 |   23 |       No | 14666.6667 |     - |     - | 123169200 B |
|       Fnv1A |          Word |  65.829 ms |  2.7842 ms |  4.0810 ms |  64.694 ms |    3.236 |   0.9009 |   25 |       No | 14666.6667 |     - |     - | 123169200 B |
|    Jenkins3 |          Word |  72.210 ms |  3.0167 ms |  4.4218 ms |  72.294 ms |    1.915 |   0.1134 |   26 |       No | 16500.0000 |     - |     - | 138098800 B |
|     Murmur3 |          Word |  60.475 ms |  1.2744 ms |  1.8680 ms |  60.290 ms |    1.934 |   0.3581 |   24 |       No | 16000.0000 |     - |     - | 134366400 B |
|     Pearson |          Word |  81.062 ms |  1.1622 ms |  1.6292 ms |  81.075 ms |    2.855 |   0.6971 |   27 |       No | 12857.1429 |     - |     - | 108239600 B |
|      XxHash |          Word |  86.047 ms |  1.2216 ms |  1.8285 ms |  85.875 ms |    2.336 |   0.6202 |   28 |       No | 20666.6667 |     - |     - | 173538911 B |
|    Crc8Hash |          Word | 115.822 ms |  1.5054 ms |  2.2531 ms | 114.915 ms |    2.513 |   0.7208 |   29 |       No | 16800.0000 |     - |     - | 141834352 B |
|   Crc16Hash |          Word | 113.993 ms |  2.3850 ms |  3.5697 ms | 113.806 ms |    2.011 |   0.0743 |   29 |       No | 16800.0000 |     - |     - | 141834352 B |
|  Crc32Force |          Word |  10.111 ms |  0.0489 ms |  0.0732 ms |  10.083 ms |    4.470 |   1.3787 |   11 |       No |          - |     - |     - |           - |
| Crc32CForce |          Word |  10.032 ms |  0.0297 ms |  0.0435 ms |  10.035 ms |    2.102 |  -0.0025 |   11 |       No |          - |     - |     - |           - |
|        Sha1 | ShortSentence | 142.005 ms |  1.2032 ms |  1.7255 ms | 141.901 ms |    2.734 |   0.4319 |   30 |       No |  7750.0000 |     - |     - |  65946790 B |
|         Md5 | ShortSentence | 145.969 ms |  2.8884 ms |  4.3232 ms | 146.507 ms |    1.539 |  -0.0437 |   31 |       No |  7250.0000 |     - |     - |  60740470 B |
|      Sha512 | ShortSentence | 195.901 ms | 17.3449 ms | 25.9611 ms | 182.469 ms |    3.620 |   1.4581 |   32 |       No | 10666.6667 |     - |     - |  91978552 B |
|      Blake2 | ShortSentence | 318.260 ms |  4.7568 ms |  6.3503 ms | 317.516 ms |    1.699 |  -0.0010 |   34 |       No | 33000.0000 |     - |     - | 282878304 B |
|        City | ShortSentence |  24.562 ms |  0.3847 ms |  0.5393 ms |  24.731 ms |    2.247 |  -0.7712 |   16 |       No |  3312.5000 |     - |     - |  27767040 B |
|        Fnv1 | ShortSentence |  27.682 ms |  0.9425 ms |  1.3212 ms |  28.263 ms |    1.401 |  -0.0037 |   17 |       No |  6843.7500 |     - |     - |  57269520 B |
|       Fnv1A | ShortSentence |  28.019 ms |  0.6472 ms |  0.9688 ms |  28.027 ms |    3.466 |   0.5900 |   17 |       No |  6812.5000 |     - |     - |  57269520 B |
|    Jenkins3 | ShortSentence |  35.513 ms |  0.6719 ms |  1.0057 ms |  35.468 ms |    3.211 |   0.3412 |   19 |       No |  7666.6667 |     - |     - |  64211280 B |
|     Murmur3 | ShortSentence |  30.695 ms |  1.0400 ms |  1.5566 ms |  30.148 ms |    2.436 |   0.8934 |   18 |       No |  7468.7500 |     - |     - |  62475840 B |
|     Pearson | ShortSentence |  42.064 ms |  0.9897 ms |  1.4194 ms |  42.468 ms |    1.879 |  -0.1768 |   20 |       No |  6000.0000 |     - |     - |  50327760 B |
|      XxHash | ShortSentence |  42.896 ms |  0.8349 ms |  1.2496 ms |  42.316 ms |    2.608 |   0.8460 |   20 |       No |  9545.4545 |     - |     - |  80376216 B |
|    Crc8Hash | ShortSentence |  56.638 ms |  0.6624 ms |  0.9500 ms |  56.409 ms |    3.687 |   1.2242 |   22 |       No |  7800.0000 |     - |     - |  65946731 B |
|   Crc16Hash | ShortSentence |  56.593 ms |  1.3896 ms |  2.0799 ms |  55.918 ms |    3.756 |   1.4045 |   22 |       No |  7800.0000 |     - |     - |  65946731 B |
|  Crc32Force | ShortSentence |   5.180 ms |  0.0673 ms |  0.1007 ms |   5.156 ms |    3.224 |   0.9949 |    9 |       No |          - |     - |     - |           - |
| Crc32CForce | ShortSentence |   5.237 ms |  0.0462 ms |  0.0678 ms |   5.232 ms |    2.527 |   0.4590 |    9 |       No |          - |     - |     - |           - |
|        Sha1 |      LongText |   3.454 ms |  0.0157 ms |  0.0234 ms |   3.450 ms |    2.219 |   0.5088 |    6 |       No |          - |     - |     - |       304 B |
|         Md5 |      LongText |   4.421 ms |  0.0424 ms |  0.0634 ms |   4.419 ms |    1.845 |   0.4277 |    7 |       No |          - |     - |     - |       280 B |
|      Sha512 |      LongText |   4.770 ms |  0.0993 ms |  0.1487 ms |   4.773 ms |    2.933 |   0.7487 |    8 |       No |          - |     - |     - |       424 B |
|      Blake2 |      LongText |  23.026 ms |  0.1299 ms |  0.1945 ms |  23.003 ms |    1.979 |   0.3790 |   15 |       No |   750.0000 |     - |     - |   6472552 B |
|        City |      LongText |   1.473 ms |  0.0063 ms |  0.0088 ms |   1.473 ms |    3.016 |   0.6086 |    1 |       No |          - |     - |     - |       128 B |
|        Fnv1 |      LongText |   3.773 ms |  0.7305 ms |  1.0934 ms |   3.142 ms |    2.410 |   0.8645 |    6 |       No |          - |     - |     - |       264 B |
|       Fnv1A |      LongText |   3.106 ms |  0.5530 ms |  0.7931 ms |   2.757 ms |    4.711 |   1.8444 |    4 |       No |          - |     - |     - |       264 B |
|    Jenkins3 |      LongText |   3.299 ms |  0.0630 ms |  0.0942 ms |   3.355 ms |    1.532 |  -0.5211 |    5 |       No |          - |     - |     - |       296 B |
|     Murmur3 |      LongText |   1.908 ms |  0.0492 ms |  0.0721 ms |   1.938 ms |    2.581 |  -0.5963 |    3 |       No |          - |     - |     - |       288 B |
|     Pearson |      LongText |  19.296 ms |  0.3208 ms |  0.4601 ms |  19.065 ms |    3.575 |   1.3156 |   14 |       No |          - |     - |     - |       232 B |
|      XxHash |      LongText |   5.490 ms |  0.0596 ms |  0.0874 ms |   5.469 ms |    3.838 |   1.3537 |   10 |       No |          - |     - |     - |       376 B |
|    Crc8Hash |      LongText |  17.575 ms |  0.2721 ms |  0.4073 ms |  17.548 ms |    1.961 |   0.3938 |   12 |       No |          - |     - |     - |       308 B |
|   Crc16Hash |      LongText |  17.884 ms |  0.1293 ms |  0.1769 ms |  17.844 ms |    3.371 |   0.9060 |   13 |       No |          - |     - |     - |       308 B |
|  Crc32Force |      LongText |   1.522 ms |  0.0103 ms |  0.0147 ms |   1.521 ms |    4.965 |   1.2820 |    2 |       No |          - |     - |     - |           - |
| Crc32CForce |      LongText |   1.538 ms |  0.0240 ms |  0.0351 ms |   1.526 ms |    4.075 |   1.3692 |    2 |       No |          - |     - |     - |           - |
 */