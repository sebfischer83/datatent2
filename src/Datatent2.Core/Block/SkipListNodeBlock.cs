using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Datatent2.Core.Page;
using Datatent2.Core.Page.Index;

namespace Datatent2.Core.Block
{
    internal class SkipListNodeBlock : Block<IndexPage, BlockHeader>
    {
        public SkipListNodeBlock(IndexPage page, byte entryId) : base(page, entryId)
        {
        }

        public SkipListNodeBlock(IndexPage page, byte entryId, PageAddress nextBlock, bool isFollowingBlock) : base(page, entryId, nextBlock, isFollowingBlock)
        {
        }

        public override void SetFollowingBlock(PageAddress pageAddress)
        {
            Header = new BlockHeader(pageAddress, Header.IsFollowingBlock);
            Header.ToBuffer(Page.GetDataByIndex(EntryId));
        }
    }
}
