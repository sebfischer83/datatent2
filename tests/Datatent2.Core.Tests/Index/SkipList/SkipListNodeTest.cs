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
            SkipListNode<long> node = new SkipListNode<long>(35, new PageAddress(1, 1), 5);
            
            node.Forward.Length.ShouldBe(5);
            node.Key.ShouldBe(35);
            node.PageAddress.PageId.ShouldBe((uint)1);
            node.PageAddress.SlotId.ShouldBe((byte)1);
        }

        [Fact]
        public void CreateStartNodeTest()
        {
            SkipListNode<long> node = new SkipListNode<long>( 5);

            node.Forward.Length.ShouldBe(5);
            node.Key.ShouldBe(0);
            node.PageAddress.PageId.ShouldBe((uint)0);
            node.PageAddress.SlotId.ShouldBe((byte)0);
        }

        [Fact]
        public void ToAndFromByteTest()
        {
            SkipListNode<int> node = new SkipListNode<int>(35, new PageAddress(1, 1), 5);

            var bytes = node.ToBytes();

            var node2 = SkipListNode<int>.FromBytes(bytes);
        }
    }
}
