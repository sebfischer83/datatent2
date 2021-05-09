// # SPDX-License-Identifier: MIT
// # Copyright 2021
// # Sebastian Fischer sebfischer@gmx.net

using System;
using System.Runtime.InteropServices;
using Datatent2.Contracts;
using Dawn;

namespace Datatent2.Core.Page
{
    /// <summary>
    /// The position of a piece of data in a page
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = Constants.PAGE_ADDRESS_SIZE)]
    internal readonly struct PageAddress
    {
        [FieldOffset(PA_PAGE_ID)]
        public readonly uint PageId;

        [FieldOffset(PA_BLOCK)]
        public readonly byte SlotId;

        private const int PA_PAGE_ID = 0; // 0-3 uint
        private const int PA_BLOCK = 4; // 4 byte

        public static PageAddress Empty { get; } = new PageAddress(0, 0);

        public PageAddress(uint pageId, byte slotId)
        {
            PageId = pageId;
            SlotId = slotId;
        }

        public bool IsEmpty()
        {
            return PageId == 0 && SlotId == 0;
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
            Guard.Argument(span.Length).Min(Constants.PAGE_ADDRESS_SIZE);
            PageAddress a = this;
            MemoryMarshal.Write(span, ref a);
        }

        public void ToBuffer(Span<byte> span, int offset)
        {
            Guard.Argument(offset).GreaterThan(0);
            ToBuffer(span.Slice(offset));
        }

        public override string ToString()
        {
            return $"<{PageId}:{SlotId}>";
        }

        public bool Equals(PageAddress other)
        {
            return PageId == other.PageId && SlotId == other.SlotId;
        }

        public override bool Equals(object? obj)
        {
            return obj is PageAddress other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(PageId, SlotId);
        }
    }
}
