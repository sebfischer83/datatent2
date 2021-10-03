// # SPDX-License-Identifier: MIT
// # Copyright 2021
// # Sebastian Fischer sebfischer@gmx.net

using System;
using System.Collections.Generic;
using System.IO.Enumeration;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using Datatent2.Contracts;
using Datatent2.Core.Block;
using Datatent2.Core.Memory;

namespace Datatent2.Core.Page.Data
{
    internal sealed class DataPage : BasePage
    {
        public DataPage(IBufferSegment buffer) : base(buffer)
        {
            if (Header.Type != PageType.Data)
                throw new Exception($"Invalid page type! {nameof(PageType.Data)} expected but get {Header.Type}");
        }

        public DataPage(IBufferSegment buffer, uint id) : base(buffer, id, PageType.Data)
        {
        }
        
        public DataBlock InsertBlock(ushort length, bool isFollowingBlock)
        {
            var span = Insert((ushort)(length + Constants.BLOCK_HEADER_SIZE), out var index);

            return new DataBlock(this, index, PageAddress.Empty, isFollowingBlock);
        }
    }
}
