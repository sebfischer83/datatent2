﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Datatent2.Core.Block;
using Datatent2.Core.Memory;

namespace Datatent2.Core.Page
{
    internal class DataPage : BasePage
    {
        public DataPage(IBufferSegment buffer) : base(buffer)
        {
            if (Header.Type != PageType.Data)
                throw new Exception("Invalid page type!");
        }

        public DataPage(IBufferSegment buffer, uint id) : base(buffer, id, PageType.Data)
        {
        }

        public DataBlock InsertBlock(ushort length, bool isFollowingBlock)
        {
            var span = base.Insert(length, out var index);

            return new DataBlock(this, index, PageAddress.Empty, isFollowingBlock);
        }
    }
}
