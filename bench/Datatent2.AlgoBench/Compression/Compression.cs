using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using CsvHelper;
using Datatent2.AlgoBench.Hash;
using K4os.Compression.LZ4;
using K4os.Compression.LZ4.Encoders;

namespace Datatent2.AlgoBench.Compression
{
    [HtmlExporter, CsvExporter(), CsvMeasurementsExporter(),
     RankColumn(), KurtosisColumn, SkewnessColumn, StdDevColumn, MeanColumn, MedianColumn, BaselineColumn, MediumRunJob, MemoryDiagnoser, Orderer(SummaryOrderPolicy.FastestToSlowest, MethodOrderPolicy.Declared)]
    public class Compression
    {
        public enum Types
        {
            Word,
            ShortSentence,
            LongText,
            Guid
        }

        private byte[][] _sentences;
        private byte[][] _words;
        private byte[] _longText;

        [Params(Types.ShortSentence, Types.Word)]
        public Types TestType;

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
            int i = 0;
            while (csv.Read())
            {
                i++;

                if (i > 500)
                    break;
                var str = csv.GetField<string>(6);
                stringBuilder.Append(" " + str);
                list.Add(Encoding.UTF8.GetBytes(str));
            }
            _sentences = list.ToArray();
            _longText = Encoding.UTF8.GetBytes(stringBuilder.ToString());

            list.Clear();
            path = Path.GetDirectoryName(typeof(StringHash).GetTypeInfo().Assembly.Location);
            using var streamReader = new StreamReader(new FileStream(Path.Combine(path, "Files", "words.txt"), FileMode.Open));
            i = 0;
            while (!streamReader.EndOfStream)
            {
                i++;
                if (i > 500)
                    break;
                list.Add(Encoding.UTF8.GetBytes(streamReader.ReadLine() ?? ""));
            }

            _words = list.ToArray();
        }

        [Benchmark]
        public int CompressBrotliDefault()
        {
            int a = 0;
            int t = 0;
            Span<byte> span = new Span<byte>();

            if (TestType == Types.ShortSentence)
                for (int i = 0; i < _sentences.GetLength(0); i++)
                {
                    Span<byte> compressed = new Span<byte>(new byte[BrotliEncoder.GetMaxCompressedLength(_sentences[i].Length)]);
                    BrotliEncoder.TryCompress(_sentences[i], compressed, out t);
                    a++;
                }
            if (TestType == Types.Word)
                for (int i = 0; i < _words.GetLength(0); i++)
                {
                    Span<byte> compressed = new Span<byte>(new byte[BrotliEncoder.GetMaxCompressedLength(_words[i].Length)]);
                    BrotliEncoder.TryCompress(_words[i], compressed, out t);
                    a++;
                }

            if (TestType == Types.LongText)
            {
                Span<byte> compressed = new Span<byte>(new byte[BrotliEncoder.GetMaxCompressedLength(_longText.Length)]);
                BrotliEncoder.TryCompress(_longText, compressed, out t);
            }

            return a;
        }

        [Benchmark]
        public int CompressBrotliFast()
        {
            int a = 0;
            int t = 0;
            
            if (TestType == Types.ShortSentence)
                for (int i = 0; i < _sentences.GetLength(0); i++)
                {
                    Span<byte> compressed = new Span<byte>(new byte[BrotliEncoder.GetMaxCompressedLength(_sentences[i].Length)]);
                    BrotliEncoder.TryCompress(_sentences[i], compressed, out t, 1, 10);
                    a++;
                }
            if (TestType == Types.Word)
                for (int i = 0; i < _words.GetLength(0); i++)
                {
                    Span<byte> compressed = new Span<byte>(new byte[BrotliEncoder.GetMaxCompressedLength(_words[i].Length)]);
                    BrotliEncoder.TryCompress(_words[i], compressed, out t, 1, 10);
                    a++;
                }

            if (TestType == Types.LongText)
            {
                Span<byte> compressed = new Span<byte>(new byte[BrotliEncoder.GetMaxCompressedLength(_longText.Length)]);
                BrotliEncoder.TryCompress(_longText, compressed, out t, 1, 10);
            }

            return a;
        }

        [Benchmark]
        public int CompressBrotli11()
        {
            int a = 0;
            int t = 0;
            Span<byte> span = new Span<byte>();

            if (TestType == Types.ShortSentence)
                for (int i = 0; i < _sentences.GetLength(0); i++)
                {
                    Span<byte> compressed = new Span<byte>(new byte[BrotliEncoder.GetMaxCompressedLength(_sentences[i].Length)]);
                    BrotliEncoder.TryCompress(_sentences[i], compressed, out t, 11, 20);
                    a++;
                }
            if (TestType == Types.Word)
                for (int i = 0; i < _words.GetLength(0); i++)
                {
                    Span<byte> compressed = new Span<byte>(new byte[BrotliEncoder.GetMaxCompressedLength(_words[i].Length)]);
                    BrotliEncoder.TryCompress(_words[i], compressed, out t, 11, 20);
                    a++;
                }

            if (TestType == Types.LongText)
            {
                Span<byte> compressed = new Span<byte>(new byte[BrotliEncoder.GetMaxCompressedLength(_longText.Length)]);
                BrotliEncoder.TryCompress(_longText, compressed, out t, 11, 20);
            }

            return a;
        }

        [Benchmark]
        public int CompressLz4Opt()
        {
            int a = 0;
            int t = 0;
            Span<byte> span = new Span<byte>();

            if (TestType == Types.ShortSentence)
                for (int i = 0; i < _sentences.GetLength(0); i++)
                {
                    LZ4Pickler.Pickle(_sentences[i], LZ4Level.L10_OPT);
                    a++;
                }
            if (TestType == Types.Word)
                for (int i = 0; i < _words.GetLength(0); i++)
                {
                    LZ4Pickler.Pickle(_words[i], LZ4Level.L10_OPT);
                    a++;
                }

            if (TestType == Types.LongText)
                LZ4Pickler.Pickle(_longText, LZ4Level.L10_OPT);
            return a;
        }

        [Benchmark]
        public int CompressLz4Fast()
        {
            int a = 0;

            if (TestType == Types.ShortSentence)
                for (int i = 0; i < _sentences.GetLength(0); i++)
                {
                    LZ4Pickler.Pickle(_sentences[i], LZ4Level.L00_FAST);
                    a++;
                }
            if (TestType == Types.Word)
                for (int i = 0; i < _words.GetLength(0); i++)
                {
                    LZ4Pickler.Pickle(_words[i], LZ4Level.L00_FAST);
                    a++;
                }

            if (TestType == Types.LongText)
                LZ4Pickler.Pickle(_longText, LZ4Level.L00_FAST);
            return a;
        }
    }
}
