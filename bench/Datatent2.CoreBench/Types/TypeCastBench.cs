using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Datatent2.Core.Index.Heap;
using Datatent2.Core.Page;
using Datatent2.CoreBench.Page;

namespace Datatent2.CoreBench.Types
{
    [HtmlExporter, CsvExporter(), CsvMeasurementsExporter(),
     RankColumn(), KurtosisColumn, SkewnessColumn, StdDevColumn, MeanColumn, MedianColumn, BaselineColumn, MediumRunJob, MemoryDiagnoser]
    public class TypeCastBench
    {
        private static string TestString = "fdsf4t";
        private static Guid TestGuid = Guid.NewGuid();


        [Benchmark]
        public uint ChangeTypeULong()
        {
            var res = SearchHeapIndexKeyInternal((ulong)5, new HeapKey(PageAddress.Empty, (ulong)5));
            return res[0].PageId;
        }

        [Benchmark]
        public uint ChangeTypeLong()
        {
            var res = SearchHeapIndexKeyInternal((long)5, new HeapKey(PageAddress.Empty, (long)5));
            return res[0].PageId;
        }

        [Benchmark]
        public uint ChangeTypeString()
        {
            var res = SearchHeapIndexKeyInternal(TestString, new HeapKey(PageAddress.Empty, TestString));
            return res[0].PageId;
        }

        [Benchmark]
        public uint ChangeTypeGuid()
        {
            var res = SearchHeapIndexKeyInternal(TestGuid, new HeapKey(PageAddress.Empty, TestGuid));
            return res[0].PageId;
        }

        [Benchmark]
        public uint DirectULong()
        {
            var res = SearchHeapIndexKeyInternal2((ulong)5, new HeapKey(PageAddress.Empty, (ulong)5));
            return res[0].PageId;
        }

        [Benchmark]
        public uint DirectTypeLong()
        {
            var res = SearchHeapIndexKeyInternal2((long)5, new HeapKey(PageAddress.Empty, (long)5));
            return res[0].PageId;
        }

        [Benchmark]
        public uint DirectString()
        {
            var res = SearchHeapIndexKeyInternal2(TestString, new HeapKey(PageAddress.Empty, TestString));
            return res[0].PageId;
        }

        [Benchmark]
        public uint DirectGuid()
        {
            var res = SearchHeapIndexKeyInternal2(TestGuid, new HeapKey(PageAddress.Empty, TestGuid));
            return res[0].PageId;
        }

        private PageAddress[] SearchHeapIndexKeyInternal2<T>(T key, HeapKey foundKey)
        {
            switch (key)
            {
                case string s:
                    if (string.Equals(s, foundKey.StringValue, StringComparison.Ordinal))
                    {
                        return new[] { foundKey.PageAddress };
                    }
                    break;
                case sbyte sb:
                    if (sb == foundKey.NumericalValue)
                    {
                        return new[] { foundKey.PageAddress };
                    }
                    break;
                case byte bt:
                    if (bt == foundKey.UnsignedNumericalValue)
                    {
                        return new[] { foundKey.PageAddress };
                    }
                    break;
                case short st:
                    if (st == foundKey.NumericalValue)
                    {
                        return new[] { foundKey.PageAddress };
                    }
                    break;
                case int it:
                    if (it == foundKey.NumericalValue)
                    {
                        return new[] { foundKey.PageAddress };
                    }

                    break;
                case long la:
                    if (la == foundKey.NumericalValue)
                    {
                        return new[] { foundKey.PageAddress };
                    }
                    break;
                case ushort us:
                    if (us == foundKey.UnsignedNumericalValue)
                    {
                        return new[] { foundKey.PageAddress };
                    }
                    break;
                case uint ut:
                    if (ut == foundKey.UnsignedNumericalValue)
                    {
                        return new[] { foundKey.PageAddress };
                    }
                    break;
                case ulong ul:
                    if (ul == foundKey.UnsignedNumericalValue)
                    {
                        return new[] { foundKey.PageAddress };
                    }
                    break;
                case Guid g:
                    if (g == foundKey.GuidValue)
                    {
                        return new[] { foundKey.PageAddress };
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(key));
            }
            
            return Array.Empty<PageAddress>();
        }

        private PageAddress[] SearchHeapIndexKeyInternal<T>(T key, HeapKey foundKey)
        {
            switch (key)
            {
                case string s:
                    if (s == foundKey.StringValue)
                    {
                        return new[] { foundKey.PageAddress };
                    }
                    break;
                case sbyte:
                case byte:
                case short:
                case int:
                case long:
                    if ((long)System.Convert.ChangeType(key, TypeCode.Int64) == foundKey.NumericalValue)
                    {
                        return new[] { foundKey.PageAddress };
                    }

                    break;
                case ushort:
                case uint:
                case ulong:
                    if ((ulong)System.Convert.ChangeType(key, TypeCode.UInt64) == foundKey.UnsignedNumericalValue)
                    {
                        return new[] { foundKey.PageAddress };
                    }
                    break;
                case Guid g:
                    if (g == foundKey.GuidValue)
                    {
                        return new[] { foundKey.PageAddress };
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(key));
            }

            return Array.Empty<PageAddress>();
        }
    }
}
