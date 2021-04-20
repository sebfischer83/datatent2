// # SPDX-License-Identifier: MIT
// # Copyright 2021
// # Sebastian Fischer sebfischer@gmx.net

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Datatent2.Core.Page;

namespace Datatent2.Core.Block
{
    internal class DataBlock : Block<DataPage, BlockHeader>
    {
        public static readonly uint EMPTY_CHECKSUM = 0;

        public DataBlock(DataPage page, byte entryId) : base(page, entryId)
        {
            var memory = page.GetDataByIndex(entryId);
            Header = BlockHeader.FromBuffer(memory, 0);
        }

        public DataBlock(DataPage page,
            byte entryId,
            PageAddress nextBlock,
            bool isFollowingBlock,
            uint checksum) : base(page,
            entryId,
            nextBlock,
            isFollowingBlock)
        {
            var memory = page.GetDataByIndex(entryId);
            Header = new BlockHeader(nextBlock, isFollowingBlock, checksum);
            Header.ToBuffer(memory);
        }


        public override void SetFollowingBlock(PageAddress pageAddress)
        {
            Header = new BlockHeader(pageAddress, Header.IsFollowingBlock, DataBlock.EMPTY_CHECKSUM);
            Header.ToBuffer(Page.GetDataByIndex(_entryId));
        }
    }
}
