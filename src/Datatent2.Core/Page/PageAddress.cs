using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Dawn;

namespace Datatent2.Core.Page
{
    [StructLayout(LayoutKind.Explicit, Size = Constants.PAGE_ADDRESS_SIZE)]
    internal readonly struct PageAddress
    {
        [FieldOffset(PA_PAGE_ID)]
        public readonly uint PageId;

        [FieldOffset(PA_BLOCK)]
        public readonly byte BlockIndex;

        [FieldOffset(PA_IS_EXTENSION_BLOCK)]
        public readonly bool IsExtensionBlock;

        private const int PA_PAGE_ID = 0; // 0-3 uint
        private const int PA_BLOCK = 4; // 4 ushort
        private const int PA_IS_EXTENSION_BLOCK = 5; // 5 bool (byte)

        public static PageAddress Empty { get; } = new PageAddress(uint.MaxValue, byte.MaxValue, false);

        public PageAddress(uint pageId, byte blockIndex, bool isExtensionBlock)
        {
            PageId = pageId;
            BlockIndex = blockIndex;
            IsExtensionBlock = isExtensionBlock;
        }

        public bool IsEmpty()
        {
            return IsExtensionBlock == false && PageId == 0 && PageId == uint.MaxValue && BlockIndex == byte.MaxValue;
        }

        public static PageAddress FromBuffer(Span<byte> span)
        {
            return MemoryMarshal.Read<PageAddress>(span);
        }

        public static PageAddress FromBuffer(Span<byte> span, int offset)
        {
            Guard.Argument(offset).GreaterThan(0);
            return FromBuffer(span.Slice(offset));
        }

        public void ToBuffer(Span<byte> span)
        {
            Guard.Argument(span.Length + 1).GreaterThan(Constants.PAGE_ADDRESS_SIZE);
            PageAddress a = this;
            MemoryMarshal.Write(span, ref a);
        }

        public void ToBuffer(Span<byte> span, int offset)
        {
            Guard.Argument(offset).GreaterThan(0);
            ToBuffer(span.Slice(offset));
        }
    }
}
