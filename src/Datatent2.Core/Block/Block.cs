using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Datatent2.Core.Memory;
using Datatent2.Core.Page;

namespace Datatent2.Core.Block
{
    internal abstract class Block<T> where T: BasePage
    {
        public BlockHeader Header { get; private set; }

        public PageAddress Position { get; private set; }

        protected readonly T Page;
        private readonly byte _entryId;
        
        protected Block(T page, byte entryId)
        {
            Page = page;
            _entryId = entryId;
            Position = new PageAddress(Page.Id, _entryId);
            var memory = page.GetDataByIndex(entryId);
            Header = BlockHeader.FromBuffer(memory, 0);
        }
        
        public void FillData(Span<byte> data)
        {
            var dataArea = Page.GetDataByIndex(_entryId).Slice(Constants.BLOCK_HEADER_SIZE);
            dataArea.WriteBytes(0, data);
        }

        public byte[] GetData()
        {
            var dataArea = Page.GetDataByIndex(_entryId).Slice(Constants.BLOCK_HEADER_SIZE);
            return dataArea.ToArray();
        }

        protected Block(T page, byte entryId, PageAddress nextBlock, bool isFollowingBlock)
        {
            Page = page;
            _entryId = entryId;
            Position = new PageAddress(Page.Id, _entryId);
            var memory = page.GetDataByIndex(entryId);
            Header = new BlockHeader(nextBlock, isFollowingBlock);
            Header.ToBuffer(memory);
        }

        public void SetFollowingBlock(PageAddress pageAddress)
        {
            Header = new BlockHeader(pageAddress, Header.IsFollowingBlock);
            Header.ToBuffer(Page.GetDataByIndex(_entryId));
        }
    }
}
