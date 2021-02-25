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
        public void InsertTest_Continuous()
        {
            var bogus = new Bogus.Randomizer();

            byte[] toInsert = Encoding.UTF8.GetBytes("Hello World!");
            ushort insertLength = (ushort) toInsert.Length;
            using BufferSegment bufferSegment = new BufferSegment(Constants.PAGE_SIZE);
            PageHeader header = new PageHeader(1, PageType.Data);

            header.ToBuffer(bufferSegment.Span, 0);
            
            DataPage dataPage = new DataPage(bufferSegment);
            dataPage.IsFull.ShouldBeFalse();
            var ins = dataPage.Insert(insertLength, out var index);
            index.ShouldBe((byte)1);
            ins.Length.ShouldBe(insertLength);
            ins.WriteBytes(0, toInsert);
            dataPage.FreeContinuousBytes.ShouldBe((ushort)(Constants.MAX_USABLE_BYTES_IN_PAGE - insertLength));
            bufferSegment.Span.Slice(Constants.PAGE_HEADER_SIZE, insertLength).ToArray().ShouldBe(toInsert);

            ins = dataPage.Insert(insertLength, out index);
            index.ShouldBe((byte)2);
            ins.Length.ShouldBe(insertLength);
            ins.WriteBytes(0, toInsert);
            dataPage.FreeContinuousBytes.ShouldBe((ushort)(Constants.MAX_USABLE_BYTES_IN_PAGE - 4 - insertLength * 2));
            bufferSegment.Span.Slice(Constants.PAGE_HEADER_SIZE + insertLength, insertLength).ToArray().ShouldBe(toInsert);
        }

        [Fact]
        public void DeleteTest()
        {
            var bogus = new Bogus.Randomizer();
            const int insertLength = 20;
            using BufferSegment bufferSegment = new BufferSegment(Constants.PAGE_SIZE);
            PageHeader header = new PageHeader(1, PageType.Data);
            header.ToBuffer(bufferSegment.Span, 0);
            byte[] toInsert = TestHelper.GenerateByteArray(insertLength, 0xFF);
            DataPage dataPage = new DataPage(bufferSegment);
            dataPage.IsFull.ShouldBeFalse();
            byte index = 0;

            for (int i = 0; i < 10; i++)
            {
                var ins = dataPage.Insert(insertLength, out index);
                ins.WriteBytes(0, toInsert);
            }
            // Highest index should be 10
            index.ShouldBe((byte)10);
            dataPage.PageHeader.HighestEntryId.ShouldBe(index);

            var before = dataPage.PageHeader.UsedBytes;
            var unaligned = dataPage.PageHeader.UnalignedFreeBytes;
            unaligned.ShouldBe((ushort)0);
            // delete entry 3
            var dirEntryPosition = PageDirectoryEntry.GetEntryPosition(3);
            var dirEntry = PageDirectoryEntry.FromBuffer(bufferSegment.Span, dirEntryPosition);
            dirEntry.DataLength.ShouldBe((ushort)insertLength);
            dataPage.Delete(3);
            // data should be gone
            var data = bufferSegment.Span.Slice(dirEntry.DataOffset, dirEntry.DataLength);
            data.ToArray().ShouldAllBe(b => b == 0x00);
            dataPage.PageHeader.UsedBytes.ShouldBe((ushort)(before - insertLength));

            // delete entry 7
            dirEntryPosition = PageDirectoryEntry.GetEntryPosition(7);
            dirEntry = PageDirectoryEntry.FromBuffer(bufferSegment.Span, dirEntryPosition);
            dirEntry.DataLength.ShouldBe((ushort)insertLength);
            dataPage.Delete(7);
            // data should be gone
            data = bufferSegment.Span.Slice(dirEntry.DataOffset, dirEntry.DataLength);
            data.ToArray().ShouldAllBe(b => b == 0x00);

            // delete entry 10
            dirEntryPosition = PageDirectoryEntry.GetEntryPosition(10);
            dirEntry = PageDirectoryEntry.FromBuffer(bufferSegment.Span, dirEntryPosition);
            dirEntry.DataLength.ShouldBe((ushort)insertLength);
            dataPage.Delete(10);
            // data should be gone
            data = bufferSegment.Span.Slice(dirEntry.DataOffset, dirEntry.DataLength);
            data.ToArray().ShouldAllBe(b => b == 0x00);
            dataPage.PageHeader.HighestEntryId.ShouldBe((byte)9);

            dataPage.PageHeader.UnalignedFreeBytes.ShouldBe((ushort)40);
            var s = dataPage.ToString();
        }

        [Fact]
        public void GetMaxFreeSpace_End()
        {
            using BufferSegment bufferSegment = new BufferSegment(Constants.PAGE_SIZE);
            PageHeader header = new PageHeader(1, PageType.Data);
            header.ToBuffer(bufferSegment.Span, 0);
            DataPage dataPage = new DataPage(bufferSegment);
            dataPage.IsFull.ShouldBeFalse();

            byte[] toInsert = TestHelper.GenerateByteArray(3000, 0x0F);
            ushort insertLength = (ushort)toInsert.Length;
            byte index = 0;

            var ins = dataPage.Insert(insertLength, out index);
            ins.WriteBytes(0, toInsert);
            ins = dataPage.Insert(insertLength, out index);
            ins.WriteBytes(0, toInsert);

            var freeSpace = dataPage.GetMaxContiguounesFreeSpace();
            freeSpace.ShouldBe((ushort)dataPage.FreeContinuousBytes);
            freeSpace.ShouldBe((ushort)2152);
        }

        [Fact]
        public void GetMaxFreeSpace_Middle()
        {
            using BufferSegment bufferSegment = new BufferSegment(Constants.PAGE_SIZE);
            PageHeader header = new PageHeader(1, PageType.Data);
            header.ToBuffer(bufferSegment.Span, 0);
            DataPage dataPage = new DataPage(bufferSegment);
            dataPage.IsFull.ShouldBeFalse();

            byte[] toInsert = TestHelper.GenerateByteArray(36, 0x0F);
            ushort insertLength = (ushort)toInsert.Length;
            byte index = 0;

            for (int i = 0; i < 203; i++)
            {
                if (!dataPage.IsInsertPossible(insertLength))
                    continue;
                var ins = dataPage.Insert(insertLength, out index);
                ins.WriteBytes(0, toInsert);
                dataPage.PageHeader.HighestEntryId.ShouldBe(index);
            }
            dataPage.IsFull.ShouldBeTrue();
            var freeSpace = dataPage.GetMaxContiguounesFreeSpace();
            freeSpace.ShouldBe((ushort)40);
            dataPage.Delete(10);
            freeSpace = dataPage.GetMaxContiguounesFreeSpace();
            freeSpace.ShouldBe((ushort)40);
            dataPage.Delete(11);
            freeSpace = dataPage.GetMaxContiguounesFreeSpace();
            freeSpace.ShouldBe((ushort)72);
            dataPage.Delete(12);
            freeSpace = dataPage.GetMaxContiguounesFreeSpace();
            freeSpace.ShouldBe((ushort)108);
        }

        /// <summary>
        /// 
        /// </summary>
        [Fact]
        public void FindFreeSpaceBetweenTest_EmptySpaceOnStart()
        {
            using BufferSegment bufferSegment = new BufferSegment(Constants.PAGE_SIZE);
            bufferSegment.Span.Clear();
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
