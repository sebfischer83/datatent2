using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Datatent2.Contracts;
using Datatent2.Core.Memory;
using Datatent2.Core.Page;
using Datatent2.Core.Page.Data;
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
            ushort insertLength = (ushort)toInsert.Length;
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
        public void InsertTest_MaxCount()
        {
            var bogus = new Bogus.Randomizer();

            byte[] toInsert = TestHelper.GenerateByteArray(4, 0x0F);
            ushort insertLength = (ushort)toInsert.Length;
            using BufferSegment bufferSegment = new BufferSegment(Constants.PAGE_SIZE);
            PageHeader header = new PageHeader(1, PageType.Data);

            header.ToBuffer(bufferSegment.Span, 0);

            DataPage dataPage = new DataPage(bufferSegment);
            dataPage.IsFull.ShouldBeFalse();
            byte index = 0;
            for (int i = 0; i < 255; i++)
            {
                var ins = dataPage.Insert(insertLength, out index);
                ins.WriteBytes(0, toInsert);
            }

            dataPage.IsInsertPossible(1).ShouldBe(false);
            Should.Throw<Exception>(() =>
            {
                dataPage.Insert(insertLength, out var index);
            });
            dataPage.Delete(100);
            dataPage.IsInsertPossible(1).ShouldBe(true);
            dataPage.Insert(insertLength, out index);
            index.ShouldBe((byte)100);
        }


        [Fact]
        public void InsertTest_Middle()
        {
            using BufferSegment bufferSegment = new BufferSegment(Constants.PAGE_SIZE);
            PageHeader header = new PageHeader(1, PageType.Data);
            header.ToBuffer(bufferSegment.Span, 0);
            DataPage dataPage = new DataPage(bufferSegment);
            dataPage.IsFull.ShouldBeFalse();

            byte[] toInsert = TestHelper.GenerateByteArray(36, 0x0F);
            ushort insertLength = (ushort)toInsert.Length;
            byte index = 0;

            for (int i = 0; i < 202; i++)
            {
                if (!dataPage.IsInsertPossible(insertLength))
                    continue;
                var ins = dataPage.Insert(insertLength, out index);
                ins.WriteBytes(0, toInsert);
                dataPage.PageHeader.HighestSlotId.ShouldBe(index);
            }
            var freeSpaceExpected = (ushort)(Constants.PAGE_SIZE - Constants.PAGE_HEADER_SIZE -
                                             202 * Constants.PAGE_DIRECTORY_ENTRY_SIZE - 202 * 36);

            var freeSpace = dataPage.GetMaxContiguounesFreeSpace();
            freeSpace.ShouldBe(freeSpaceExpected);
            dataPage.Delete(10);
            freeSpace = dataPage.GetMaxContiguounesFreeSpace();
            freeSpace.ShouldBe(freeSpaceExpected);
            dataPage.Delete(11);
            freeSpace = dataPage.GetMaxContiguounesFreeSpace();
            freeSpace.ShouldBe((ushort)72);
            dataPage.Delete(12);
            freeSpace = dataPage.GetMaxContiguounesFreeSpace();
            freeSpace.ShouldBe((ushort)108);
            var toTest =dataPage.Insert(50, out var newIndex);
            toTest.WriteBytes(0, TestHelper.GenerateByteArray(50, 0xFF));

            dataPage.PageHeader.ItemCount.ShouldBe((byte)200);
            var dataSegment = bufferSegment.Span.Slice(Constants.PAGE_HEADER_SIZE,
                Constants.PAGE_SIZE - Constants.PAGE_HEADER_SIZE -
                (dataPage.PageHeader.ItemCount * Constants.PAGE_DIRECTORY_ENTRY_SIZE)).ToArray();
            dataSegment.Count(b => b == 0xFF).ShouldBe(50);
            dataSegment.Count(b => b == 0x0F).ShouldBe((dataPage.PageHeader.ItemCount - 1) * insertLength);
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
            dataPage.PageHeader.HighestSlotId.ShouldBe(index);

            var before = dataPage.PageHeader.UsedBytes;
            var unaligned = dataPage.PageHeader.UnalignedFreeBytes;
            unaligned.ShouldBe((ushort)0);
            // delete entry 3
            var dirEntryPosition = SlotEntry.GetEntryPosition(3);
            var dirEntry = SlotEntry.FromBuffer(bufferSegment.Span, dirEntryPosition);
            dirEntry.DataLength.ShouldBe((ushort)insertLength);
            dataPage.Delete(3);
            // data should be gone
            var data = bufferSegment.Span.Slice(dirEntry.DataOffset, dirEntry.DataLength);
            data.ToArray().ShouldAllBe(b => b == 0x00);
            dataPage.PageHeader.UsedBytes.ShouldBe((ushort)(before - insertLength));

            // delete entry 7
            dirEntryPosition = SlotEntry.GetEntryPosition(7);
            dirEntry = SlotEntry.FromBuffer(bufferSegment.Span, dirEntryPosition);
            dirEntry.DataLength.ShouldBe((ushort)insertLength);
            dataPage.Delete(7);
            // data should be gone
            data = bufferSegment.Span.Slice(dirEntry.DataOffset, dirEntry.DataLength);
            data.ToArray().ShouldAllBe(b => b == 0x00);

            // delete entry 10
            dirEntryPosition = SlotEntry.GetEntryPosition(10);
            dirEntry = SlotEntry.FromBuffer(bufferSegment.Span, dirEntryPosition);
            dirEntry.DataLength.ShouldBe((ushort)insertLength);
            dataPage.Delete(10);
            // data should be gone
            data = bufferSegment.Span.Slice(dirEntry.DataOffset, dirEntry.DataLength);
            data.ToArray().ShouldAllBe(b => b == 0x00);
            dataPage.PageHeader.HighestSlotId.ShouldBe((byte)9);

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
            freeSpace.ShouldBe((ushort)(Constants.PAGE_SIZE - Constants.PAGE_HEADER_SIZE - 3000 - 3000 -
                                        2 * Constants.PAGE_DIRECTORY_ENTRY_SIZE));
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

            for (int i = 0; i < 202; i++)
            {
                if (!dataPage.IsInsertPossible(insertLength))
                    continue;
                var ins = dataPage.Insert(insertLength, out index);
                ins.WriteBytes(0, toInsert);
                dataPage.PageHeader.HighestSlotId.ShouldBe(index);
            }

            var freeSpaceExpected = (ushort) (Constants.PAGE_SIZE - Constants.PAGE_HEADER_SIZE -
                                              202 * Constants.PAGE_DIRECTORY_ENTRY_SIZE - 202 * 36);

            var freeSpace = dataPage.GetMaxContiguounesFreeSpace();
            freeSpace.ShouldBe(freeSpaceExpected);
            dataPage.Delete(10);
            freeSpace = dataPage.GetMaxContiguounesFreeSpace();
            freeSpace.ShouldBe(freeSpaceExpected);
            dataPage.Delete(11);
            freeSpace = dataPage.GetMaxContiguounesFreeSpace();
            freeSpace.ShouldBe((ushort)72);
            dataPage.Delete(12);
            freeSpace = dataPage.GetMaxContiguounesFreeSpace();
            freeSpace.ShouldBe((ushort)108);
        }

        [Fact]
        public void GetBytesByIndexTest()
        {
            using BufferSegment bufferSegment = new BufferSegment(Constants.PAGE_SIZE);
            PageHeader header = new PageHeader(1, PageType.Data);
            header.ToBuffer(bufferSegment.Span, 0);
            DataPage dataPage = new DataPage(bufferSegment);
            dataPage.IsFull.ShouldBeFalse();

            for (int i = 0; i < 10; i++)
            {
                byte[] toInsert = Encoding.UTF8.GetBytes($"Hello World {i + 1}");
                ushort insertLength = (ushort)toInsert.Length;
                dataPage.IsInsertPossible(insertLength).ShouldBeTrue();
                var ins = dataPage.Insert(insertLength, out var index);
                ins.WriteBytes(0, toInsert);
                dataPage.PageHeader.HighestSlotId.ShouldBe(index);
            }

            for (int i = 0; i < 10; i++)
            {
                var bytes = dataPage.GetDataByIndex((byte) (i + 1));
                var text = Encoding.UTF8.GetString(bytes);
                text.ShouldBe($"Hello World {i+1}");
            }
        }

        [Fact]
        public void DefragTest()
        {
            using BufferSegment bufferSegment = new BufferSegment(Constants.PAGE_SIZE);
            PageHeader header = new PageHeader(1, PageType.Data);
            header.ToBuffer(bufferSegment.Span, 0);
            DataPage dataPage = new DataPage(bufferSegment);
            dataPage.IsFull.ShouldBeFalse();

            byte index = 0;

            List<string> expectedData = new(Enumerable.Range(1, 20).Select(i => "H" + i));
            List<byte> indexes = new();

            for (int i = 1; i < 21; i++)
            {
                byte[] toInsert = Encoding.UTF8.GetBytes($"H{i}");
                ushort insertLength = (ushort)toInsert.Length;
                if (!dataPage.IsInsertPossible(insertLength))
                    continue;
                var ins = dataPage.Insert(insertLength, out index);
                ins.WriteBytes(0, toInsert);
                indexes.Add(index);
                dataPage.PageHeader.HighestSlotId.ShouldBe(index);
            }
            expectedData.Remove("H" + 10);
            expectedData.Remove("H" + 11);
            expectedData.Remove("H" + 12);
            expectedData.Remove("H" + 18);
            indexes.Remove(10);
            indexes.Remove(11);
            indexes.Remove(12);
            indexes.Remove(18);
            dataPage.Delete(10);
            dataPage.Delete(11);
            dataPage.Delete(12);
            dataPage.Delete(18);
            
            dataPage.Defrag();

            List<string> currentData = new();
            foreach (var ind in indexes)
            {
                currentData.Add(Encoding.UTF8.GetString(dataPage.GetDataByIndex(ind)));
            }

            dataPage.PageHeader.UnalignedFreeBytes.ShouldBe((ushort)0);
            currentData.ShouldBe(expectedData, true);
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
            SlotEntry directoryEntry = new SlotEntry(2032, 5000);
            directoryEntry.ToBuffer(bufferSegment.Span, SlotEntry.GetEntryPosition(2));
            header.ToBuffer(bufferSegment.Span);

            DataPage dataPage = new DataPage(bufferSegment);

            // Empty space should be between header and 2 entry
            var res = dataPage.FindFreeSpaceBetween(1000);
            res.Index1.ShouldBe((byte)0);
            res.Index2.ShouldBe((byte)2);


        }
    }
}
