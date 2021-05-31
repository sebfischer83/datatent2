using Datatent2.Core.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Datatent2.Core.Page.Overflow
{
    internal sealed class OverflowPage : BasePage
    {
        public OverflowPage(IBufferSegment buffer) : base(buffer)
        {
        }

        public OverflowPage(IBufferSegment buffer, uint id) : base(buffer, id, PageType.Overflow)
        {
        }
    }
}
