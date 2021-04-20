// # SPDX-License-Identifier: MIT
// # Copyright 2021
// # Sebastian Fischer sebfischer@gmx.net

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
// ReSharper disable InconsistentNaming

namespace Datatent2.Core.Page.AllocationInformation
{
    [StructLayout(LayoutKind.Explicit, Size = Constants.ALLOCATION_INFORMATION_ENTRY_SIZE)]
    internal readonly struct AllocationInformationEntry
    {
        [FieldOffset(PAGE_ID)]
        public readonly uint PageId;

        [FieldOffset(PAGE_TYPE)]
        public readonly PageType PageType;

        [FieldOffset(PAGE_FILL_FACTOR)]
        public readonly PageFillFactor PageFillFactor;

        public AllocationInformationEntry(uint pageId, PageType pageType, PageFillFactor pageFillFactor)
        {
            PageId = pageId;
            PageType = pageType;
            PageFillFactor = pageFillFactor;
        }

        private const int PAGE_ID = 0; // uint 0-3
        private const int PAGE_TYPE = 4; // PageType byte 4
        private const int PAGE_FILL_FACTOR = 5; // PageFillFactor byte 5
    }
}
