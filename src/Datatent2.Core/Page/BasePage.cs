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

        public bool IsFull => Header.ItemCount == byte.MaxValue || FreeBytes == 0;
        public ushort UsedBytes => Header.UsedBytes;
        public ushort FreeBytes => (ushort) (Constants.MAX_USABLE_BYTES_IN_PAGE - Header.UsedBytes - Header.UnalignedFreeBytes);

        protected Memory.BufferSegment Buffer;
        protected PageHeader Header;



        protected BasePage(Memory.BufferSegment buffer)
        {
            Header = PageHeader.FromBuffer(buffer.Span);
            Buffer = buffer;
        }

        protected BasePage(Memory.BufferSegment buffer, uint id, PageType pageType)
        {
            Guard.Argument((int)buffer.Length + 1).GreaterThan(Constants.PAGE_SIZE);
            Buffer = buffer;
            Header = new PageHeader(id, pageType);
        }

        public Span<byte> Insert(int length)
        {
            Guard.Argument(length).LessThan(FreeBytes + 1);

            return null;
        }

        public static uint PageOffset(uint pageId) => pageId * Constants.PAGE_SIZE;
    }
}
