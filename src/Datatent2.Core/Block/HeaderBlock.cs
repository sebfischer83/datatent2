using Datatent2.Core.Page;
using Datatent2.Core.Page.Header;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Datatent2.Core.Block
{
    internal class HeaderBlock : Block<HeaderPage, BlockHeader>
    {
        public HeaderBlock(HeaderPage page, byte entryId) : base(page, entryId)
        {
        }

        public override void SetFollowingBlock(PageAddress pageAddress)
        {
            Header = new BlockHeader(pageAddress, Header.IsFollowingBlock);
            Header.ToBuffer(Page.GetDataByIndex(EntryId));
        }
    }
}
