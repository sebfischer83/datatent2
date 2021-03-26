using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Datatent2.Core.Memory;
using Datatent2.Core.Page;

namespace Datatent2.Core.Block
{
    internal abstract class Block<T, H> where T: BasePage where H: struct 
    {
        public H Header { get; protected set; }

        public PageAddress Position { get; protected set; }

        protected readonly T Page;
        protected readonly byte _entryId;
        
        protected Block(T page, byte entryId)
        {
            Page = page;
            _entryId = entryId;
            Position = new PageAddress(Page.Id, _entryId);
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
        }

        public abstract void SetFollowingBlock(PageAddress pageAddress);
    }
}
