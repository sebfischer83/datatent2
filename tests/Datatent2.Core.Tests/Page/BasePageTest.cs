using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Datatent2.Core.Memory;
using Datatent2.Core.Page;
using Shouldly;
using Xunit;

namespace Datatent2.Core.Tests.Page
{
    public class BasePageTest
    {
        [Fact]
        public void InsertTest()
        {
            var bogus = new Bogus.Randomizer();
            const int insertLength = 20;
            using BufferSegment bufferSegment = new BufferSegment(Constants.PAGE_SIZE);
            PageHeader header = new PageHeader(1, PageType.Data);

            header.ToBuffer(bufferSegment.Span, 0);
            byte[] toInsert = TestHelper.GenerateByteArray(insertLength, 0xFF);
            
            DataPage dataPage = new DataPage(bufferSegment);
            dataPage.IsFull.ShouldBeFalse();
            var ins = dataPage.Insert(insertLength, out var index);
            index.ShouldBe((byte)1);
            ins.Length.ShouldBe(insertLength);
            ins.WriteBytes(0, toInsert);
            dataPage.FreeBytes.ShouldBe((ushort)(Constants.MAX_USABLE_BYTES_IN_PAGE - 24));
            bufferSegment.Span.Slice(Constants.PAGE_HEADER_SIZE, insertLength).ToArray().ShouldAllBe(b => b > 0x00);

            ins = dataPage.Insert(insertLength, out index);
            index.ShouldBe((byte)2);
            ins.Length.ShouldBe(insertLength);
            ins.WriteBytes(0, toInsert);
            dataPage.FreeBytes.ShouldBe((ushort)(Constants.MAX_USABLE_BYTES_IN_PAGE - 48));
            bufferSegment.Span.Slice(Constants.PAGE_HEADER_SIZE + insertLength, insertLength).ToArray().ShouldAllBe(b => b > 0x00);
        }

        /// <summary>
        /// 
        /// </summary>
        [Fact]
        public void FindFreeSpaceBetweenTest_EmptySpaceOnStart()
        {
            using BufferSegment bufferSegment = new BufferSegment(Constants.PAGE_SIZE);
            PageHeader header = new PageHeader(1, PageType.Data, 0, 0, 5000, 0, 7032, 2000, 2);
            PageDirectoryEntry directoryEntry = new PageDirectoryEntry(2032, 5000);
            directoryEntry.ToBuffer(bufferSegment.Span, PageDirectoryEntry.GetEntryPosition(2));
            header.ToBuffer(bufferSegment.Span);

            DataPage dataPage = new DataPage(bufferSegment);

            // Empty space should be between header and 2 entry
            var res = dataPage.FindFreeSpaceBetween(1000);
            res.Index1.ShouldBe((byte)0);
            res.Index2.ShouldBe((byte)2);
            

        }
    }
}
