using System;
using System.Collections.Generic;
using System.Data.HashFunction.Blake2;
using System.Data.HashFunction.CityHash;
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

namespace Datatent2.AlgoBench.Hash
{
    [HtmlExporter, CsvExporter(), CsvMeasurementsExporter(),
     RankColumn(), KurtosisColumn, SkewnessColumn, StdDevColumn, MeanColumn, MedianColumn, BaselineColumn, MediumRunJob, MemoryDiagnoser, Orderer(SummaryOrderPolicy.FastestToSlowest, MethodOrderPolicy.Declared)]
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
    }
}
