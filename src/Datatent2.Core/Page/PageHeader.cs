// # SPDX-License-Identifier: MIT
// # Copyright 2021
// # Sebastian Fischer sebfischer@gmx.net

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using ConsoleTableExt;
using Datatent2.Contracts;
using Dawn;
using ObjectLayoutInspector;

namespace Datatent2.Core.Page
{
    [StructLayout(LayoutKind.Explicit, Size = Constants.PAGE_COMMON_HEADER_SIZE)] 
    internal readonly struct PageHeader
    {
        /// <summary>
        /// The id of the page, a continuous increasing number.
        /// Starts with 0 for the header of the database. 
        /// </summary>
        [FieldOffset(ID)]
        public readonly uint PageId;

        /// <summary>
        /// The type of the page.
        /// <see cref="PageType"/>
        /// </summary>
        [FieldOffset(TYPE)]
        public readonly PageType Type;

        /// <summary>
        /// Link to the previous page of the same type.
        /// If none then uint.MaxValue.
        /// </summary>
        [FieldOffset(PREV_ID)]
        public readonly uint PrevPageId;

        /// <summary>
        /// Link to the next page of the same type.
        /// If none then uint.MaxValue.
        /// </summary>
        [FieldOffset(NEXT_ID)]
        public readonly uint NextPageId;

        [FieldOffset(USED_BYTES)]
        public readonly ushort UsedBytes;

        [FieldOffset(NUMBER_OF_ITEMS)]
        public readonly byte ItemCount;
        
        [FieldOffset(NEXT_FREE_POSITION)]
        public readonly ushort NextFreePosition;

        [FieldOffset(UNALIGNED_FREE_BYTES)]
        public readonly ushort UnalignedFreeBytes;

        [FieldOffset(HIGHEST_SLOT_ID)]
        public readonly byte HighestSlotId;
        
        private const int ID = 0; // 0-3 uint
        private const int TYPE = 4; // 4 byte (enum PageType)
        private const int PREV_ID = 5; // 5-8 uint 
        private const int NEXT_ID = 9; // 9-12 uint
        private const int USED_BYTES = 13; // 13-14 ushort
        private const int NUMBER_OF_ITEMS = 15; // 15 byte
        private const int NEXT_FREE_POSITION = 16; // 16-17 ushort
        private const int UNALIGNED_FREE_BYTES = 18; // 18-19 ushort
        private const int HIGHEST_SLOT_ID = 20; // 20 byte

        public PageHeader(uint pageId, PageType type, uint prevPageId, uint nextPageId, ushort usedBytes, byte itemCount, ushort nextFreePosition, ushort unalignedFreeBytes, byte highestSlotId)
        {
            PageId = pageId;
            Type = type;
            PrevPageId = prevPageId;
            NextPageId = nextPageId;
            UsedBytes = usedBytes;
            ItemCount = itemCount;
            NextFreePosition = nextFreePosition;
            UnalignedFreeBytes = unalignedFreeBytes;
            HighestSlotId = highestSlotId;
        }

        public PageHeader(uint pageId, PageType type)
        {
            PageId = pageId;
            Type = type;
            PrevPageId = uint.MaxValue;
            NextPageId = uint.MaxValue;
            UsedBytes = 0;
            ItemCount = 0;
            NextFreePosition = Constants.PAGE_HEADER_SIZE;
            UnalignedFreeBytes = 0;
            HighestSlotId = 0;
        }

        public static PageHeader FromBuffer(Span<byte> span)
        {
            return MemoryMarshal.Read<PageHeader>(span);
        }

        public static PageHeader FromBuffer(Span<byte> span, int offset)
        {
            //Guard.Argument(offset).Min(0);
            return FromBuffer(span.Slice(offset));
        }

        public void ToBuffer(Span<byte> span)
        {
            //Guard.Argument(span.Length,  nameof(span.Length)).Min(Constants.PAGE_COMMON_HEADER_SIZE);
            PageHeader a = this;
            MemoryMarshal.Write(span, ref a);
        }

        public void ToBuffer(Span<byte> span, int offset)
        {
            //Guard.Argument(offset).Min(0);
            ToBuffer(span.Slice(offset));
        }

        public override string ToString()
        {
            var tableData = new List<List<object>>()
            {
                new List<object>{nameof(ItemCount), ItemCount},
                new List<object>{nameof(UsedBytes), UsedBytes },
                new List<object>{nameof(UnalignedFreeBytes), UnalignedFreeBytes },
                new List<object>{nameof(NextFreePosition), NextFreePosition },
            };

            return ConsoleTableBuilder
                .From(tableData)
                .WithTitle($"{Enum.GetName(typeof(PageType), Type)}:{PageId}", ConsoleColor.Yellow, ConsoleColor.DarkGray)
                .WithColumn("Property", "Value").Export().ToString();
        }
    }
}
