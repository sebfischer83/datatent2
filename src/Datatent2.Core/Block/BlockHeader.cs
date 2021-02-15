using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Datatent2.Core.Page;
using Dawn;

namespace Datatent2.Core.Block
{
    [StructLayout(LayoutKind.Explicit, Size = Constants.BLOCK_HEADER_SIZE)]
    internal readonly struct BlockHeader
    {
        [FieldOffset(B_PAGE_ADDRESS)]
        public readonly PageAddress Address;

        [FieldOffset(B_PAGE_DATA_LENTGH)]
        public readonly ushort DataLength;

        private const int B_PAGE_ADDRESS = 0; // 0-7 PageAddress 8 byte
        private const int B_PAGE_DATA_LENTGH = Constants.PAGE_ADDRESS_SIZE; // 8-9

        public BlockHeader(PageAddress pageAddress, ushort dataLength)
        {
            Address = pageAddress;
            DataLength = dataLength;
        }

        public static BlockHeader FromBuffer(Span<byte> span)
        {
            return MemoryMarshal.Read<BlockHeader>(span);
        }

        public static BlockHeader FromBuffer(Span<byte> span, int offset)
        {
            Guard.Argument(offset).GreaterThan(0);
            return FromBuffer(span.Slice(offset));
        }

        public void ToBuffer(Span<byte> span)
        {
            Guard.Argument(span.Length + 1).GreaterThan(Constants.BLOCK_HEADER_SIZE);
            BlockHeader a = this;
            MemoryMarshal.Write(span, ref a);
        }

        public void ToBuffer(Span<byte> span, int offset)
        {
            Guard.Argument(offset).GreaterThan(0);
            ToBuffer(span.Slice(offset));
        }
    }
}
