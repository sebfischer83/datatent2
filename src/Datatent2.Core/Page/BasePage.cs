// # SPDX-License-Identifier: MIT
// # Copyright 2021
// # Sebastian Fischer sebfischer@gmx.net

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Datatent2.Contracts;
using Datatent2.Core.Memory;
using Datatent2.Core.Page.Data;
using Datatent2.Core.Page.Index;
using Datatent2.Core.Page.Table;
using Datatent2.Core.Services.Transactions;
using Dawn;

namespace Datatent2.Core.Page
{
    internal abstract class BasePage : IPage
    {
        public uint Id => Header.PageId;
        public PageType Type => Header.Type;
        public byte ItemCount => Header.ItemCount;

        public ref PageHeader PageHeader => ref Header;

        public bool IsDirty { get; set; }

        public Transaction? Transaction { get; set; }

        public virtual bool IsFull => FreeContinuousBytes < (Constants.PAGE_DIRECTORY_ENTRY_SIZE + 1) || MaxFreeUsableBytes <= 8 || HighestDirectoryEntryId == byte.MaxValue;
        public ushort UsedBytes => Header.UsedBytes;

        public virtual PageFillFactor FillFactor
        {
            get
            {
                if (Header.UsedBytes == 0)
                    return PageFillFactor.Zero;
                return ((100 * Header.UsedBytes) / (decimal)(Constants.PAGE_SIZE - Constants.PAGE_HEADER_SIZE)) switch
                {
                    0 => PageFillFactor.Zero,
                    < 50 => PageFillFactor.ZeroToFifty,
                    < 70 => PageFillFactor.FiftyToSeventy,
                    < 95 => PageFillFactor.SeventyToNinetyFive,
                    < 99 => PageFillFactor.NinetyFiveToNinetyNine,
                    > 99 => PageFillFactor.Full,
                    _ => PageFillFactor.Zero
                };
            }
        }

        /// <summary>
        /// The free bytes in a continuous block
        /// </summary>
        public virtual ushort FreeContinuousBytes => (ushort)(Constants.PAGE_SIZE - Constants.PAGE_HEADER_SIZE - Header.UsedBytes - Header.UnalignedFreeBytes - (Header.HighestSlotId * Constants.PAGE_DIRECTORY_ENTRY_SIZE));

        public virtual ushort MaxFreeUsableBytes
        {
            get
            {
                if (FreeContinuousBytes < Constants.PAGE_DIRECTORY_ENTRY_SIZE)
                    return FreeContinuousBytes;
                return (ushort)(FreeContinuousBytes - Constants.PAGE_DIRECTORY_ENTRY_SIZE);
            }
        }

        protected IBufferSegment Buffer;
        protected PageHeader Header;
        protected byte HighestDirectoryEntryId;

        public IBufferSegment PageBuffer => Buffer;

        protected BasePage(IBufferSegment buffer)
        {
            Header = PageHeader.FromBuffer(buffer.Span);
            Buffer = buffer;
            HighestDirectoryEntryId = Header.HighestSlotId;
        }

        protected BasePage(IBufferSegment buffer, uint id, PageType pageType)
        {
            Guard.Argument((int)buffer.Length).Min(Constants.PAGE_SIZE);
            Buffer = buffer;
            Header = new PageHeader(id, pageType);
            HighestDirectoryEntryId = Header.HighestSlotId;
            IsDirty = true;
        }

        public void SetPreviousPage(uint pageId)
        {
            SetLinkedPages(pageId, Header.NextPageId);
        }

        public void SetNextPage(uint pageId)
        {
            SetLinkedPages(Header.PrevPageId, pageId);
        }

        private void SetLinkedPages(uint prev, uint next)
        {
            var pageHeader = new PageHeader(Header.PageId, Header.Type, prev, next,
                (ushort)(Header.UsedBytes),
                (byte)(Header.ItemCount),
                Header.NextFreePosition,
                Header.UnalignedFreeBytes,
                HighestDirectoryEntryId);
            pageHeader.ToBuffer(Buffer.Span, 0);
            Header = pageHeader;
            IsDirty = true;
        }

        //public virtual void WriteUnslotted(Span<byte> data)
        //{
        //    if (data.Length > MaxFreeUsableBytes)
        //        throw new ArgumentOutOfRangeException(nameof(data));

        //    var span = Buffer.Span;
        //    span.WriteBytes(Constants.PAGE_HEADER_SIZE, data);

        //}

        public bool IsInsertPossible(ushort length)
        {
            if (PageHeader.ItemCount == byte.MaxValue)
                return false;

            // is enough space free at the end?
            if (FreeContinuousBytes >= length + Constants.PAGE_DIRECTORY_ENTRY_SIZE)
                return true;

            // unaligned space is not big enough
            if (Header.UnalignedFreeBytes < length)
                return false;

            // check if unaligned space has enough free contiguous space
            var res = FindFreeSpaceBetween(length);
            return res.Index1 != byte.MaxValue || res.Index2 != byte.MaxValue;
        }

        internal virtual void SaveHeader()
        {
            Header.ToBuffer(PageBuffer.Span);
        }

        public void ConvertToFreePage()
        {
            Buffer.Span.Clear();
            var pageHeader = new PageHeader(Header.PageId, PageType.Free);
            pageHeader.ToBuffer(Buffer.Span);
            IsDirty = true;
        }

        public virtual void Defrag()
        {
            // nothing to do
            if (Header.UnalignedFreeBytes == 0)
                return;

            var regions = GetFreeRegions();
            var freeRegions = regions.Count;
            // maximum number of loops to is the initial number of regions with free space
            int maxLoops = regions.Count;

            while (freeRegions > 0 && maxLoops > 0)
            {
                // take the first free region
                var region = regions[0];

                // last free region in the file
                byte nextFreeEntry = HighestDirectoryEntryId;
                if (regions.Count > 1)
                {
                    // next free entry starts here, so all between these indexes need to be moved
                    var nextRegion = regions[1];
                    nextFreeEntry = nextRegion.Item1;
                }

                var between = GetAllDirectoryEntriesBetween(region.Item2, nextFreeEntry);
                var startOffset = between[0].Entry.DataOffset;
                var endOffset = between[^1].Entry.EndPositionOfData();

                var entry = SlotEntry.FromBuffer(Buffer.Span,
                    SlotEntry.GetEntryPosition(region.Item1));
                var spanTarget = Buffer.Span.Slice(entry.EndPositionOfData());
                var spanSource = Buffer.Span.Slice(startOffset, endOffset - startOffset);
                // copy all bytes
                spanTarget.WriteBytes(0, spanSource);
                Buffer.Span.Slice(endOffset - region.Item3, region.Item3).Clear();
                for (int i = 0; i < between.Count; i++)
                {
                    var entryToChange = SlotEntry.FromBuffer(Buffer.Span, between[i].Index);
                    entryToChange = new SlotEntry((ushort)(entryToChange.DataOffset - region.Item3),
                        entryToChange.DataLength);
                    entryToChange.ToBuffer(Buffer.Span, SlotEntry.GetEntryPosition(between[i].Index));
                }


                var pageHeader = new PageHeader(Header.PageId, Header.Type, Header.PrevPageId, Header.NextPageId,
                    (ushort)(Header.UsedBytes),
                    (byte)(Header.ItemCount - 1),
                    Header.NextFreePosition,
                    (ushort)(regions.Sum(tuple => tuple.Item3) - region.Item3),
                    HighestDirectoryEntryId);
                pageHeader.ToBuffer(Buffer.Span, 0);
                Header = pageHeader;

                regions = GetFreeRegions();
                freeRegions = regions.Count;
                maxLoops -= 1;
            }

            IsDirty = true;
        }

        public Span<byte> GetDataByIndex(byte directoryIndex)
        {
            Guard.Argument(directoryIndex).Max(Header.HighestSlotId);

            var entry = SlotEntry.FromBuffer(Buffer.Span, directoryIndex);

            return Buffer.Span.Slice(entry.DataOffset, entry.DataLength);
        }

        public ushort GetMaxContiguounesFreeSpace()
        {
            // at the end is more empty as between the data blocks
            if (FreeContinuousBytes > Header.UnalignedFreeBytes)
                return FreeContinuousBytes;

            var listFreeRegions = GetFreeRegions();

            if (listFreeRegions.Count == 0)
                return 0;
            return listFreeRegions.Max(tuple => tuple.Item3);
        }

        private List<(byte Index, SlotEntry Entry)> GetAllDirectoryEntriesBetween(byte from, byte to)
        {
            List<(byte Index, SlotEntry Entry)> regionsList =
                new();

            SlotEntry entryFrom = SlotEntry.FromBuffer(Buffer.Span, SlotEntry.GetEntryPosition(from));
            SlotEntry entryTo = SlotEntry.FromBuffer(Buffer.Span, SlotEntry.GetEntryPosition(to));

            for (byte i = 1; i <= HighestDirectoryEntryId; i++)
            {
                var current = SlotEntry.FromBuffer(Buffer.Span, SlotEntry.GetEntryPosition(i));
                if (current.DataOffset >= entryFrom.DataOffset && current.DataOffset <= entryTo.DataOffset)
                    regionsList.Add((i, current));
            }

            return regionsList.OrderBy(tuple => tuple.Entry.DataOffset).ToList();
        }

        private List<(byte, byte, ushort)> GetFreeRegions()
        {
            List<(byte, byte, ushort)> regionsList = new();
            SlotEntry lastDirectoryEntry = new SlotEntry(0, 0);
            if (HighestDirectoryEntryId == 0)
            {
                regionsList.Add((Byte.MaxValue, Byte.MaxValue, FreeContinuousBytes));
                return regionsList;
            }

            byte lastId = 0;
            for (byte i = 1; i <= HighestDirectoryEntryId; i++)
            {
                var offset = SlotEntry.GetEntryPosition(i);
                if (SlotEntry.IsEmpty(Buffer.Span, offset))
                    continue;

                var current = SlotEntry.FromBuffer(Buffer.Span, SlotEntry.GetEntryPosition(i));

                if (lastDirectoryEntry.DataLength == 0 && lastDirectoryEntry.DataOffset == 0)
                {
                    if (current.DataOffset - Constants.PAGE_HEADER_SIZE > 0)
                    {
                        regionsList.Add(((byte, byte, ushort))(0, i, Constants.PAGE_SIZE - Constants.PAGE_HEADER_SIZE - current.EndPositionOfData()));
                    }

                    lastDirectoryEntry = current;
                    lastId = i;
                    continue;
                }

                if (current.DataOffset - lastDirectoryEntry.EndPositionOfData() > 0)
                {
                    regionsList.Add(((byte, byte, ushort))(lastId, i,
                        current.DataOffset - lastDirectoryEntry.EndPositionOfData()));
                    lastDirectoryEntry = current;
                    lastId = i;
                }
                else
                {
                    lastDirectoryEntry = current;
                    lastId = i;
                }
            }

            return regionsList;
        }

        internal (byte Index1, byte Index2) FindFreeSpaceBetween(ushort length)
        {
            var regions = GetFreeRegions();
            if (regions.Count > 0 && regions.Any(tuple => tuple.Item3 >= length))
            {
                var region = regions.FirstOrDefault(tuple => tuple.Item3 >= length);
                return (region.Item1, region.Item2);
            }

            return (byte.MaxValue, byte.MaxValue);
        }

        /// <summary>
        /// Retrieves a Span for insertion of data if a specified length.
        /// </summary>
        /// <param name="length">The length.</param>
        /// <param name="entryIndex">The entry index.</param>
        /// <returns>A Span.</returns>
        public Span<byte> Insert(ushort length, out byte entryIndex)
        {
            Guard.Argument(IsInsertPossible(length)).True();

            // get the directory entry that stores the position of the data
            var found = GetNextAvailableDirectoryEntryIndex(out entryIndex);
            if (!found)
                throw new Exception("No available directory entry in page.");

            var entryPos = SlotEntry.GetEntryPosition(entryIndex);

            var nextFreePos = Header.NextFreePosition;
            var unalignedBytes = Header.UnalignedFreeBytes;
            SlotEntry entry = new SlotEntry(0, 0);

            // can be added at the end?
            if (FreeContinuousBytes >= length + Constants.PAGE_DIRECTORY_ENTRY_SIZE)
            {
                entry = new SlotEntry(nextFreePos, length);
                nextFreePos += length;
            }
            else
            {
                // fit to an empty area between existing blocks
                var regions = GetFreeRegions();
                for (int i = 0; i < regions.Count; i++)
                {
                    var region = regions[i];
                    if (region.Item3 >= length)
                    {
                        var entryBefore = SlotEntry.FromBuffer(Buffer.Span, SlotEntry.GetEntryPosition(region.Item1));
                        entry = new SlotEntry(entryBefore.EndPositionOfData(), length);
                        unalignedBytes -= length;
                        break;
                    }
                }
            }

            Guard.Argument(entry.DataOffset == 0 && entry.DataLength == 0).False();

            // new highest index
            if (entryIndex > Header.HighestSlotId)
            {
                HighestDirectoryEntryId = entryIndex;
            }

            entry.ToBuffer(Buffer.Span, entryPos);

            var pageHeader = new PageHeader(Header.PageId, Header.Type, Header.PrevPageId, Header.NextPageId,
                (ushort)(Header.UsedBytes + length),
                (byte)(Header.ItemCount + 1), nextFreePos, unalignedBytes, HighestDirectoryEntryId);
            pageHeader.ToBuffer(Buffer.Span, 0);
            Header = pageHeader;
            IsDirty = true;
            return Buffer.Span.Slice(entry.DataOffset, entry.DataLength);
        }

        public bool Delete(byte entryIndex)
        {
            Guard.Argument(entryIndex).Min((byte)0);
            Guard.Argument(entryIndex).Max(Header.HighestSlotId,
                (b, _) => $"EntryIndex is out of range {b} max available is {1}");

            var entryPos = SlotEntry.GetEntryPosition(entryIndex);
            var pageDirectoryEntry = SlotEntry.FromBuffer(Buffer.Span, entryPos);

            // did we delete the last entry in the page?
            var lastEntry = Header.NextFreePosition == pageDirectoryEntry.EndPositionOfData();
            ushort nextFreePosition = Header.NextFreePosition;
            ushort unalignedBytes = Header.UnalignedFreeBytes;
            if (lastEntry)
            {
                nextFreePosition = pageDirectoryEntry.DataOffset;
            }
            else
            {
                unalignedBytes += pageDirectoryEntry.DataLength;
            }

            if (entryIndex == HighestDirectoryEntryId)
            {
                HighestDirectoryEntryId = GetNextHighestDirectoryEntry(entryIndex);
            }

            Buffer.Span.Slice(pageDirectoryEntry.DataOffset, pageDirectoryEntry.DataLength).Clear();
            SlotEntry.Clear(Buffer.Span, entryIndex);

            var pageHeader = new PageHeader(Header.PageId, Header.Type, Header.PrevPageId, Header.NextPageId,
                (ushort)(Header.UsedBytes - pageDirectoryEntry.DataLength),
                (byte)(Header.ItemCount - 1), nextFreePosition, unalignedBytes, HighestDirectoryEntryId);
            pageHeader.ToBuffer(Buffer.Span, 0);
            Header = pageHeader;
            IsDirty = true;

            return true;
        }

        /// <summary>
        /// Search the next lower not empty directory index.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        protected byte GetNextHighestDirectoryEntry(byte index)
        {
            if (index == 1)
            {
                return 0;
            }

            byte startIndex = (byte)(index - 1);

            for (byte i = startIndex; i != 0; i--)
            {
                var offset = SlotEntry.GetEntryPosition(i);
                if (!SlotEntry.IsEmpty(Buffer.Span, offset))
                    return i;
            }

            // found only empty entries, then we can start from the beginning
            return 0;
        }

        /// <summary>
        /// Search the next free directory index.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        protected bool GetNextAvailableDirectoryEntryIndex(out byte index)
        {
            index = byte.MaxValue;
            if (Header.HighestSlotId == 0)
            {
                index = 1;
                return true;
            }

            if (Header.HighestSlotId < byte.MaxValue)
            {
                if (Header.HighestSlotId == byte.MaxValue)
                    throw new Exception("Full");

                index = (byte)(HighestDirectoryEntryId + 1);
                return true;
            }

            for (byte i = Header.HighestSlotId; i != 0; i--)
            {
                var offset = SlotEntry.GetEntryPosition(i);
                if (SlotEntry.IsEmpty(Buffer.Span, offset))
                {
                    index = i;
                    return true;
                }
            }

            index = Byte.MaxValue;
            return false;
        }

        #region ToString

        protected virtual string GenerateLayoutString()
        {
            StringBuilder stringBuilder = new();
            stringBuilder.AppendLine();
            stringBuilder.AppendLine("------------- Content");
            stringBuilder.AppendLine($"|HHHHHHHHHHH| Header {Constants.PAGE_HEADER_SIZE} bytes");

            List<(SlotEntry, byte)> pageDirectoryEntries = new();
            var lastEntry = new SlotEntry(0, 0);
            for (byte i = HighestDirectoryEntryId; i > 0; i--)
            {
                var pos = SlotEntry.GetEntryPosition(i);
                if (!SlotEntry.IsEmpty(Buffer.Span, pos))
                    pageDirectoryEntries.Add((SlotEntry.FromBuffer(Buffer.Span, pos), i));
            }

            var lastIndex = 0;
            pageDirectoryEntries = pageDirectoryEntries.OrderBy(entry => entry.Item1.DataOffset).ToList();
            foreach (var (pageDirectoryEntry, pos) in pageDirectoryEntries)
            {
                if (lastEntry.DataOffset == 0 && pageDirectoryEntry.DataOffset != Constants.PAGE_HEADER_SIZE)
                {
                    stringBuilder.AppendLine($"|FFFFFFFFFFF| FREE {pageDirectoryEntry.DataOffset + 1 - Constants.PAGE_HEADER_SIZE} bytes");
                    lastEntry = pageDirectoryEntry;
                    lastIndex = pos;
                    continue;
                }
                if (lastEntry.DataOffset == 0)
                {
                    lastEntry = pageDirectoryEntry;
                    lastIndex = pos;
                    continue;
                }
                stringBuilder.AppendLine($"|DDDDDDDDDDD| DATA entry {lastIndex:000}|{lastEntry.DataOffset}:{lastEntry.DataLength} bytes");

                if (lastEntry.EndPositionOfData() != pageDirectoryEntry.DataOffset)
                {
                    stringBuilder.AppendLine($"|FFFFFFFFFFF| FREE {pageDirectoryEntry.DataOffset - lastEntry.DataOffset} bytes");
                }
                lastIndex = pos;
                lastEntry = pageDirectoryEntry;
            }
            if (lastEntry.DataOffset != 0)
                stringBuilder.AppendLine($"|DDDDDDDDDDD| DATA entry {lastIndex:000}|{lastEntry.DataOffset}:{lastEntry.DataLength} bytes");
            if (FreeContinuousBytes > 0)
            {
                stringBuilder.AppendLine($"|FFFFFFFFFFF| FREE {FreeContinuousBytes} bytes");
            }
            if (Header.ItemCount > 0)
                stringBuilder.AppendLine($"|DDDDDDDDDDD| DIRECTORY {Constants.PAGE_DIRECTORY_ENTRY_SIZE * Header.ItemCount} bytes");
            stringBuilder.AppendLine("-------------");
            return stringBuilder.ToString();
        }

        public override string ToString()
        {
            return Environment.NewLine + Header + GenerateLayoutString();
        }

        #endregion

        public static uint PageOffset(uint pageId) => (pageId) * Constants.PAGE_SIZE;

        /// <summary>
        /// Is the BufferSegment an empty page 
        /// </summary>
        /// <param name="bufferSegment"></param>
        /// <returns></returns>
        public static bool IsEmpty(IBufferSegment bufferSegment)
        {
            PageHeader pageHeader = PageHeader.FromBuffer(bufferSegment.Span);

            return pageHeader.PageId == 0;
        }

        public static T? Create<T>(IBufferSegment bufferSegment) where T : BasePage
        {
            if (typeof(T) == typeof(DataPage))
            {
                return (T)(object)new DataPage(bufferSegment);
            }
            if (typeof(T) == typeof(TablePage))
            {
                return (T)(object)new TablePage(bufferSegment);
            }
            if (typeof(T) == typeof(IndexPage))
            {
                return (T)(object)new IndexPage(bufferSegment);
            }
            return null;
        }

        public override bool Equals(object? obj)
        {
            var page = obj as BasePage;
            if (page == null)
                return false;

            return Equals(page);
        }

        public bool Equals(BasePage? other)
        {
            return other != null &&
                   Id == other.Id;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Id);
        }

        public static bool operator ==(BasePage? left, BasePage? right)
        {
            return EqualityComparer<BasePage>.Default.Equals(left, right);
        }

        public static bool operator !=(BasePage? left, BasePage? right)
        {
            return !(left == right);
        }

        public void Dispose()
        {
            BufferPoolFactory.Get().Return(Buffer);
        }
    }
}
