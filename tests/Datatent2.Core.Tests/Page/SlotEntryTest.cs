using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Datatent2.Core.Page;
using Shouldly;
using Xunit;

namespace Datatent2.Core.Tests.Page
{
    public class SlotEntryTest
    {
        [Fact]
        public void IsEmptyTest()
        {
            Span<byte> span = new byte[Constants.PAGE_SIZE];
            span.Clear();
            var pos = SlotEntry.GetEntryPosition(1);

            SlotEntry.IsEmpty(span, pos).ShouldBeTrue();

            SlotEntry slotEntry = new SlotEntry(1, 324);
            slotEntry.ToBuffer(span, pos);

            SlotEntry.IsEmpty(span, pos).ShouldBeFalse();
        }

        [Fact]
        public void WriteAndReadTest()
        {
            Span<byte> span = new byte[Constants.PAGE_SIZE];
            span.Clear();
            var pos = SlotEntry.GetEntryPosition(1);

            SlotEntry slotEntry = new SlotEntry(1, 324);
            slotEntry.ToBuffer(span, pos);

            var res = SlotEntry.FromBuffer(span, pos);
            res.DataLength.ShouldBe(slotEntry.DataLength);
            res.DataOffset.ShouldBe(slotEntry.DataOffset);

            pos = SlotEntry.GetEntryPosition(7);

            slotEntry = new SlotEntry(7, 324);
            slotEntry.ToBuffer(span, pos);

            res = SlotEntry.FromBuffer(span, pos);
            res.DataLength.ShouldBe(slotEntry.DataLength);
            res.DataOffset.ShouldBe(slotEntry.DataOffset);
        }

        [Fact]
        public void EndPositionOfDataTest()
        {
            Span<byte> span = new byte[Constants.PAGE_SIZE];
            span.Clear();

            SlotEntry slotEntry = new SlotEntry(1, 324);
            slotEntry.EndPositionOfData().ShouldBe((ushort)(1 + 324));
        }

        [Fact]
        public void GetEntryPositionTest()
        {
            SlotEntry.GetEntryPosition(1).ShouldBe((ushort)(Constants.PAGE_SIZE - Constants.PAGE_DIRECTORY_ENTRY_SIZE));
            SlotEntry.GetEntryPosition(10).ShouldBe((ushort)(Constants.PAGE_SIZE - Constants.PAGE_DIRECTORY_ENTRY_SIZE * 10));
            SlotEntry.GetEntryPosition(3).ShouldBe((ushort)(Constants.PAGE_SIZE - Constants.PAGE_DIRECTORY_ENTRY_SIZE * 3));
        }
    }
}
