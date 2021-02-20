using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Datatent2.Core.Memory;

namespace Datatent2.Core.Page
{
    internal class DataPage : BasePage
    {
        public DataPage(BufferSegment buffer) : base(buffer)
        {
            if (Header.Type != PageType.Data)
                throw new Exception("Invalid page type!");
        }

        public DataPage(BufferSegment buffer, uint id) : base(buffer, id, PageType.Data)
        {
        }
    }
}
