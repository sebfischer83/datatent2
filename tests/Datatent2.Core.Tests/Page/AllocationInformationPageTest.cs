using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Datatent2.Contracts;
using Datatent2.Core.Memory;
using Datatent2.Core.Page;
using Datatent2.Core.Page.AllocationInformation;
using Datatent2.Core.Page.GlobalAllocationMap;
using Moq;
using Shouldly;
using Xunit;

namespace Datatent2.Core.Tests.Page
{
    public class AllocationInformationPageTest
    {
        private IPage[] _pages;

        public AllocationInformationPageTest()
        {
            IBufferSegment segment = new BufferSegment(Constants.PAGE_SIZE);
            IPage[] pages = new IPage[AllocationInformationPage.ENTRIES_PER_PAGE];
            for (int i = 1; i < AllocationInformationPage.ENTRIES_PER_PAGE; i++)
            {
                var page = new DataPage(segment, (uint) (2 + i));
                pages[i] = (page);
            }

            _pages = pages;
        }

        [Fact]
        public void GetAllocationInformationEntryTest()
        {
            IBufferSegment bufferSegment = new BufferSegment(Constants.PAGE_SIZE);
            AllocationInformationPage allocationInformationPage = new AllocationInformationPage(bufferSegment, 2);
            var entry = allocationInformationPage.GetAllocationInformationEntry(2);
            entry.PageType.ShouldBe(PageType.AllocationInformation);
            entry.PageFillFactor.ShouldBe(PageFillFactor.Zero);
            allocationInformationPage.FillFactor.ShouldBe(PageFillFactor.ZeroToFifty);

            var page = new Mock<IPage>();
            page.Setup(basePage => basePage.FillFactor).Returns(PageFillFactor.NinetyFiveToNinetyNine);
            page.Setup(basePage => basePage.Id).Returns(3);
            page.Setup(basePage => basePage.Type).Returns(PageType.Data);
            allocationInformationPage.AddAllocationInformation(page.Object);

            entry = allocationInformationPage.GetAllocationInformationEntry(3);
            entry.PageType.ShouldBe(PageType.Data);
            entry.PageFillFactor.ShouldBe(PageFillFactor.NinetyFiveToNinetyNine);
        }

        [Fact]
        public void AddTest()
        {

            IBufferSegment bufferSegment = new BufferSegment(Constants.PAGE_SIZE);
            var allocationInformationPage = new AllocationInformationPage(bufferSegment, 2);
            for (int i = 1; i < AllocationInformationPage.ENTRIES_PER_PAGE; i++)
            {
                allocationInformationPage.AddAllocationInformation(_pages[i]);
            }
            allocationInformationPage.IsFull.ShouldBe(true);
        }

        [Fact]
        public void FindPageWithFreeSpaceTest()
        {
            IBufferSegment bufferSegment = new BufferSegment(Constants.PAGE_SIZE);
            AllocationInformationPage allocationInformationPage = new AllocationInformationPage(bufferSegment, 2);

            var page = new Mock<IPage>();
            page.Setup(basePage => basePage.FillFactor).Returns(PageFillFactor.NinetyFiveToNinetyNine);
            page.Setup(basePage => basePage.Id).Returns(3);
            page.Setup(basePage => basePage.Type).Returns(PageType.Data);
            allocationInformationPage.AddAllocationInformation(page.Object);

            page = new Mock<IPage>();
            page.Setup(basePage => basePage.FillFactor).Returns(PageFillFactor.ZeroToFifty);
            page.Setup(basePage => basePage.Id).Returns(4);
            page.Setup(basePage => basePage.Type).Returns(PageType.Data);
            allocationInformationPage.AddAllocationInformation(page.Object);

            var result = allocationInformationPage.FindPageWithFreeSpace(PageType.Data, PageFillFactor.SeventyToNinetyFive);
            result.ShouldBe(4);

            result = allocationInformationPage.FindPageWithFreeSpace(PageType.Data, PageFillFactor.Full);
            result.ShouldBe(3);
        }

        [Fact]
        public void FindPageWithFreeSpaceTest2()
        {
            IBufferSegment bufferSegment = new BufferSegment(Constants.PAGE_SIZE);
            AllocationInformationPage allocationInformationPage = new AllocationInformationPage(bufferSegment, 2);

            for (int i = 1; i < AllocationInformationPage.ENTRIES_PER_PAGE; i++)
            {
                var page = new Mock<IPage>();
                page.Setup(basePage => basePage.FillFactor).Returns(PageFillFactor.Full);
                page.Setup(basePage => basePage.Id).Returns((uint)(2 + i));
                page.Setup(basePage => basePage.Type).Returns(PageType.Data);
                allocationInformationPage.AddAllocationInformation(page.Object);
            }

            var result = allocationInformationPage.FindPageWithFreeSpace(PageType.Data, PageFillFactor.SeventyToNinetyFive);
            result.ShouldBe(-1);
            allocationInformationPage.IsFull.ShouldBe(true);
        }

        [Fact]
        public void IsAllocationInformationPageTest()
        {
            AllocationInformationPage.IsAllocationInformationPage(0).ShouldBeFalse();
            AllocationInformationPage.IsAllocationInformationPage(1).ShouldBeFalse();
            AllocationInformationPage.IsAllocationInformationPage(2).ShouldBeTrue();
            AllocationInformationPage.IsAllocationInformationPage(2 + AllocationInformationPage.ENTRIES_PER_PAGE).ShouldBeTrue();
            AllocationInformationPage.IsAllocationInformationPage(2 + GlobalAllocationMapPage.PAGES_PER_GAM + 1).ShouldBeTrue();
            AllocationInformationPage.IsAllocationInformationPage(2 + (GlobalAllocationMapPage.PAGES_PER_GAM * 2) + 2).ShouldBeTrue();
            AllocationInformationPage.IsAllocationInformationPage(2 + AllocationInformationPage.ENTRIES_PER_PAGE * 2).ShouldBeTrue();
            AllocationInformationPage.IsAllocationInformationPage(2 + GlobalAllocationMapPage.PAGES_PER_GAM + 1 + AllocationInformationPage.ENTRIES_PER_PAGE * 2).ShouldBeTrue();
        }

        [Fact]
        public void GetNextAllocationInformationPageIdTest()
        {
            var next = AllocationInformationPage.GetNextAllocationInformationPageId(2);
            next.Id.ShouldBe((uint)(2 + AllocationInformationPage.ENTRIES_PER_PAGE));
            next.NewGAM.ShouldBeFalse();

            next = AllocationInformationPage.GetNextAllocationInformationPageId(2 + AllocationInformationPage.ENTRIES_PER_PAGE);
            next.Id.ShouldBe((uint)(2 + AllocationInformationPage.ENTRIES_PER_PAGE * 2));
            next.NewGAM.ShouldBeFalse();

            next = AllocationInformationPage.GetNextAllocationInformationPageId(2 + AllocationInformationPage.ENTRIES_PER_PAGE * 63);
            next.Id.ShouldBe((uint)(2 + GlobalAllocationMapPage.PAGES_PER_GAM + 1));
            next.NewGAM.ShouldBeTrue();
        }
    }
}
