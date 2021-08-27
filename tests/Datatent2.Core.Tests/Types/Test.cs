using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Datatent2.Core.Index.Heap;
using Datatent2.Core.Page;
using Xunit;

namespace Datatent2.Core.Tests.Types
{
    public class Test
    {
        //[Fact]
        //public void Test2()
        //{
        //    SearchHeapIndexKeyInternal((ulong)5, new HeapKey(PageAddress.Empty, (ulong)5));
        //}
        

        //internal Key[] Test<T>(T key, Key foundKey)
        //{
        //    switch (key)
        //    {
        //        case string s:
        //            if (s == foundKey.StringValue)
        //            {
        //                return new[] { foundKey };
        //            }
        //            break;
        //        case sbyte:
        //        case byte:
        //        case short:
        //        case int:
        //        case long l:
        //            if ((long)System.Convert.ChangeType(key, TypeCode.Int64) == foundKey.NumericalValue)
        //            {

        //                return new[] { foundKey };
        //            }

        //            break;
        //        case ushort:
        //        case uint:
        //        case ulong u:
        //            if ((ulong)System.Convert.ChangeType(key, TypeCode.UInt64) == foundKey.UnsignedNumericalValue)
        //            {
        //                var a = u;
        //                return new[] { foundKey };
        //            }
        //            break;
        //        case Guid g:
        //            if (g == foundKey.GuidValue)
        //            {
        //                return new[] { foundKey };
        //            }
        //            break;
        //        default:
        //            throw new ArgumentOutOfRangeException(nameof(key));
        //    }

        //    return Array.Empty<Key>();
        //}
    }
}
