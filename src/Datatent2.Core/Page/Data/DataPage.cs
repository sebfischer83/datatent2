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
    /// <summary>
    /// The page that holds the actual data
    /// </summary>
    internal sealed class DataPage : BasePage
    {
        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="buffer"></param>
        public DataPage(IBufferSegment buffer) : base(buffer)
        {
            if (Header.Type != PageType.Data)
                throw new Exception($"Invalid page type! {nameof(PageType.Data)} expected but get {Header.Type}");
        }

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="id"></param>
        public DataPage(IBufferSegment buffer, uint id) : base(buffer, id, PageType.Data)
        {
        }
        
        /// <summary>
        /// Get the <seealso cref="DataBlock"/> for inserting
        /// </summary>
        /// <param name="length"></param>
        /// <param name="isFollowingBlock"></param>
        /// <returns></returns>
        public DataBlock InsertBlock(ushort length, bool isFollowingBlock)
        {
            // only need the index here
            _ = Insert((ushort)(length + Constants.BLOCK_HEADER_SIZE), out var index);

            return new DataBlock(this, index, PageAddress.Empty, isFollowingBlock);
        } 
    }
}
