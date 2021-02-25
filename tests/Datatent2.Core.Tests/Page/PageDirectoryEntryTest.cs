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
    public class PageDirectoryEntryTest
    {
        [Fact]
        public void IsEmptyTest()
        {
            Span<byte> span = new byte[Constants.PAGE_SIZE];
            span.Clear();
            var pos = PageDirectoryEntry.GetEntryPosition(1);

            PageDirectoryEntry.IsEmpty(span, pos).ShouldBeTrue();

            PageDirectoryEntry pageDirectoryEntry = new PageDirectoryEntry(1, 324);
            pageDirectoryEntry.ToBuffer(span, pos);

            PageDirectoryEntry.IsEmpty(span, pos).ShouldBeFalse();
        }

        [Fact]
        public void WriteAndReadTest()
        {
            Span<byte> span = new byte[Constants.PAGE_SIZE];
            span.Clear();
            var pos = PageDirectoryEntry.GetEntryPosition(1);

            PageDirectoryEntry pageDirectoryEntry = new PageDirectoryEntry(1, 324);
            pageDirectoryEntry.ToBuffer(span, pos);

            var res = PageDirectoryEntry.FromBuffer(span, pos);
            res.DataLength.ShouldBe(pageDirectoryEntry.DataLength);
            res.DataOffset.ShouldBe(pageDirectoryEntry.DataOffset);

            pos = PageDirectoryEntry.GetEntryPosition(7);

            pageDirectoryEntry = new PageDirectoryEntry(7, 324);
            pageDirectoryEntry.ToBuffer(span, pos);

            res = PageDirectoryEntry.FromBuffer(span, pos);
            res.DataLength.ShouldBe(pageDirectoryEntry.DataLength);
            res.DataOffset.ShouldBe(pageDirectoryEntry.DataOffset);
        }

        [Fact]
        public void EndPositionOfDataTest()
        {
            Span<byte> span = new byte[Constants.PAGE_SIZE];
            span.Clear();

            PageDirectoryEntry pageDirectoryEntry = new PageDirectoryEntry(1, 324);
            pageDirectoryEntry.EndPositionOfData().ShouldBe((ushort)(1 + 324));
        }

        [Fact]
        public void GetEntryPositionTest()
        {
            PageDirectoryEntry.GetEntryPosition(1).ShouldBe((ushort)(Constants.PAGE_SIZE - Constants.PAGE_DIRECTORY_ENTRY_SIZE - 1));
            PageDirectoryEntry.GetEntryPosition(10).ShouldBe((ushort)(Constants.PAGE_SIZE - Constants.PAGE_DIRECTORY_ENTRY_SIZE * 10 - 1));
            PageDirectoryEntry.GetEntryPosition(3).ShouldBe((ushort)(Constants.PAGE_SIZE - Constants.PAGE_DIRECTORY_ENTRY_SIZE * 3 - 1));
        }
    }
}
