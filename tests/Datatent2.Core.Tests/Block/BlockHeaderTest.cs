using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Datatent2.Contracts;
using Datatent2.Core.Block;
using Datatent2.Core.Page;
using ObjectLayoutInspector;
using Shouldly;
using Xunit;
using TypeLayout = System.Reflection.Metadata.TypeLayout;

namespace Datatent2.Core.Tests.Block
{
    public class BlockHeaderTest
    {
        [Fact]
        public void Write_Read_Test()
        {
            int numberOfElements = 2000;
            int neededSize = numberOfElements * Constants.BLOCK_HEADER_SIZE;
            Span<byte> buffer = new byte[neededSize];

            for (int i = 0; i < numberOfElements; i++)
            {
                BlockHeader blockHeader = new BlockHeader(new PageAddress((uint)i, 1), false);
                blockHeader.ToBuffer(buffer, Constants.BLOCK_HEADER_SIZE * i);
            }
             
            for (int i = 0; i < numberOfElements; i++)
            {
                var blockHeader = BlockHeader.FromBuffer(buffer, Constants.BLOCK_HEADER_SIZE * i);
                //blockHeader.Checksum.ShouldBe(UInt32.MaxValue);
                blockHeader.NextBlockAddress.SlotId.ShouldBe((byte)1);
                blockHeader.NextBlockAddress.PageId.ShouldBe((uint)i);
                blockHeader.IsFollowingBlock.ShouldBeFalse();
            }
        }

        [Fact]
        public void LayoutTest()
        {
            var layout = ObjectLayoutInspector.TypeLayout.GetLayout<BlockHeader>(null, true);
            System.Runtime.InteropServices.Marshal.SizeOf(typeof(BlockHeader)).ShouldBe(Constants.BLOCK_HEADER_SIZE);
            //layout.Size.ShouldBe(Constants.BLOCK_HEADER_SIZE);
        }
    }
}
