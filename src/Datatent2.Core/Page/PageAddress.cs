using System;
using System.Collections.Generic;
using System.Linq;
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
        public readonly ushort Block;

        [FieldOffset(PA_IS_EXTENSION_BLOCK)]
        public readonly bool IsExtensionBlock;

        private const int PA_PAGE_ID = 0; // 0-3 uint
        private const int PA_BLOCK = 4; // 4-5 ushort
        private const int PA_IS_EXTENSION_BLOCK = 6; // 6 bool (byte)

        public PageAddress(uint pageId, ushort block, bool isExtensionBlock)
        {
            PageId = pageId;
            Block = block;
            IsExtensionBlock = isExtensionBlock;
        }

        public static PageAddress FromBuffer(Span<byte> span)
        {
            return MemoryMarshal.Read<PageAddress>(span);
        }

        public void ToBuffer(Span<byte> span)
        {
            Guard.Argument(span.Length + 1).GreaterThan(Constants.PAGE_ADDRESS_SIZE);
            PageAddress a = this;
            MemoryMarshal.Write(span, ref a);
        }
    }
}
