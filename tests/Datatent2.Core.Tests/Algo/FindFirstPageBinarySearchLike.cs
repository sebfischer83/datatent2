using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
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

        private static Vector<byte> _testVector;

        static FindFirstPageBinarySearchLike()
        {
            List<byte> list = new List<byte>();
            for (int i = 0; i < Vector<byte>.Count; i++)
            {
                list.Add(0xFF);
            }

            _testVector = new Vector<byte>(list.ToArray());
        }

        public int FindFirstEmptyVectorT(Span<byte> span)
        {
            int iterations = Math.DivRem(span.Length, Vector<byte>.Count, out int nonAligned);
            for (int i = 0; i < iterations; i++)
            {
                var vector = new Vector<byte>(span[(Vector<byte>.Count * i)..]);
                if (vector != _testVector)
                {
                    int bitIndex = Vector<byte>.Count * i * 8;
                    var u64vector = Vector.AsVectorUInt64(vector); // handle LZC with uiint here
                    for (int j = 0; j < Vector<ulong>.Count; j++)
                    {
                        var l = u64vector[j];
                        if (l == ulong.MaxValue)
                            continue;

                        int res = 0;
                        var count = BitOperations.LeadingZeroCount((ulong)l);
                        res = (64 - count) + 1;
                        if (j > 0 && res != -1)
                            res += (64 * j);
                        return res + bitIndex;
                    }
                }
            }

            return -1;
        }

        [Fact]
        public void TestFindFirstEmptyVectorT()
        {
            Span<byte> span = new Span<byte>(new byte[8192 - 64]);
            span.Clear();
            var index = FindFirstEmptyVectorT(span);
            index.ShouldBe(1);

            ref byte b = ref span[0];
            b = (byte)(b | (1 << 0));
            index = FindFirstEmptyVectorT(span);
            index.ShouldBe(2);
            b = (byte)(b | (1 << 1));
            index = FindFirstEmptyVectorT(span);
            index.ShouldBe(3);
            span.Clear();

            span.WriteByte(0, 0xFF);
            b = ref span[1];
            b = (byte)(b | (1 << 0));
            index = FindFirstEmptyVectorT(span);
            index.ShouldBe(10);
            span.Clear();

            for (int i = 0; i < 200; i++)
            {
                span.WriteByte(i, 0xFF);
            }

            index = FindFirstEmptyVectorT(span);
            index.ShouldBe(1601);
            span.Clear();
            for (int i = 0; i < 145; i++)
            {
                span.WriteByte(i, 0xFF);
            }
            index = FindFirstEmptyVectorT(span);
            index.ShouldBe(1161);
            span.Clear();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public unsafe int OctupleSearchLowerBound(uint* spanPtr, Vector256<int> indexes, int sectionLength)
        {
            //Load 8 indexes at once into a Vector256
            var values = Avx2.GatherVector256(spanPtr, indexes, (byte)sizeof(int));

            //How many loaded values have all bits set?
            //If true then set to 0xffffffff else 0
            var isMaxValue = Avx2.CompareEqual(values, Vector256<uint>.AllBitsSet);

            //Take msb of each 32bit element and return them as an int.
            //Then count number of bits that are set and that is equals
            //to the number of loaded values that were all ones.
            var isMaxValueMask = Avx2.MoveMask(isMaxValue.AsSingle());
            var isMaxCount = BitOperations.PopCount((uint)isMaxValueMask);

            //For each loaded vaue that's all ones, a sectionLength
            //number of integers must also be all ones
            return isMaxCount * sectionLength;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public unsafe int FindFirstOctupleSearchOnce(Span<byte> spanByte)
        {
            Span<uint> span = MemoryMarshal.Cast<byte, uint>(spanByte);
            fixed (uint* spanPtr = span)
            {
                const int bitsPerInt = 32;
                const int ints = 8128 / 4;
                const int loadCount = 8;
                const int sections = loadCount + 1;
                const int sectionLength = (ints / sections) + 1; // 225.8 -> 226
                var indexes = Vector256.Create(
                    sectionLength * 1,
                    sectionLength * 2,
                    sectionLength * 3,
                    sectionLength * 4,
                    sectionLength * 5,
                    sectionLength * 6,
                    sectionLength * 7,
                    sectionLength * 8);

                int lowerBound = OctupleSearchLowerBound(spanPtr, indexes, sectionLength);
                int index = lowerBound * bitsPerInt;

                int upperBound = Math.Min(lowerBound + sectionLength + 1, span.Length);
                for (int i = lowerBound; i < upperBound; i++)
                {
                    int bitsSet = BitOperations.PopCount(span[i]);
                    index += bitsSet;
                    if (bitsSet != bitsPerInt)
                    {
                        break;
                    }
                }

                return index + 1;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public unsafe int FindFirstOctupleSearchTwice(Span<byte> spanByte)
        {
            Span<uint> span = MemoryMarshal.Cast<byte, uint>(spanByte);
            fixed (uint* spanPtr = span)
            {
                const int bitsPerInt = 32;
                const int ints = 8128 / 4;
                const int loadCount = 8;
                const int sections = loadCount + 1;
                const int sectionLength1 = (ints / sections) + 1; // 225.8 -> 226
                const int sectionLength2 = (sectionLength1 / sections) + 1; // 25.1 -> 26
                var indexes1 = Vector256.Create(
                    sectionLength1 * 1,
                    sectionLength1 * 2,
                    sectionLength1 * 3,
                    sectionLength1 * 4,
                    sectionLength1 * 5,
                    sectionLength1 * 6,
                    sectionLength1 * 7,
                    sectionLength1 * 8);

                var indexes2 = Vector256.Create(
                    sectionLength2 * 1,
                    sectionLength2 * 2,
                    sectionLength2 * 3,
                    sectionLength2 * 4,
                    sectionLength2 * 5,
                    sectionLength2 * 6,
                    sectionLength2 * 7,
                    sectionLength2 * 8);

                int lowerBound1 = OctupleSearchLowerBound(spanPtr, indexes1, sectionLength1);
                int lowerBound2 = OctupleSearchLowerBound(spanPtr + lowerBound1, indexes2, sectionLength2);

                int lowerBound = lowerBound1 + lowerBound2;
                int upperBound = Math.Min(lowerBound + sectionLength2, span.Length);

                int index = lowerBound * bitsPerInt;
                for (int i = lowerBound; i < upperBound; i++)
                {
                    int bitsSet = BitOperations.PopCount(span[i]);
                    index += bitsSet;
                    if (bitsSet != bitsPerInt)
                    {
                        break;
                    }
                }

                return index + 1;
            }
        }


        [Fact]
        public void OctupleSearchOnceTest()
        {
            Span<byte> span = new Span<byte>(new byte[8192 - 64]);
            span.Clear();
            var index = FindFirstOctupleSearchOnce(span);
            index.ShouldBe(1);

            ref byte b = ref span[0];
            b = (byte)(b | (1 << 0));
            index = FindFirstOctupleSearchOnce(span);
            index.ShouldBe(2);
            b = (byte)(b | (1 << 1));
            index = FindFirstOctupleSearchOnce(span);
            index.ShouldBe(3);
            span.Clear();

            span.WriteByte(0, 0xFF);
            b = ref span[1];
            b = (byte)(b | (1 << 0));
            index = FindFirstOctupleSearchOnce(span);
            index.ShouldBe(10);
            span.Clear();

            for (int i = 0; i < 200; i++)
            {
                span.WriteByte(i, 0xFF);
            }

            index = FindFirstOctupleSearchOnce(span);
            index.ShouldBe(1601);
            span.Clear();
            for (int i = 0; i < 145; i++)
            {
                span.WriteByte(i, 0xFF);
            }
            index = FindFirstOctupleSearchOnce(span);
            index.ShouldBe(1161);
            span.Clear();
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
