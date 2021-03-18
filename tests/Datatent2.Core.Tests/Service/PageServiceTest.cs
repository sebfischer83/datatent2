using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Datatent2.Core.Memory;
using Datatent2.Core.Page;
using Datatent2.Core.Services.Cache;
using Datatent2.Core.Services.Disk;
using Datatent2.Core.Services.Page;
using Shouldly;
using Xunit;

namespace Datatent2.Core.Tests.Service
{
    public class PageServiceTest
    {
        [Fact]
        public async void GetFreeDataPageTest_NewPage()
        {
            using BufferSegment headerBufferSegment = new BufferSegment(Constants.PAGE_SIZE);
            var headerPage = HeaderPage.CreateHeaderPage(headerBufferSegment);

            using BufferSegment bufferSegment = new BufferSegment(Constants.PAGE_SIZE);
            PageHeader header = new PageHeader(0, PageType.Data);
            header.ToBuffer(bufferSegment.Span, 0);
            
            PageService pageService = new PageService(new InMemoryDiskService());
            await pageService.Init();

            var dataPage = await pageService.GetDataPageWithFreeSpace();

            var dataPage2 = await pageService.GetDataPageWithFreeSpace();

            dataPage.Id.ShouldBe(dataPage2.Id);
        }
    }
}
