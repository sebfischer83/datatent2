using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Dawn;

namespace Datatent2.Core.Page
{
    [StructLayout(LayoutKind.Explicit, Size = Constants.PAGE_DIRECTORY_ENTRY_SIZE)]
    internal readonly struct SlotEntry
    {
        [FieldOffset(PDE_DATA_OFFSET)]
        public readonly ushort DataOffset;

        [FieldOffset(PDE_DATA_LENGTH)]
        public readonly ushort DataLength;

        private const int PDE_DATA_OFFSET = 0; // ushort 0-1
        private const int PDE_DATA_LENGTH = 2; // ushort 2-3

        public SlotEntry(ushort dataOffset, ushort dataLength)
        {
            DataOffset = dataOffset;
            DataLength = dataLength;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ushort EndPositionOfData()
        {
            return (ushort) (DataOffset + DataLength);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SlotEntry FromBuffer(Span<byte> span)
        {
            return MemoryMarshal.Read<SlotEntry>(span);
        }

        public static void Clear(Span<byte> span, byte index)
        {
            var pos = GetEntryPosition(index);
            span.Slice(pos, Constants.PAGE_DIRECTORY_ENTRY_SIZE).Clear();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsEmpty(Span<byte> span, int offset)
        {
            var i = MemoryMarshal.Read<int>(span.Slice(offset));

            return i == 0;
        }

        public static SlotEntry FromBuffer(Span<byte> span, int offset)
        {
            Guard.Argument(offset).Min(0);
            return FromBuffer(span.Slice(offset));
        }

        public static SlotEntry FromBuffer(Span<byte> span, byte index)
        {
            var offset = (int)GetEntryPosition(index);
            Guard.Argument(offset).Min(0);
            return FromBuffer(span.Slice(offset));
        }

        public void ToBuffer(Span<byte> span)
        {
            Guard.Argument(span.Length).Min(Constants.PAGE_DIRECTORY_ENTRY_SIZE);
            SlotEntry a = this;
            MemoryMarshal.Write(span, ref a);
        }

        public void ToBuffer(Span<byte> span, int offset)
        {
            Guard.Argument(offset).GreaterThan(0);
            ToBuffer(span.Slice(offset));
        }

        public static ushort GetEntryPosition(byte id)
        {
            return (ushort)(Constants.PAGE_SIZE -
                             (id * Constants.PAGE_DIRECTORY_ENTRY_SIZE));
        }
    }
}
