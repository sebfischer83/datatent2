using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Shouldly;
using Xunit;

namespace Datatent2.Core.Tests.Algo
{
    public class FindFirstPageBinarySearchLike
    {
        public int FindFirstSearchLong(Span<byte> spanByte)
        {
            Span<ulong> span = MemoryMarshal.Cast<byte, ulong>(spanByte);
            int min = 0;
            int max = span.Length - 1;
            int index = -1;

            while (min <= max)
            {
                int mid = mid = (int)unchecked((uint)(min + max) >> 1);
                ref var b = ref span[mid];
               
                if (b != ulong.MaxValue)
                {
                    if (mid == 0)
                    {
                        index = 0;
                        break;
                    }

                    ref var b1 = ref span[mid - 1];
                    if (b1 != 0)
                    {
                        index = mid;
                        break;
                    }

                    max = mid - 1;
                }
                else
                {
                    min = mid + 1;
                }
            }

            if (index > -1)
            {
                int res = 0;
                ref var l = ref span[index];
                var count = BitOperations.LeadingZeroCount((ulong)l);
                res = (64 - count) + 1;
                if (index > 0 && res != -1)
                    res += (64 * index);
                return res;
            }

            return index;
        }

   
        [Fact]
        public void TestSearchLong()
        {
            Span<byte> span = new Span<byte>(new byte[8192 - 64]);
            span.Clear();
            var index = FindFirstSearchLong(span);
            index.ShouldBe(1);

            ref byte b = ref span[0];
            b = (byte)(b | (1 << 0));
            index = FindFirstSearchLong(span);
            index.ShouldBe(2);
            b = (byte)(b | (1 << 1));
            index = FindFirstSearchLong(span);
            index.ShouldBe(3);
            span.Clear();

            span.WriteByte(0, 0xFF);
            b = ref span[1];
            b = (byte)(b | (1 << 0));
            index = FindFirstSearchLong(span);
            index.ShouldBe(10);
            span.Clear();

            for (int i = 0; i < 200; i++)
            {
                span.WriteByte(i, 0xFF);
            }

            index = FindFirstSearchLong(span);
            index.ShouldBe(1601);
            span.Clear();
            for (int i = 0; i < 145; i++)
            {
                span.WriteByte(i, 0xFF);
            }
            index = FindFirstSearchLong(span);
            index.ShouldBe(1161);
            span.Clear();
        }
    }
}
