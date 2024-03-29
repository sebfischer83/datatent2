﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Datatent2.Contracts.Exceptions;
using Datatent2.Core.Page;
using Datatent2.Core.Services.Index;
using Datatent2.Core.Services.Index.SkipList;
using Datatent2.Core.Services.Page;
using Datatent2.Core.Tests.Mocks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Datatent2.Core.Tests.Index.SkipList
{
    public class SkipListIndexServiceTest
    {
        private readonly Random _random = new Random();

        [Fact]
        public async Task Create_New_SkipList_Index()
        {
            IPageService pageService = new FakePageService();

            var index = await IndexService.CreateIndex(pageService, IndexType.SkipList, NullLogger.Instance);

            await index.Insert(5, new PageAddress(5, 5));

            await index.Insert(3, new PageAddress(3, 3));
            await index.Insert(7, new PageAddress(7, 7));
            await index.Insert(17, new PageAddress(17, 17));
            await index.Insert(27, new PageAddress(27, 27));
            await index.Insert(9, new PageAddress(9, 9));
            await index.Insert(91, new PageAddress(9, 9));
            await index.Insert(19, new PageAddress(9, 9));
            await index.Insert(329, new PageAddress(9, 9));
            await index.Insert(95, new PageAddress(9, 9));

            var s = await index.Print(new PrintStyle() { AttachIndexAddresses = true });
            var address = await index.Find<int>(329);
        }

        [Fact]
        public async Task Search_SkipList_Index()
        {
            IPageService pageService = new FakePageService();

            var index = await IndexService.CreateIndex(pageService, IndexType.SkipList, NullLogger.Instance);

            await index.Insert(5, new PageAddress(5, 5));

            await index.Insert(3, new PageAddress(3, 3));
            await index.Insert(7, new PageAddress(7, 7));
            await index.Insert(17, new PageAddress(17, 17));
            await index.Insert(27, new PageAddress(27, 27));
            await index.Insert(9, new PageAddress(9, 9));
            await index.Insert(9, new PageAddress(9, 9));

            var address = await index.Find<int>(7);
            address.ShouldNotBe(PageAddress.Empty);
            address = await index.Find<int>(43553553);
            address.ShouldBe(PageAddress.Empty);
        }

        [Fact]
        public async Task Large_Insert_Test()
        {
            Random _random = new Random();

            HashSet<int> toInsert = new HashSet<int>();
            int i = 0;
            while (i < 250)
            {
                var a = _random.Next();
                if (!toInsert.Contains(a))
                {
                    toInsert.Add(a);
                    i++;
                }
            }

            IPageService pageService = new FakePageService();

            var index = await IndexService.CreateIndex(pageService, IndexType.SkipList, NullLogger.Instance);

            foreach (var x in toInsert)
            {
                await index.Insert(x, PageAddress.Empty);
            }

            var sortedToInsert = toInsert.OrderBy(i1 => i1).ToList();
            var list = new List<int>();

            await foreach (var a in index.GetAll<int>())
            {
                list.Add(a.Key);
            }

            sortedToInsert.ShouldBeInOrder();
            list.ShouldBeInOrder();
            list.SequenceEqual(sortedToInsert).ShouldBe(true);
        }

        [Fact]
        public async Task Large_Insert_Search_Test()
        {
            Random _random = new Random();

            HashSet<int> toInsert = new HashSet<int>();
            int i = 0;
            while (i < 25000)
            {
                var a = _random.Next();
                if (!toInsert.Contains(a))
                {
                    toInsert.Add(a);
                    i++;
                }
            }

            IPageService pageService = new FakePageService();

            var index = await IndexService.CreateIndex(pageService, IndexType.SkipList, NullLogger.Instance);

            foreach (var x in toInsert)
            {
                await index.Insert(x, PageAddress.Empty);
            }

            var sortedToInsert = toInsert.OrderBy(i1 => i1).ToList();
            var last = sortedToInsert[^1];

            var s = await index.Find(last);
        }

        [Fact]
        public void TestCoinFlip()
        {
            List<int> list = new();
            for (int i = 0; i < 1000; i++)
            {
                var level = CoinFlip(0.5f, 16);
                level.ShouldBeGreaterThanOrEqualTo(0);
                level.ShouldBeLessThanOrEqualTo(16);
                list.Add(level);
            }

            var query = (from i in list
                group i by i
                into g
                orderby g.Count() descending
                select new { Key = g.Key, Count = g.Count() });
        }

        // coin flip algo from SkipListIndexService because it's private
        private int CoinFlip(float probability, int maxLevel)
        {
            float r = (float)_random.Next() / int.MaxValue;
            int lvl = 0;

            while (r < probability && lvl < maxLevel)
            {
                lvl++;
                r = (float)_random.Next() / int.MaxValue;
            }

            return lvl;
        }
    }
}
