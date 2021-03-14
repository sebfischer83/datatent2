using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Datatent2.Core.Page;

namespace Datatent2.Core.Block
{
    internal class DataBlock : Block<DataPage>
    {
        public DataBlock(DataPage page, byte entryId) : base(page, entryId)
        {
        }

        public DataBlock(DataPage page, byte entryId, PageAddress nextBlock, bool isFollowingBlock) : base(page, entryId, nextBlock, isFollowingBlock)
        {
        }

        
    }
}
