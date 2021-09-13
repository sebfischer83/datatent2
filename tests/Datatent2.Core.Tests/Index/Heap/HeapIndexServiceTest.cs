using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Datatent2.Core.Page;
using Datatent2.Core.Services.Index;
using Datatent2.Core.Services.Page;
using Datatent2.Core.Tests.Mocks;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Datatent2.Core.Tests.Index.Heap
{
    public class HeapIndexServiceTest
    {
        [Fact]
        public async Task HeapIndex_Create_Test()
        {
            IPageService pageService = new FakePageService();

            var index = await IndexService.CreateIndex(pageService, IndexType.Heap, NullLogger.Instance);

            await index.Insert(5, new PageAddress(5, 5));
            await index.Insert(87, new PageAddress(5, 5));
            await index.Insert(1, new PageAddress(5, 5));

            await index.Find(1);
        }
    }
}
