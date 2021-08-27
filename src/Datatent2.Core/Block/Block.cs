// # SPDX-License-Identifier: MIT
// # Copyright 2021
// # Sebastian Fischer sebfischer@gmx.net

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Datatent2.Contracts;
using Datatent2.Core.Memory;
using Datatent2.Core.Page;

namespace Datatent2.Core.Block
{
    /// <summary>
    /// A block encapsulates a part of data and give them a meaning
    /// </summary>
    /// <typeparam name="TPage"></typeparam>
    /// <typeparam name="THeader"></typeparam>
    internal abstract class Block<TPage, THeader> where TPage : BasePage where THeader : struct
    {
        /// <summary>
        /// The Header of the block
        /// </summary>
        public THeader Header { get; protected set; }
        
        /// <summary>
        /// The address of the block
        /// </summary>
        public PageAddress Position { get; protected set; }

        /// <summary>
        /// The associated page
        /// </summary>
        protected readonly TPage Page;
        /// <summary>
        /// The entry in the given page
        /// </summary>
        protected readonly byte EntryId;


        /// <summary>
        /// Initializes a new instance of the <see cref="Block"/> class.
        /// </summary>
        /// <param name="page">The page the block belongs to.</param>
        /// <param name="entryId">The entry id of the block.</param>
        protected Block(TPage page, byte entryId)
        {
            Page = page;
            EntryId = entryId;
            Position = new PageAddress(Page.Id, EntryId);
        }

        public void FillData(Span<byte> data, uint checksum = 0)
        {
            var dataArea = Page.GetDataByIndex(EntryId)[Constants.BLOCK_HEADER_SIZE..];
            dataArea.WriteBytes(0, data);
            if (checksum > 0)
                dataArea.WriteUInt32(dataArea.Length - sizeof(uint), checksum);
        }

        public byte[] GetData()
        {
            var dataArea = Page.GetDataByIndex(EntryId).Slice(Constants.BLOCK_HEADER_SIZE);
            return dataArea.ToArray();
        }

        protected Block(TPage page, byte entryId, PageAddress nextBlock, bool isFollowingBlock)
        {
            Page = page;
            EntryId = entryId;
            Position = new PageAddress(Page.Id, EntryId);
        }

        public override string ToString()
        {
            return Position.ToString();
        }

        public abstract void SetFollowingBlock(PageAddress pageAddress);
    }
}
