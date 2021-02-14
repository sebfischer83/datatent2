using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using ObjectLayoutInspector;

namespace Datatent2.Core.Page
{
    [StructLayout(LayoutKind.Explicit, Size = Constants.PAGE_HEADER_SIZE)]
    internal struct PageHeader
    {
        /// <summary>
        /// The id of the page, a continuous increasing number.
        /// Starts with 0 for the header of the database. 
        /// </summary>
        [FieldOffset(PAGE_ID)]
        public uint PageId;

        /// <summary>
        /// The type of the page.
        /// <see cref="PageType"/>
        /// </summary>
        [FieldOffset(PAGE_TYPE)]
        public PageType Type;

        /// <summary>
        /// Link to the previous page of the same type.
        /// If none then uint.MaxValue.
        /// </summary>
        [FieldOffset(PAGE_PREV_ID)]
        public uint PrevPageId;

        /// <summary>
        /// Link to the next page of the same type.
        /// If none then uint.MaxValue.
        /// </summary>
        [FieldOffset(PAGE_NEXT_ID)]
        public uint NextPageId;

        private const int PAGE_ID = 0; // 0-3 uint
        private const int PAGE_TYPE = 4; // 4 byte (enum PageType)
        private const int PAGE_PREV_ID = 5; // 5-8 uint 
        private const int PAGE_NEXT_ID = 9; // 9-12 uint
        private const int PAGE_USED_BYTES = 13; // 13-14 ushort
        private const int PAGE_NUMBER_OF_ITEMS = 13; // 13-14 ushort
        private const int PAGE_NEXT_FREE_POSITION = 15; // 15-16 ushort
        private const int PAGE_NEXT_FREE_INDEX = 17; // 17-18 ushort
        private const int PAGE_UNALIGNED_FREE_BYTES = 19; // 19-20 ushort
    }

    internal enum PageType : byte
    {
        Header = 1,
        Data = 2,
        Index = 3
    }
}
