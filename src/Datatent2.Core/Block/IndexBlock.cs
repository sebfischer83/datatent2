using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Datatent2.Contracts;
using Datatent2.Core.Page;
using Datatent2.Core.Page.Index;

namespace Datatent2.Core.Block
{
    /// <summary>
    /// A block that holds index nodes
    /// </summary>
    internal class IndexBlock : Block<IndexPage, BlockHeader>
    {
        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="page"></param>
        /// <param name="entryId"></param>
        public IndexBlock(IndexPage page, byte entryId) : base(page, entryId)
        {
        }

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="page"></param>
        /// <param name="entryId"></param>
        /// <param name="nextBlock"></param>
        /// <param name="isFollowingBlock"></param>
        public IndexBlock(IndexPage page, byte entryId, PageAddress nextBlock, bool isFollowingBlock) : base(page, entryId, nextBlock, isFollowingBlock)
        {
        }

        /// <inheritdoc />
        public override void SetFollowingBlock(PageAddress pageAddress)
        {
            Header = new BlockHeader(pageAddress, Header.IsFollowingBlock);
            Header.ToBuffer(Page.GetDataByIndex(EntryId));
        }
    }
}
