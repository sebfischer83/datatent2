// # SPDX-License-Identifier: MIT
// # Copyright 2021
// # Sebastian Fischer sebfischer@gmx.net

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
    [StructLayout(LayoutKind.Explicit, Size = Constants.BLOCK_HEADER_SIZE, Pack = 1)]
    internal readonly struct BlockHeader
    {
        [FieldOffset(BH_PAGE_IS_FOLLOWING_BLOCK)]
        public readonly bool IsFollowingBlock;

        [FieldOffset(BH_PAGE_ADDRESS)]
        public readonly PageAddress NextBlockAddress;

        [FieldOffset(BH_CHECKSUM)]
        public readonly uint Checksum;

        private const int BH_PAGE_IS_FOLLOWING_BLOCK = 0; // 0 byte
        private const int BH_PAGE_ADDRESS = 1; // 1-4 PageAddress 4 byte
        private const int BH_CHECKSUM = 5; // 5-7 byte 4 byte

        public BlockHeader(PageAddress pageAddress, bool isFollowingBlock, uint checksum)
        {
            NextBlockAddress = pageAddress;
            IsFollowingBlock = isFollowingBlock;
            Checksum = checksum;
        }

        public static BlockHeader FromBuffer(Span<byte> span)
        {
            return MemoryMarshal.Read<BlockHeader>(span);
        }

        public static BlockHeader FromBuffer(Span<byte> span, int offset)
        {
            return FromBuffer(span.Slice(offset));
        }

        public void ToBuffer(Span<byte> span)
        {
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
