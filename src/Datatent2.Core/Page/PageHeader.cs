using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using ConsoleTableExt;
using Dawn;
using ObjectLayoutInspector;

namespace Datatent2.Core.Page
{
    [StructLayout(LayoutKind.Explicit, Size = Constants.PAGE_HEADER_SIZE)] 
    internal readonly struct PageHeader
    {
        /// <summary>
        /// The id of the page, a continuous increasing number.
        /// Starts with 0 for the header of the database. 
        /// </summary>
        [FieldOffset(PAGE_ID)]
        public readonly uint PageId;

        /// <summary>
        /// The type of the page.
        /// <see cref="PageType"/>
        /// </summary>
        [FieldOffset(PAGE_TYPE)]
        public readonly PageType Type;

        /// <summary>
        /// Link to the previous page of the same type.
        /// If none then uint.MaxValue.
        /// </summary>
        [FieldOffset(PAGE_PREV_ID)]
        public readonly uint PrevPageId;

        /// <summary>
        /// Link to the next page of the same type.
        /// If none then uint.MaxValue.
        /// </summary>
        [FieldOffset(PAGE_NEXT_ID)]
        public readonly uint NextPageId;

        [FieldOffset(PAGE_USED_BYTES)]
        public readonly ushort UsedBytes;

        [FieldOffset(PAGE_NUMBER_OF_ITEMS)]
        public readonly byte ItemCount;
        
        [FieldOffset(PAGE_NEXT_FREE_POSITION)]
        public readonly ushort NextFreePosition;

        [FieldOffset(PAGE_UNALIGNED_FREE_BYTES)]
        public readonly ushort UnalignedFreeBytes;

        [FieldOffset(PAGE_HIGHEST_ENTRY_ID)]
        public readonly byte HighestEntryId;

        private const int PAGE_ID = 0; // 0-3 uint
        private const int PAGE_TYPE = 4; // 4 byte (enum PageType)
        private const int PAGE_PREV_ID = 5; // 5-8 uint 
        private const int PAGE_NEXT_ID = 9; // 9-12 uint
        private const int PAGE_USED_BYTES = 13; // 13-14 ushort
        private const int PAGE_NUMBER_OF_ITEMS = 15; // 15 byte
        private const int PAGE_NEXT_FREE_POSITION = 16; // 16-17 ushort
        private const int PAGE_UNALIGNED_FREE_BYTES = 18; // 18-19 ushort
        private const int PAGE_HIGHEST_ENTRY_ID = 20; // 20 byte

        public PageHeader(uint pageId, PageType type, uint prevPageId, uint nextPageId, ushort usedBytes, byte itemCount, ushort nextFreePosition, ushort unalignedFreeBytes, byte highestEntryId)
        {
            PageId = pageId;
            Type = type;
            PrevPageId = prevPageId;
            NextPageId = nextPageId;
            UsedBytes = usedBytes;
            ItemCount = itemCount;
            NextFreePosition = nextFreePosition;
            UnalignedFreeBytes = unalignedFreeBytes;
            HighestEntryId = highestEntryId;
        }

        public PageHeader(uint pageId, PageType type)
        {
            PageId = pageId;
            Type = type;
            PrevPageId = ushort.MaxValue;
            NextPageId = ushort.MaxValue;
            UsedBytes = 0;
            ItemCount = 0;
            NextFreePosition = Constants.PAGE_HEADER_SIZE;
            UnalignedFreeBytes = 0;
            HighestEntryId = 0;
        }

        public static PageHeader FromBuffer(Span<byte> span)
        {
            return MemoryMarshal.Read<PageHeader>(span);
        }

        public static PageHeader FromBuffer(Span<byte> span, int offset)
        {
            Guard.Argument(offset).Min(0);
            return FromBuffer(span.Slice(offset));
        }

        public void ToBuffer(Span<byte> span)
        {
            Guard.Argument(span.Length).Min(Constants.PAGE_HEADER_SIZE);
            PageHeader a = this;
            MemoryMarshal.Write(span, ref a);
        }

        public void ToBuffer(Span<byte> span, int offset)
        {
            Guard.Argument(offset).Min(0);
            ToBuffer(span.Slice(offset));
        }

        public override string ToString()
        {
            var tableData = new List<List<object>>()
            {
                new List<object>{nameof(ItemCount), ItemCount}
            };

            return ConsoleTableBuilder
                .From(tableData)
                .WithTitle($"{Enum.GetName(typeof(PageType), Type)}:{PageId}", ConsoleColor.Yellow, ConsoleColor.DarkGray)
                .WithColumn("Property", "Value").Export().ToString();
        }
    }

    internal enum PageType : byte
    {
        Header = 1,
        Data = 2,
        Index = 3,
        Directory
    }
}
