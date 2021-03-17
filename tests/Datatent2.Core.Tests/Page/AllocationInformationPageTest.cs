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
            var p = Core.Page.GlobalAllocationMap.GlobalAllocationMapPage.PAGES_PER_GAM /
                    Core.Page.AllocationInformation.AllocationInformationPage.ENTRIES_PER_PAGE;
            uint[] pos = new uint[p];
            pos[0] = 3;
            for (int i = 1; i < p; i++)
            {
                pos[i] = (uint)((uint)(i * (uint)Core.Page.AllocationInformation.AllocationInformationPage.ENTRIES_PER_PAGE) + 2 + (i * 1));
            }

            AllocationInformationPage.IsAllocationInformationPage(1).ShouldBeFalse();
            AllocationInformationPage.IsAllocationInformationPage(2).ShouldBeFalse();
            AllocationInformationPage.IsAllocationInformationPage(3).ShouldBeTrue();
            AllocationInformationPage.IsAllocationInformationPage(3 + AllocationInformationPage.ENTRIES_PER_PAGE).ShouldBeTrue();
            AllocationInformationPage.IsAllocationInformationPage(3 + AllocationInformationPage.ENTRIES_PER_PAGE + 2).ShouldBeFalse();
            AllocationInformationPage.IsAllocationInformationPage(3 + GlobalAllocationMapPage.PAGES_PER_GAM).ShouldBeTrue();
            AllocationInformationPage.IsAllocationInformationPage(3 + ((AllocationInformationPage.ENTRIES_PER_PAGE * 2) + 1) + (GlobalAllocationMapPage.PAGES_PER_GAM * 2)).ShouldBeTrue();
        }

        [Fact]
        public void GetNextAllocationInformationPageIdTest()
        {
            var p = Core.Page.GlobalAllocationMap.GlobalAllocationMapPage.PAGES_PER_GAM /
                    Core.Page.AllocationInformation.AllocationInformationPage.ENTRIES_PER_PAGE;
            uint[] pos = new uint[p];
            pos[0] = 3;
            for (int i = 1; i < p; i++)
            {
                pos[i] = (uint)((uint)(i * (uint)Core.Page.AllocationInformation.AllocationInformationPage.ENTRIES_PER_PAGE) + 2 + (i * 1));
            }

            var next = AllocationInformationPage.GetNextAllocationInformationPageId(pos[0]);
            next.ShouldBe(pos[1]);

            next = AllocationInformationPage.GetNextAllocationInformationPageId(pos[5]);
            next.ShouldBe(pos[6]);

            next = AllocationInformationPage.GetNextAllocationInformationPageId(pos[5] + GlobalAllocationMapPage.PAGES_PER_GAM);
            next.ShouldBe(pos[6] + GlobalAllocationMapPage.PAGES_PER_GAM);

            next = AllocationInformationPage.GetNextAllocationInformationPageId(pos[^1] + GlobalAllocationMapPage.PAGES_PER_GAM);
            next.ShouldBe(pos[0] + GlobalAllocationMapPage.PAGES_PER_GAM * 2);
        }
    }
}
