// # SPDX-License-Identifier: MIT
// # Copyright 2021
// # Sebastian Fischer sebfischer@gmx.net

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Datatent2.Contracts;
using Datatent2.Core.Page;
using Dawn;

namespace Datatent2.Core.Block
{
    /// <summary>
    /// The header of a block
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = Constants.BLOCK_HEADER_SIZE)]
    internal readonly struct BlockHeader
    {
        /// <summary>
        /// Is this a following block or the first one?
        /// </summary>
        [FieldOffset(BH_PAGE_IS_FOLLOWING_BLOCK)]
        public readonly bool IsFollowingBlock;

        /// <summary>
        /// The address of the next block
        /// </summary>
        [FieldOffset(BH_PAGE_ADDRESS)]
        public readonly PageAddress NextBlockAddress;

        /// <summary>
        /// The offset of the field in the structure
        /// </summary>
        private const int BH_PAGE_IS_FOLLOWING_BLOCK = 0; // 0 byte
        /// <summary>
        /// The offset of the field in the structure
        /// </summary>
        private const int BH_PAGE_ADDRESS = 1; // 1-8 PageAddress 8 byte

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="pageAddress"></param>
        /// <param name="isFollowingBlock"></param>
        public BlockHeader(PageAddress pageAddress, bool isFollowingBlock)
        {
            NextBlockAddress = pageAddress;
            IsFollowingBlock = isFollowingBlock;
        }

        /// <summary>
        /// Restore the header from a buffer
        /// </summary>
        /// <param name="span"></param>
        /// <returns></returns>
        public static BlockHeader FromBuffer(Span<byte> span)
        {
            return MemoryMarshal.Read<BlockHeader>(span);
        }

        /// <summary>
        /// Restore the header from a buffer
        /// </summary>
        /// <param name="span"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        public static BlockHeader FromBuffer(Span<byte> span, int offset)
        {
            return FromBuffer(span[offset..]);
        }

        /// <summary>
        /// Save the header to a buffer
        /// </summary>
        /// <param name="span"></param>
        public void ToBuffer(Span<byte> span)
        {
            BlockHeader a = this;
            MemoryMarshal.Write(span, ref a);
        }

        /// <summary>
        /// Save the header to a buffer
        /// </summary>
        public void ToBuffer(Span<byte> span, int offset)
        {
            ToBuffer(span[offset..]);
        }
    }
}
