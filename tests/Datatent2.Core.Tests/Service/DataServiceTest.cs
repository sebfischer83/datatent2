using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Datatent2.Core.Memory;
using Datatent2.Core.Page;
using Datatent2.Core.Page.Header;
using Datatent2.Core.Services.Cache;
using Datatent2.Core.Services.Data;
using Datatent2.Core.Services.Disk;
using Datatent2.Core.Services.Page;
using Datatent2.Plugins.Compression;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Datatent2.Core.Tests.Service
{
    public class DataServiceTest
    {
        [Fact()]
        public async Task Insert_One_Object_Test()
        {
            var bogus = new Bogus.Randomizer();
            CacheService cacheService = new CacheService();
            PageService pageService = await PageService.Create(new InMemoryDiskService(), cacheService, NullLogger<PageService>.Instance);
            DataService dataService = new DataService(new NopCompressionService(), pageService, NullLogger<DataService>.Instance);

            TestObject testObject = new TestObject();
            testObject.IntProp = bogus.Int();
            testObject.StringProp = bogus.String2(1000);
            var address = await dataService.Insert(testObject);

            address.SlotId.ShouldBe((byte)1);
            address.PageId.ShouldBe((ushort)3);
        }

        [Fact]
        public async Task Insert_One_Object_And_Get_Test()
        {
            var bogus = new Bogus.Randomizer();
            CacheService cacheService = new CacheService();
            PageService pageService = await PageService.Create(new InMemoryDiskService(), cacheService, NullLogger<PageService>.Instance);
            DataService dataService = new DataService(new NopCompressionService(), pageService, NullLogger<DataService>.Instance);
            TestObject testObject = new TestObject();
            testObject.IntProp = bogus.Int();
            testObject.StringProp = bogus.String2(1000);
            var address = await dataService.Insert(testObject);

            address.SlotId.ShouldBe((byte)1);
            address.PageId.ShouldBe((ushort)3);
            var obj = await dataService.Get<TestObject>(address);
            obj.ShouldNotBeNull();
            obj.ShouldBe(testObject);
        }

        [Fact]
        public async Task Inster_Large_Object_And_Get_Test()
        {
            var bogus = new Bogus.Randomizer();
            CacheService cacheService = new CacheService();
            PageService pageService = await PageService.Create(new InMemoryDiskService(), cacheService, NullLogger<PageService>.Instance);
            DataService dataService = new DataService(new NopCompressionService(), pageService, NullLogger<DataService>.Instance);
            TestObject testObject = new TestObject();
            testObject.IntProp = bogus.Int();
            testObject.StringProp = bogus.String2(40000);
            var address = await dataService.Insert(testObject);

            address.SlotId.ShouldBe((byte)1);
            address.PageId.ShouldBe((ushort)3);
            var obj = await dataService.Get<TestObject>(address);
            obj.ShouldNotBeNull();
            obj.ShouldBe(testObject);
        }

        //[Fact]
        //public async Task SimpleInsertAndGetWithCheckpointTest()
        //{
        //    var bogus = new Bogus.Randomizer();
        //    using BufferSegment headerBufferSegment = new BufferSegment(Constants.PAGE_SIZE);
        //    var headerPage = HeaderPage.CreateHeaderPage(headerBufferSegment);
        //    using BufferSegment bufferSegment = new BufferSegment(Constants.PAGE_SIZE);
        //    PageHeader header = new PageHeader(1, PageType.Data);
        //    header.ToBuffer(bufferSegment.Span, 0);
        //    PageService pageService = new PageService(new InMemoryDiskService());
        //    DataService dataService = new DataService(new NopCompressionService(), pageService);
        //    TestObject testObject = new TestObject();
        //    testObject.IntProp = bogus.Int();
        //    testObject.StringProp = bogus.String2(1000);
        //    var address = await dataService.Insert(testObject);

        //    address.SlotId.ShouldBe((byte)1);
        //    address.PageId.ShouldBe((ushort)1);
        //    await pageService.CheckPoint();

        //    var obj = await dataService.Get<TestObject>(address);
        //    obj.ShouldNotBeNull();
        //    obj.ShouldBe(testObject);

        //    // there should be no second page
        //    var secondPage = await pageService.GetPage<BasePage>(2);
        //    secondPage.ShouldBeNull();
        //}

        //[Fact]
        //public async Task InsertMaxAmountOfEntriesPerPageTest()
        //{
        //    var bogus = new Bogus.Randomizer();
        //    using BufferSegment headerBufferSegment = new BufferSegment(Constants.PAGE_SIZE);
        //    var headerPage = HeaderPage.CreateHeaderPage(headerBufferSegment);
        //    using BufferSegment bufferSegment = new BufferSegment(Constants.PAGE_SIZE);
        //    PageHeader header = new PageHeader(1, PageType.Data);
        //    header.ToBuffer(bufferSegment.Span, 0);
        //    PageService pageService = new PageService(new InMemoryDiskService());
        //    DataService dataService = new DataService(new NopCompressionService(), pageService);

        //    for (int i = 0; i < 300; i++)
        //    {
        //        var address = await dataService.Insert("ABC");
        //        address.SlotId.ShouldBeGreaterThan((byte)0);
        //        address.PageId.ShouldBeGreaterThan((byte)0);

        //        if (i > 254)
        //            address.PageId.ShouldBeGreaterThan((uint)1);

        //        var resObject = await dataService.Get<string>(address);
        //        resObject.ShouldBe("ABC");
        //    }
        //}

        //[Fact]
        //public async Task LargeInsertTest()
        //{
        //    var bogus = new Bogus.Randomizer();
        //    using BufferSegment headerBufferSegment = new BufferSegment(Constants.PAGE_SIZE);
        //    var headerPage = HeaderPage.CreateHeaderPage(headerBufferSegment);
        //    using BufferSegment bufferSegment = new BufferSegment(Constants.PAGE_SIZE);
        //    PageHeader header = new PageHeader(1, PageType.Data);
        //    header.ToBuffer(bufferSegment.Span, 0);
        //    PageService pageService = new PageService(new InMemoryDiskService());
        //    DataService dataService = new DataService(new NopCompressionService(), pageService);
        //    TestObject testObject = new TestObject();
        //    testObject.IntProp = bogus.Int();
        //    testObject.StringProp = bogus.String2(1000);

        //    for (int i = 0; i < 10000; i++)
        //    {

        //        var address = await dataService.Insert(testObject);
        //        address.SlotId.ShouldBeGreaterThan((byte)0);
        //        address.PageId.ShouldBeGreaterThan((byte)0);

        //        if (i > 254)
        //            address.PageId.ShouldBeGreaterThan((uint)1);

        //        var resObject = await dataService.Get<TestObject>(address);
        //        resObject.ShouldBe(testObject);
        //    }
        //}

        //[Fact]
        //public async Task LargeInsertAndGetWithCheckpointTest()
        //{
        //    var bogus = new Bogus.Randomizer();
        //    using BufferSegment headerBufferSegment = new BufferSegment(Constants.PAGE_SIZE);
        //    var headerPage = HeaderPage.CreateHeaderPage(headerBufferSegment);
        //    using BufferSegment bufferSegment = new BufferSegment(Constants.PAGE_SIZE);
        //    PageHeader header = new PageHeader(1, PageType.Data);
        //    header.ToBuffer(bufferSegment.Span, 0);
        //    PageService pageService = new PageService(new InMemoryDiskService());
        //    DataService dataService = new DataService(new NopCompressionService(), pageService);


        //    for (int i = 0; i < 10000; i++)
        //    {
        //        TestObject testObject = new TestObject();
        //        testObject.IntProp = i;
        //        testObject.StringProp = $"Test{i}";
        //        var address = await dataService.Insert(testObject);
        //        address.SlotId.ShouldBeGreaterThan((byte)0);
        //        address.PageId.ShouldBeGreaterThan((byte)0);

        //        var resObject = await dataService.Get<TestObject>(address);
        //        resObject.ShouldBe(testObject);
        //    }

        //    for (int i = 0; i < 10000; i++)
        //    {
        //        TestObject testObject = new TestObject();
        //        testObject.IntProp = i;
        //        testObject.StringProp = $"Test{i}";
        //        var address = await dataService.Insert(testObject);
        //        address.SlotId.ShouldBeGreaterThan((byte)0);
        //        address.PageId.ShouldBeGreaterThan((byte)0);

        //        var resObject = await dataService.Get<TestObject>(address);
        //        resObject.ShouldBe(testObject);
        //    }
        //}

        public class TestObject
        {
            public int IntProp { get; set; }
            public string StringProp { get; set; }

            protected bool Equals(TestObject other)
            {
                return IntProp == other.IntProp && StringProp == other.StringProp;
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != this.GetType()) return false;
                return Equals((TestObject)obj);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(IntProp, StringProp);
            }
        }
    }
}
