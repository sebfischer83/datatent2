using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Datatent2.Contracts;
using Datatent2.Core.Memory;
using Datatent2.Core.Page;
using Datatent2.Core.Page.Header;
using Datatent2.Core.Services.Cache;
using Datatent2.Core.Services.Disk;
using Datatent2.Core.Services.Page;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Datatent2.Core.Tests.Service
{
    public class PageServiceTest
    {
        [Fact()]
        public async void Subsequent_GetDataPageWithFreeSpace_Call_Test()
        {
            using BufferSegment headerBufferSegment = new BufferSegment(Constants.PAGE_SIZE);
            var headerPage = HeaderPage.CreateHeaderPage(headerBufferSegment);

            using BufferSegment bufferSegment = new BufferSegment(Constants.PAGE_SIZE);
            PageHeader header = new PageHeader(2, PageType.Data);
            header.ToBuffer(bufferSegment.Span, 0);
            CacheService cacheService = new CacheService();
            PageService pageService = await PageService.Create(new InMemoryDiskService(new DatatentSettings()), cacheService, NullLogger<PageService>.Instance);

            var dataPage = await pageService.GetDataPageWithFreeSpace();

            var dataPage2 = await pageService.GetDataPageWithFreeSpace();

            dataPage.Id.ShouldBe(dataPage2.Id);
        }
    }
}
