using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bogus.DataSets;
using Datatent2.Core.Index.Heap;
using Datatent2.Core.Page;
using Shouldly;
using Xunit;

namespace Datatent2.Core.Tests.Index.Heap
{
    public class HeapKeyTest
    {
        [Fact]
        public void StringKeyTest()
        {
            Lorem lorem = new Lorem();
            Dictionary<int, HeapKey> dictionary = new();

            var b = new byte[8132];
            Span<byte> bytes = new Span<byte>(b);
            int pos = 0;
            for (int i = 0; i < 20; i++)
            {
                PageAddress pageAddress = new PageAddress(1, (byte) (i + 1));
                var str = lorem.Word();
                HeapKey heapKey = new HeapKey(pageAddress, str);
                dictionary.Add(i, heapKey);
                heapKey.Write(bytes, pos);
                pos += heapKey.Length;
            }

            var list = HeapKey.ReadAllKeys(bytes);
            dictionary.Values.Except(list).ShouldBeEmpty();
        }

        [Fact]
        public void ULongKeyTest()
        {
            Lorem lorem = new Lorem();
            var random = new Random();
            Dictionary<int, HeapKey> dictionary = new();

            var b = new byte[8132];
            Span<byte> bytes = new Span<byte>(b);
            int pos = 0;
            for (int i = 0; i < 20; i++)
            {
                PageAddress pageAddress = new PageAddress(1, (byte)(i + 1));
                var str = (ulong) random.Next(1, Int32.MaxValue);
                
                HeapKey heapKey = new HeapKey(pageAddress, str);
                dictionary.Add(i, heapKey);
                heapKey.Write(bytes, pos);
                pos += heapKey.Length;
            }

            var list = HeapKey.ReadAllKeys(bytes);
            dictionary.Values.Except(list).ShouldBeEmpty();
        }
    }
}
