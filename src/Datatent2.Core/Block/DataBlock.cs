// # SPDX-License-Identifier: MIT
// # Copyright 2021
// # Sebastian Fischer sebfischer@gmx.net

using Datatent2.Core.Page;
using Datatent2.Core.Page.Data;

namespace Datatent2.Core.Block
{
    internal class DataBlock : Block<DataPage, BlockHeader>
    {
        public DataBlock(DataPage page, byte entryId) : base(page, entryId)
        {
            var memory = page.GetDataByIndex(entryId);
            Header = BlockHeader.FromBuffer(memory, 0);
        }

        public DataBlock(DataPage page,
            byte entryId,
            PageAddress nextBlock,
            bool isFollowingBlock) : base(page,
            entryId,
            nextBlock,
            isFollowingBlock)
        {
            var memory = page.GetDataByIndex(entryId);
            Header = new BlockHeader(nextBlock, isFollowingBlock);
            Header.ToBuffer(memory);
        }


        public override void SetFollowingBlock(PageAddress pageAddress)
        {
            Header = new BlockHeader(pageAddress, Header.IsFollowingBlock);
            Header.ToBuffer(Page.GetDataByIndex(EntryId));
        }
    }
}
