using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Datatent2.Core.Memory;

namespace Datatent2.Core.Page
{
    internal class DirectoryPage : BasePage
    {
        public DirectoryPage(BufferSegment buffer) : base(buffer)
        {
            if (Header.Type != PageType.Directory)
                throw new Exception("Invalid page type!");
        }

        public DirectoryPage(BufferSegment buffer, uint id) : base(buffer, id, PageType.Directory)
        {
        }
    }
}
