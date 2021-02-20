using System;
using System.Collections.Generic;
using System.Linq;
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

        public bool IsFull => Header.ItemCount == byte.MaxValue || FreeBytes == 0;
        public ushort UsedBytes => Header.UsedBytes;

        /// <summary>
        /// The free bytes in a continuous block
        /// </summary>
        public ushort FreeBytes => (ushort)(Constants.MAX_USABLE_BYTES_IN_PAGE - Header.UsedBytes - Header.UnalignedFreeBytes - (Header.HighestEntryId * Constants.PAGE_DIRECTORY_ENTRY_SIZE));

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

        public bool CheckIsInsertPossible(ushort length)
        {
            // is enough space free at the end?
            if (FreeBytes >= length)
                return true;

            // unaligned space is not big enough
            if (Header.UnalignedFreeBytes < length)
                return false;

            // check if unaligned space has enough free contiguous space
            var res = FindFreeSpaceBetween(length);
            return res.Index1 != byte.MaxValue || res.Index2 != byte.MaxValue;
        }

        internal (byte Index1, byte Index2) FindFreeSpaceBetween(ushort length)
        {
            PageDirectoryEntry lastDirectoryEntry = new PageDirectoryEntry(0, 0);
            byte lastId = 0;
            for (byte i = 1; i <= HighestDirectoryEntryId; i++)
            {
                var offset = PageDirectoryEntry.GetEntryPosition(i);
                if (PageDirectoryEntry.IsEmpty(Buffer.Span, offset))
                    continue;

                var current = PageDirectoryEntry.FromBuffer(Buffer.Span, PageDirectoryEntry.GetEntryPosition(i));

                if (lastDirectoryEntry.DataLength == 0 && lastDirectoryEntry.DataOffset == 0)
                {
                    if (Constants.PAGE_SIZE - Constants.PAGE_HEADER_SIZE - current.EndPositionOfData() >= length)
                    {
                        return (0, i);
                    }

                    lastDirectoryEntry = PageDirectoryEntry.FromBuffer(Buffer.Span, offset);
                    lastId = i;
                    continue;
                }

                if (current.EndPositionOfData() - lastDirectoryEntry.EndPositionOfData() >= length)
                    return (lastId, i);
            }

            // no free space
            return (byte.MaxValue, byte.MaxValue);
        }

        public Span<byte> Insert(ushort length, out byte entryIndex)
        {
            Guard.Argument(CheckIsInsertPossible(length)).True();

            var entry = GetNextDirectoryEntry(length, out entryIndex);
            var entryPos = PageDirectoryEntry.GetEntryPosition(entryIndex);

            if (entryIndex > Header.HighestEntryId) HighestDirectoryEntryId = entryIndex;

            entry.ToBuffer(Buffer.Span, entryPos);
            var pageHeader = new PageHeader(Header.PageId, Header.Type, Header.PrevPageId, Header.NextPageId,
                (ushort)(Header.UsedBytes + length),
                (byte)(Header.ItemCount + 1), (ushort)(Header.NextFreePosition + length), Header.UnalignedFreeBytes, HighestDirectoryEntryId);
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

        protected byte GetNextHighestDirectoryEntry(byte index)
        {
            if (index == 1)
            {
                return 0;
            }

            byte startIndex = (byte) (index - 1);

            for (byte i = startIndex; i != 0; i--)
            {
                var offset = PageDirectoryEntry.GetEntryPosition(i);
                if (!PageDirectoryEntry.IsEmpty(Buffer.Span, offset))
                    return i;
            }

            // found only empty entries, then we can start from the beginning
            return 0;
        }

        protected PageDirectoryEntry GetNextDirectoryEntry(ushort length, out byte index)
        {
            index = byte.MaxValue;
            if (Header.HighestEntryId == 0)
            {
                index = 1;
                return new PageDirectoryEntry(Header.NextFreePosition, length);
            }

            for (byte i = 1; i <= Header.HighestEntryId; i++)
            {
                var pos = PageDirectoryEntry.GetEntryPosition(i);
                if (PageDirectoryEntry.IsEmpty(Buffer.Span, pos))
                {
                    var entry = PageDirectoryEntry.FromBuffer(Buffer.Span, pos);
                    return entry;
                }
            }
            if (Header.HighestEntryId == byte.MaxValue)
                throw new Exception("Full");

            index = (byte)(HighestDirectoryEntryId + 1);
            return new PageDirectoryEntry(Header.NextFreePosition, length);
        }

        protected virtual string GenerateLayoutString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine();
            stringBuilder.AppendLine($"------------- Content");
            stringBuilder.AppendLine($"|HHHHHHHHHHH| Header {Constants.PAGE_HEADER_SIZE} bytes");

            for (int i = HighestDirectoryEntryId; i > 0; i--)
            {
                
            }


            return "";
        }

        public override string ToString()
        {
            return Header.ToString();
        }

        public static uint PageOffset(uint pageId) => pageId * Constants.PAGE_SIZE;
    }
}
