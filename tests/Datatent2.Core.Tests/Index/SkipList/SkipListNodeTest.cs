using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Datatent2.Core.Page;
using Datatent2.Core.Services.Index.SkipList;
using Shouldly;
using Xunit;

namespace Datatent2.Core.Tests.Index.SkipList
{
    public class SkipListNodeTest
    {
        [Fact]
        public void CreateNodeTest()
        {
            SkipListNode node = new SkipListNode(35, new PageAddress(1, 1), 16);
            
            node.Forward.Length.ShouldBe(16);
            node.Key.ShouldBe(35);
            node.Key.ShouldBeOfType(typeof(int));
            node.PageAddress.PageId.ShouldBe((uint)1);
            node.PageAddress.SlotId.ShouldBe((byte)1);
        }

        [Fact]
        public void CreateNodeTest2()
        {
            SkipListNode node = new SkipListNode((ulong)35, new PageAddress(1, 1), 5);

            node.Forward.Length.ShouldBe(5);
            node.Key.ShouldBe(35);
            node.Key.ShouldBeOfType(typeof(ulong));
            node.PageAddress.PageId.ShouldBe((uint)1);
            node.PageAddress.SlotId.ShouldBe((byte)1);
        }

        [Fact]
        public void CreateStartNodeTest()
        {
            SkipListNode node = new SkipListNode( 5);

            node.Forward.Length.ShouldBe(5);
            node.Key.ShouldBe(null);
            node.PageAddress.PageId.ShouldBe((uint)0);
            node.PageAddress.SlotId.ShouldBe((byte)0);
        }

        [Fact]
        public void ToAndFromByteTest()
        {
            SkipListNode node = new SkipListNode(35, new PageAddress(1, 1), 5);

            node.Forward[0] = new PageAddress(1, 1);
            node.Forward[1] = new PageAddress(2, 2);
            node.Forward[2] = new PageAddress(3, 3);
            node.Forward[3] = new PageAddress(4, 4);
            node.Forward[4] = new PageAddress(5, 5);

            var bytes = node.ToBytes();

            var node2 = SkipListNode.FromBytes(bytes);
            node2.Forward[3].PageId.ShouldBe((uint)4);
            node2.Forward[3].SlotId.ShouldBe((byte)4);
        }
    }
}
