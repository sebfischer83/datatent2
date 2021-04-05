using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using CsvHelper;
using Datatent2.AlgoBench.Hash;
using NReco.Text;

namespace Datatent2.AlgoBench.Find
{
    [HtmlExporter, CsvExporter(), CsvMeasurementsExporter(),
     RankColumn(), KurtosisColumn, SkewnessColumn, StdDevColumn, MeanColumn, MedianColumn, BaselineColumn, MediumRunJob, MemoryDiagnoser]
    public class StringContains
    {
        private string _longText;
        private string[] _tokens;
        private AhoCorasickDoubleArrayTrie<int> _matcher;

        [GlobalSetup]
        public void Setup()
        {
            var path = Path.GetDirectoryName(typeof(StringContains).GetTypeInfo().Assembly.Location);
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

            _longText = stringBuilder.ToString();
            _tokens = new string[] {"temperature", "composer", "consists", "yellow"};
            _matcher = new AhoCorasickDoubleArrayTrie<int>(_tokens.Select(s => new KeyValuePair<string, int>(s, 1)), true);
        }

        [Benchmark(Baseline = true)]
        public long Contains()
        {
            long a = 0;
            for (int i = 0; i < _tokens.Length; i++)
            {
                var search = _tokens[i];
                a += _longText.Contains(search, StringComparison.InvariantCultureIgnoreCase) ? 1 : 0;
            }

            return a;
        }

        [Benchmark(Baseline = false)]
        public long IndexOf()
        {
            long a = 0;
            for (int i = 0; i < _tokens.Length; i++)
            {
                var search = _tokens[i];
                a += _longText.IndexOf(search, StringComparison.InvariantCultureIgnoreCase) > -1 ? 1 : 0;
            }

            return a;
        }

        [Benchmark(Baseline = false)]
        public long AhoCorasick()
        {
            long a = 0;
            _matcher.ParseText(_longText, hit =>
            {
                a++;
            });

            return a;
        }

        [Benchmark(Baseline = false)]
        public long AhoCorasickBuildMatcher()
        {
            long a = 0;
            var matcher = new AhoCorasickDoubleArrayTrie<int>(_tokens.Select(s => new KeyValuePair<string, int>(s, 1)), true);
            matcher.ParseText(_longText, hit =>
            {
                a++;
            });

            return a;
        }
    }
}
