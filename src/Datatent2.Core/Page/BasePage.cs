using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Datatent2.Core.Memory;
using Dawn;

namespace Datatent2.Core.Page
{
    internal abstract class BasePage
    {
        public uint Id => Header.PageId;
        public PageType Type => Header.Type;
        public byte ItemCount => Header.ItemCount;

        public ref PageHeader PageHeader => ref Header;

        public bool IsFull => FreeContinuousBytes < 8;
        public ushort UsedBytes => Header.UsedBytes;

        /// <summary>
        /// The free bytes in a continuous block
        /// </summary>
        public ushort FreeContinuousBytes => (ushort)(Constants.PAGE_SIZE - Constants.PAGE_HEADER_SIZE - Header.UsedBytes - Header.UnalignedFreeBytes - (Header.HighestEntryId * Constants.PAGE_DIRECTORY_ENTRY_SIZE)) ;

        public ushort MaxFreeUsableBytes => (ushort) (FreeContinuousBytes - Constants.PAGE_DIRECTORY_ENTRY_SIZE);

        protected BufferSegment Buffer;
        protected PageHeader Header;
        protected byte HighestDirectoryEntryId;

        protected BasePage(BufferSegment buffer)
        {
            Header = PageHeader.FromBuffer(buffer.Span);
            Buffer = buffer;
            HighestDirectoryEntryId = Header.HighestEntryId;
        }

        protected BasePage(BufferSegment buffer, uint id, PageType pageType)
        {
            Guard.Argument((int)buffer.Length).Min(Constants.PAGE_SIZE);
            Buffer = buffer;
            Header = new PageHeader(id, pageType);
            HighestDirectoryEntryId = Header.HighestEntryId;
        }

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

        public void Defrag()
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

                var entry = PageDirectoryEntry.FromBuffer(Buffer.Span,
                    PageDirectoryEntry.GetEntryPosition(region.Item1));
                var spanTarget = Buffer.Span.Slice(entry.EndPositionOfData());
                var spanSource = Buffer.Span.Slice(startOffset, endOffset - startOffset);
                // copy all bytes
                spanTarget.WriteBytes(0, spanSource);
                Buffer.Span.Slice(endOffset - region.Item3, region.Item3).Clear();
                for (int i = 0; i < between.Count; i++)
                {
                    var entryToChange = PageDirectoryEntry.FromBuffer(Buffer.Span, between[i].Index);
                    entryToChange = new PageDirectoryEntry((ushort) (entryToChange.DataOffset - region.Item3),
                        entryToChange.DataLength);
                    entryToChange.ToBuffer(Buffer.Span, PageDirectoryEntry.GetEntryPosition(between[i].Index));
                }


                var pageHeader = new PageHeader(Header.PageId, Header.Type, Header.PrevPageId, Header.NextPageId,
                    (ushort)(Header.UsedBytes),
                    (byte) (Header.ItemCount - 1),
                    Header.NextFreePosition,
                    (ushort) (regions.Sum(tuple => tuple.Item3) -  region.Item3),
                    HighestDirectoryEntryId);
                pageHeader.ToBuffer(Buffer.Span, 0);
                Header = pageHeader;

                regions = GetFreeRegions();
                freeRegions = regions.Count;
                maxLoops -= 1;
            }
        }

        public Span<byte> GetDataByIndex(byte directoryIndex)
        {
            Guard.Argument(directoryIndex).Max(Header.HighestEntryId);

            var entry = PageDirectoryEntry.FromBuffer(Buffer.Span, directoryIndex);

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

        private List<(byte Index, PageDirectoryEntry Entry)> GetAllDirectoryEntriesBetween(byte from, byte to)
        {
            List<(byte Index, PageDirectoryEntry Entry)> regionsList =
                new();

            PageDirectoryEntry entryFrom = PageDirectoryEntry.FromBuffer(Buffer.Span, PageDirectoryEntry.GetEntryPosition(from));
            PageDirectoryEntry entryTo = PageDirectoryEntry.FromBuffer(Buffer.Span, PageDirectoryEntry.GetEntryPosition(to));
            
            for (byte i = 1; i <= HighestDirectoryEntryId; i++)
            {
                var current = PageDirectoryEntry.FromBuffer(Buffer.Span, PageDirectoryEntry.GetEntryPosition(i));
                if (current.DataOffset >= entryFrom.DataOffset && current.DataOffset <= entryTo.DataOffset)
                    regionsList.Add((i, current));
            }
            
            return regionsList.OrderBy(tuple => tuple.Entry.DataOffset).ToList();
        }

        private List<(byte, byte, ushort)> GetFreeRegions()
        {
            List<(byte, byte, ushort)> regionsList = new();
            PageDirectoryEntry lastDirectoryEntry = new PageDirectoryEntry(0, 0);
            if (HighestDirectoryEntryId == 0)
            {
                regionsList.Add((Byte.MaxValue, Byte.MaxValue, FreeContinuousBytes));
                return regionsList;
            }

            byte lastId = 0;
            for (byte i = 1; i <= HighestDirectoryEntryId; i++)
            {
                var offset = PageDirectoryEntry.GetEntryPosition(i);
                if (PageDirectoryEntry.IsEmpty(Buffer.Span, offset))
                    continue;

                var current = PageDirectoryEntry.FromBuffer(Buffer.Span, PageDirectoryEntry.GetEntryPosition(i));

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

        public Span<byte> Insert(ushort length, out byte entryIndex)
        {
            Guard.Argument(IsInsertPossible(length)).True();

            // get the directory entry that stores the position of the data
            var found = GetNextAvailableDirectoryEntryIndex(out entryIndex);
            if (!found)
                throw new Exception("No available directory entry in page.");
            
            var entryPos = PageDirectoryEntry.GetEntryPosition(entryIndex);

            var nextFreePos = Header.NextFreePosition;
            var unalignedBytes = Header.UnalignedFreeBytes;
            PageDirectoryEntry entry = new PageDirectoryEntry(0, 0);

            // can be added at the end?
            if (FreeContinuousBytes >= length + Constants.PAGE_DIRECTORY_ENTRY_SIZE)
            {
                entry = new PageDirectoryEntry(nextFreePos, length);
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
                        var entryBefore = PageDirectoryEntry.FromBuffer(Buffer.Span, PageDirectoryEntry.GetEntryPosition(region.Item1));
                        entry = new PageDirectoryEntry(entryBefore.EndPositionOfData(), length);
                        unalignedBytes -= length;
                        break;
                    }
                }
            }

            Guard.Argument(entry.DataOffset == 0 && entry.DataLength == 0).False();

            // new highest index
            if (entryIndex > Header.HighestEntryId)
            {
                HighestDirectoryEntryId = entryIndex;
            }
            
            entry.ToBuffer(Buffer.Span, entryPos);

            var pageHeader = new PageHeader(Header.PageId, Header.Type, Header.PrevPageId, Header.NextPageId,
                (ushort)(Header.UsedBytes + length),
                (byte)(Header.ItemCount + 1), nextFreePos, unalignedBytes, HighestDirectoryEntryId);
            pageHeader.ToBuffer(Buffer.Span, 0);
            Header = pageHeader;
            return Buffer.Span.Slice(entry.DataOffset, entry.DataLength);
        }

        public bool Delete(byte entryIndex)
        {
            Guard.Argument(entryIndex).Min((byte)0);
            Guard.Argument(entryIndex).Max(Header.HighestEntryId,
                (b, _) => $"EntryIndex is out of range {b} max available is {1}");

            var entryPos = PageDirectoryEntry.GetEntryPosition(entryIndex);
            var pageDirectoryEntry = PageDirectoryEntry.FromBuffer(Buffer.Span, entryPos);

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
            PageDirectoryEntry.Clear(Buffer.Span, entryIndex);

            var pageHeader = new PageHeader(Header.PageId, Header.Type, Header.PrevPageId, Header.NextPageId,
                (ushort)(Header.UsedBytes - pageDirectoryEntry.DataLength),
                (byte)(Header.ItemCount - 1), nextFreePosition, unalignedBytes, HighestDirectoryEntryId);
            pageHeader.ToBuffer(Buffer.Span, 0);
            Header = pageHeader;

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
                var offset = PageDirectoryEntry.GetEntryPosition(i);
                if (!PageDirectoryEntry.IsEmpty(Buffer.Span, offset))
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
            if (Header.HighestEntryId == 0)
            {
                index = 1;
                return true;
            }

            if (Header.HighestEntryId < byte.MaxValue)
            {
                if (Header.HighestEntryId == byte.MaxValue)
                    throw new Exception("Full");

                index = (byte)(HighestDirectoryEntryId + 1);
                return true;
            }

            for (byte i = Header.HighestEntryId; i != 0; i--)
            {
                var offset = PageDirectoryEntry.GetEntryPosition(i);
                if (PageDirectoryEntry.IsEmpty(Buffer.Span, offset))
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

            List<(PageDirectoryEntry, byte)> pageDirectoryEntries = new();
            var lastEntry = new PageDirectoryEntry(0, 0);
            for (byte i = HighestDirectoryEntryId; i > 0; i--)
            {
                var pos = PageDirectoryEntry.GetEntryPosition(i);
                if (!PageDirectoryEntry.IsEmpty(Buffer.Span, pos))
                    pageDirectoryEntries.Add((PageDirectoryEntry.FromBuffer(Buffer.Span, pos), i));
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
            return Header + GenerateLayoutString();
        }

        #endregion
        
        public static uint PageOffset(uint pageId) => pageId * Constants.PAGE_SIZE;

        /// <summary>
        /// Is the BufferSegment an empty page 
        /// </summary>
        /// <param name="bufferSegment"></param>
        /// <returns></returns>
        public static bool IsEmpty(BufferSegment bufferSegment)
        {
            PageHeader pageHeader = PageHeader.FromBuffer(bufferSegment.Span);

            return pageHeader.PageId == 0;
        }
    }
}
