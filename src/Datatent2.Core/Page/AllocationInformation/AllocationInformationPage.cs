// # SPDX-License-Identifier: MIT
// # Copyright 2021
// # Sebastian Fischer sebfischer@gmx.net

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConsoleTableExt;
using Datatent2.Contracts;
using Datatent2.Core.Memory;
using Microsoft.Extensions.Caching.Memory;

namespace Datatent2.Core.Page.AllocationInformation
{
    internal sealed class AllocationInformationPage : BasePage
    {
        // all positions in AIM are relative to the outer GAM page, but the AIM is the first page in itself

        public const int ENTRIES_PER_PAGE = (Constants.PAGE_SIZE -
                                              Constants.PAGE_HEADER_SIZE) / Constants.ALLOCATION_INFORMATION_ENTRY_SIZE;

        public static readonly int AIM_PER_GAM = GlobalAllocationMap.GlobalAllocationMapPage.PAGES_PER_GAM / ENTRIES_PER_PAGE;

        private static readonly HashSet<uint> PositionsInGamLookup;
        private static readonly uint[] PositionsInGam;
        private static readonly uint LastPosInGam;
        private readonly AllocationInformationPageHeader _allocationInformationPageHeader;
        private static readonly IMemoryCache MemoryCache = new MemoryCache(new MemoryCacheOptions());

        static AllocationInformationPage()
        {
            uint[] pos = new uint[AIM_PER_GAM];
            pos[0] = 0;
            for (int i = 1; i < AIM_PER_GAM; i++)
            {
                pos[i] = (uint)((uint)(i * (uint)ENTRIES_PER_PAGE));
            }

            LastPosInGam = pos[^1];
            PositionsInGamLookup = pos.ToHashSet();
            PositionsInGam = pos;
        }

        public override bool IsFull
        {
            get
            {
                var span = MemoryMarshal.Cast<byte, AllocationInformationEntry>(Buffer.Span);
                var lastItem = span[^1];
                return lastItem.PageId != 0 && lastItem.PageType != PageType.Undefined;
            }
        }

        public AllocationInformationPage(IBufferSegment buffer) : base(buffer)
        {
        }

        public AllocationInformationPage(IBufferSegment buffer, uint id) : base(buffer, id, PageType.AllocationInformation)
        {
            // GAM number
            var gam = (uint)Math.DivRem(id, GlobalAllocationMap.GlobalAllocationMapPage.PAGES_PER_GAM, out var posInGam) + 1;

            // AIM number
            // remove header page, all indexes here are relative to the GAM page with index 1
            var aim = Array.IndexOf(PositionsInGam, (int)posInGam);

            AddAllocationInformation(this);
        }

        public void AddAllocationInformation(IPage page)
        {
            var id = page.Id - Id;
            var span = MemoryMarshal.Cast<byte, AllocationInformationEntry>(Buffer.Span.Slice(Constants.PAGE_HEADER_SIZE));
            var info = new AllocationInformationEntry(page.Id, page.Type, page.FillFactor);
            span[(int)id] = info;

            Header = new PageHeader(Header.PageId,
                Header.Type,
                Header.PrevPageId,
                Header.NextPageId,
                (ushort)(Header.UsedBytes + Constants.ALLOCATION_INFORMATION_ENTRY_SIZE),
                0,
                Header.NextFreePosition,
                Header.UnalignedFreeBytes, Header.HighestSlotId);
            Header.ToBuffer(Buffer.Span);

            IsDirty = true;
        }

        public void UpdateAllocationInformation(BasePage page)
        {
            var id = page.Id - Id;
            var span = MemoryMarshal.Cast<byte, AllocationInformationEntry>(Buffer.Span.Slice(Constants.PAGE_HEADER_SIZE));
            var old = span[(int)id];
            span[(int)id] = new AllocationInformationEntry(page.Id, old.PageType, page.FillFactor);

            IsDirty = true;
        }

        public AllocationInformationEntry GetAllocationInformationEntry(uint pageId)
        {
            var id = pageId - Id;
            var span = MemoryMarshal.Cast<byte, AllocationInformationEntry>(Buffer.Span.Slice(Constants.PAGE_HEADER_SIZE));
            var val = span[(int)id];

            return val;
        }

        public long FindPageWithFreeSpace(PageType pageType,
            PageFillFactor maxPageFillFactor = PageFillFactor.SeventyToNinetyFive)
        {
            var span = MemoryMarshal.Cast<byte, AllocationInformationEntry>(Buffer.Span.Slice(Constants.PAGE_HEADER_SIZE));
            var elements = Header.UsedBytes / Constants.ALLOCATION_INFORMATION_ENTRY_SIZE;
            for (int i = 0; i < elements; i++)
            {
                ref AllocationInformationEntry entry = ref span[i];
                if (entry.PageType == pageType && entry.PageFillFactor <= maxPageFillFactor)
                {
                    return entry.PageId;
                }
            }

            return -1;
        }

        public uint[] FindPagesOfType(PageType pageType)
        {
            List<uint> list = new List<uint>();
            var span = MemoryMarshal.Cast<byte, AllocationInformationEntry>(Buffer.Span.Slice(Constants.PAGE_HEADER_SIZE));
            var elements = Header.UsedBytes / Constants.ALLOCATION_INFORMATION_ENTRY_SIZE;
            for (int i = 0; i < elements; i++)
            {
                ref AllocationInformationEntry entry = ref span[i];
                if (entry.PageType == pageType)
                {
                    list.Add(entry.PageId);
                }
            }

            return list.ToArray();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static uint[] GetAllAllocationInformationPageIdsForGam(uint gamPageId)
        {
            if (MemoryCache.TryGetValue(gamPageId, out uint[] cachedData))
            {
                return cachedData;
            }

            uint[] array = new uint[AIM_PER_GAM];
            for (int i = 0; i < PositionsInGam.Length; i++)
            {
                var pos = PositionsInGam[i];
                pos += gamPageId + 1;
                array[i] = pos;
            }

            var entry = MemoryCache.CreateEntry(gamPageId);
            entry.Value = array;
            return array;
        }

        public static (uint Id, bool NewGAM) GetNextAllocationInformationPageId(uint currentAIMPageId)
        {
            // only need the remainder
            var gam = (uint)Math.DivRem(currentAIMPageId, GlobalAllocationMap.GlobalAllocationMapPage.PAGES_PER_GAM, out var id);

            // when we are not in the first GAM so subtract the gam pages from the id
            id = currentAIMPageId - GlobalAllocationMap.GlobalAllocationMapPage.PAGES_PER_GAM * gam;

            // we need always to subtract 2 because there is always a header page and minimum a GAM behind us
            id -= 2 + (gam * 1);

            var pos = Array.IndexOf(PositionsInGam, (uint)id);
            if (pos == AIM_PER_GAM - 1)
                return ((uint)currentAIMPageId + ENTRIES_PER_PAGE + 1, true);

            return (currentAIMPageId + ENTRIES_PER_PAGE, false);
        }

        public override string ToString()
        {
            var tableData = new List<List<object>>()
            {
                new List<object>{nameof(ItemCount), Header.UsedBytes / Constants.ALLOCATION_INFORMATION_ENTRY_SIZE},
                new List<object>{nameof(UsedBytes), UsedBytes },
            };

            return ConsoleTableBuilder
                .From(tableData)
                .WithTitle($"{Enum.GetName(typeof(PageType), Type)}:{Id}", ConsoleColor.Yellow, ConsoleColor.DarkGray)
                .WithColumn("Property", "Value").Export().ToString();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static bool IsAllocationInformationPage(uint pageId)
        {
            // only need the remainder
            var gam = (uint)Math.DivRem(pageId, GlobalAllocationMap.GlobalAllocationMapPage.PAGES_PER_GAM, out var id);

            // when we are not in the first GAM so subtract the gam pages from the id
            id = pageId - GlobalAllocationMap.GlobalAllocationMapPage.PAGES_PER_GAM * gam;

            // we need always to subtract 2 because there is always a header page and minimum a GAM behind us
            id -= 2 + (gam * 1);

            return PositionsInGamLookup.Contains((uint)id);
        }
    }
}
