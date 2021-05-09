using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Datatent2.Core.Memory;
using Dawn;

namespace Datatent2.Core.Page.Index
{
    internal class IndexPage : BasePage
    {
        public IndexPage(IBufferSegment buffer) : base(buffer)
        {
            Guard.Argument(Header.Type == PageType.Index).True();
        }

        public IndexPage(IBufferSegment buffer, uint id) : base(buffer, id, PageType.Index)
        {
        }

        public void InsertIndexNode()
        {
        }
    }

    public class IndexKey
    {

    }
}
