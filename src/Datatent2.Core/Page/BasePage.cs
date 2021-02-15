using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Datatent2.Core.Memory;

namespace Datatent2.Core.Page
{
    internal abstract class BasePage
    {
        public uint Id => Header.PageId;

        protected Memory.BufferSegment Buffer;
        protected PageHeader Header;

        protected BasePage(Memory.BufferSegment buffer)
        {
            Header = PageHeader.FromBuffer(buffer.Span);
            Buffer = buffer;
        }

        protected BasePage(Memory.BufferSegment buffer, uint id, PageType pageType)
        {
            Buffer = buffer;
            Header = new PageHeader(id, pageType);
        }
    }
}
