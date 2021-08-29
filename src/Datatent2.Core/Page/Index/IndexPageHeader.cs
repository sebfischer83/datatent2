// # SPDX-License-Identifier: MIT
// # Copyright 2021
// # Sebastian Fischer sebfischer@gmx.net

using System;
using System.Runtime.InteropServices;
using Datatent2.Contracts;
using Datatent2.Core.Index;
using Datatent2.Core.Services.Index;

namespace Datatent2.Core.Page.Index
{
    [StructLayout(LayoutKind.Explicit, Size = Constants.PAGE_SPECIFIC_HEADER_SIZE)]
    internal readonly struct IndexPageHeader
    {
        [FieldOffset(TYPE)]
        public readonly IndexType Type;

        [FieldOffset(NODES_COUNT)]
        public readonly ushort NodesCount;

        private const int TYPE = 0; // byte index type
        private const int NODES_COUNT = 1; // 1-2 ushort

        public IndexPageHeader(IndexType type)
        {
            Type = type;
            NodesCount = 0;
        }

        public IndexPageHeader(IndexType type, ushort nodesCount)
        {
            Type = type;
            NodesCount = nodesCount;
        }

        public static IndexPageHeader FromBuffer(Span<byte> span)
        {
            return MemoryMarshal.Read<IndexPageHeader>(span);
        }

        public static IndexPageHeader FromBuffer(Span<byte> span, int offset)
        {
            return FromBuffer(span[offset..]);
        }

        public void ToBuffer(Span<byte> span)
        {
            IndexPageHeader a = this;
            MemoryMarshal.Write(span, ref a);
        }

        public void ToBuffer(Span<byte> span, int offset)
        {
            ToBuffer(span[offset..]);
        }
    }
}