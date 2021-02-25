using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Datatent2.Core.Block;
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

        public bool IsFull => GetFreeRegions().Count == 0;
        public ushort UsedBytes => Header.UsedBytes;

        /// <summary>
        /// The free bytes in a continuous block
        /// </summary>
        public ushort FreeContinuousBytes => (ushort)(Constants.PAGE_SIZE - Constants.PAGE_HEADER_SIZE - Header.UsedBytes - Header.UnalignedFreeBytes - (Header.HighestEntryId * Constants.PAGE_DIRECTORY_ENTRY_SIZE));

        protected Memory.BufferSegment Buffer;
        protected PageHeader Header;
        protected byte HighestDirectoryEntryId;

        protected BasePage(Memory.BufferSegment buffer)
        {
            Header = PageHeader.FromBuffer(buffer.Span);
            Buffer = buffer;
            HighestDirectoryEntryId = Header.HighestEntryId;
        }

        protected BasePage(Memory.BufferSegment buffer, uint id, PageType pageType)
        {
            Guard.Argument((int)buffer.Length).Min(Constants.PAGE_SIZE);
            Buffer = buffer;
            Header = new PageHeader(id, pageType);
            HighestDirectoryEntryId = Header.HighestEntryId;
        }

        public bool IsInsertPossible(ushort length)
        {
            // is enough space free at the end?
            if (FreeContinuousBytes > length + Constants.PAGE_DIRECTORY_ENTRY_SIZE)
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

            var entry = GetNextDirectoryEntry(length, out entryIndex);
            var entryPos = PageDirectoryEntry.GetEntryPosition(entryIndex);
            var nextFreePos = Header.NextFreePosition;
            var unalignedBytes = Header.UnalignedFreeBytes;

            // adding at the end
            if (entryIndex > Header.HighestEntryId)
            {
                HighestDirectoryEntryId = entryIndex;
                nextFreePos += length;
            }
            else
            {
                // adding between already existing blocks
                unalignedBytes -= length;
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
            Guard.Argument(entryIndex).Max(Header.HighestEntryId, (b, b1) => $"EntryIndex is out of range {b} max available is {1}");

            var entryPos = PageDirectoryEntry.GetEntryPosition(entryIndex);
            var pageDirectoryEntry = PageDirectoryEntry.FromBuffer(Buffer.Span, entryPos);

            // did we delete the last entry in the page?
            var lastEntry = this.Header.NextFreePosition == pageDirectoryEntry.EndPositionOfData();
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
                (byte)(Header.ItemCount + 1), nextFreePosition, unalignedBytes, HighestDirectoryEntryId);
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
        /// <param name="length"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        protected PageDirectoryEntry GetNextDirectoryEntry(ushort length, out byte index)
        {
            index = byte.MaxValue;
            if (Header.HighestEntryId == 0)
            {
                index = 1;
                return new PageDirectoryEntry(Header.NextFreePosition, length);
            }

            if (length <= Constants.PAGE_SIZE - Header.NextFreePosition)
            {
                if (Header.HighestEntryId == byte.MaxValue)
                    throw new Exception("Full");

                index = (byte)(HighestDirectoryEntryId + 1);
                return new PageDirectoryEntry(Header.NextFreePosition, length);
            }

            var res = FindFreeSpaceBetween(length);
            index = (byte)(res.Index1 + 1);
            var pos = PageDirectoryEntry.GetEntryPosition(index);
            return new PageDirectoryEntry(pos, length);
        }

        protected virtual string GenerateLayoutString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine();
            stringBuilder.AppendLine($"------------- Content");
            stringBuilder.AppendLine($"|HHHHHHHHHHH| Header {Constants.PAGE_HEADER_SIZE} bytes");

            List<(PageDirectoryEntry, byte)> pageDirectoryEntries = new List<(PageDirectoryEntry, byte)>();
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
            stringBuilder.AppendLine($"-------------");
            return stringBuilder.ToString();
        }

        public override string ToString()
        {
            return Header + GenerateLayoutString();
        }

        public static uint PageOffset(uint pageId) => pageId * Constants.PAGE_SIZE;

        /// <summary>
        /// Is the BufferSegment an empty page 
        /// </summary>
        /// <param name="bufferSegment"></param>
        /// <returns></returns>
        public static bool IsEmpty(BufferSegment bufferSegment)
        {
            Page.PageHeader pageHeader = Page.PageHeader.FromBuffer(bufferSegment.Span);

            return pageHeader.PageId == 0;
        }
    }
}
