using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Datatent2.Core.Memory;
using Datatent2.Core.Page.AllocationInformation;
using Datatent2.Core.Page.GlobalAllocationMap;
using Shouldly;
using Xunit;

namespace Datatent2.Core.Tests.Page
{
    public class AllocationInformationPageTest
    {
        [Fact]
        public void FindPageWithFreeSpaceTest()
        {
            IBufferSegment bufferSegment = new BufferSegment(Constants.PAGE_SIZE);
            AllocationInformationPage allocationInformationPage = new AllocationInformationPage(bufferSegment, 3 + 1016);

        }

        [Fact]
        public void IsAllocationInformationPageTest()
        {
            AllocationInformationPage.IsAllocationInformationPage(0).ShouldBeFalse();
            AllocationInformationPage.IsAllocationInformationPage(1).ShouldBeFalse();
            AllocationInformationPage.IsAllocationInformationPage(2).ShouldBeTrue();
            AllocationInformationPage.IsAllocationInformationPage(2 + GlobalAllocationMapPage.PAGES_PER_GAM + 1).ShouldBeTrue();
            AllocationInformationPage.IsAllocationInformationPage(2 + AllocationInformationPage.ENTRIES_PER_PAGE + 1).ShouldBeTrue();
            AllocationInformationPage.IsAllocationInformationPage(2 + AllocationInformationPage.ENTRIES_PER_PAGE * 2 + 2).ShouldBeTrue();
        }

        [Fact]
        public void GetNextAllocationInformationPageIdTest()
        {
            var next = AllocationInformationPage.GetNextAllocationInformationPageId(2);
            next.Id.ShouldBe((uint)(2 + AllocationInformationPage.ENTRIES_PER_PAGE));
            next.NewGAM.ShouldBeFalse();

            next = AllocationInformationPage.GetNextAllocationInformationPageId(2 + AllocationInformationPage.ENTRIES_PER_PAGE * 64);
            next.Id.ShouldBe((uint)(2 + AllocationInformationPage.ENTRIES_PER_PAGE * 63 + 1));
            next.NewGAM.ShouldBeTrue();
        }
    }
}
