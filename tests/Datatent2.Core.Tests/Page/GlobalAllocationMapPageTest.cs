﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Datatent2.Contracts;
using Datatent2.Contracts.Exceptions;
using Datatent2.Core.Memory;
using Datatent2.Core.Page.GlobalAllocationMap;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Datatent2.Core.Tests.Page
{
    public class GlobalAllocationMapPageTest
    {
        public GlobalAllocationMapPageTest()
        {
            BufferPoolFactory.Init(new DatatentSettings(), NullLogger.Instance);
        }

        [Fact]
        public void Is_Allocated_Test()
        {
            using IBufferSegment bufferSegment = new BufferSegment(Constants.PAGE_SIZE);
            var dataArea = bufferSegment.Span.Slice(Constants.PAGE_HEADER_SIZE);
            dataArea.Clear();
            GlobalAllocationMapPage globalAllocationMapPage = new GlobalAllocationMapPage(bufferSegment, 1);

            for (int i = 0; i < 15; i++)
            {
                var a = globalAllocationMapPage.AcquirePageId();
            }

            globalAllocationMapPage.IsAllocated(1).ShouldBeTrue();
            globalAllocationMapPage.IsAllocated(3).ShouldBeTrue();
            globalAllocationMapPage.IsAllocated(7).ShouldBeTrue();
            globalAllocationMapPage.IsAllocated(10).ShouldBeTrue();
            globalAllocationMapPage.IsAllocated(17).ShouldBeFalse();

            ShouldThrowExtensions.ShouldThrow<InvalidPageException>(() =>
            {
                globalAllocationMapPage.IsAllocated(0);
            });
        }

        [Fact]
        public void TestFindEmptyId()
        {
            using IBufferSegment bufferSegment = new BufferSegment(Constants.PAGE_SIZE);
            var dataArea = bufferSegment.Span.Slice(Constants.PAGE_HEADER_SIZE);
            dataArea.Clear();
            GlobalAllocationMapPage globalAllocationMapPage = new GlobalAllocationMapPage(bufferSegment);

            for (int i = 0; i < 1; i++)
            {
                dataArea.WriteByte(i, 0xFF);
            }
            globalAllocationMapPage.IsFull.ShouldBeFalse();
            var emptyId = globalAllocationMapPage.FindLocalEmptyPageId();
            emptyId.ShouldBe(9);

            for (int i = 0; i < 2; i++)
            {
                dataArea.WriteByte(i, 0xFF);
            }
            emptyId = globalAllocationMapPage.FindLocalEmptyPageId();
            emptyId.ShouldBe(17);

            for (int i = 0; i < 9; i++)
            {
                dataArea.WriteByte(i, 0xFF);
            }
            emptyId = globalAllocationMapPage.FindLocalEmptyPageId();
            emptyId.ShouldBe(73);

            for (int i = 0; i < 64; i++)
            {
                dataArea.WriteByte(i, 0xFF);
            }
            emptyId = globalAllocationMapPage.FindLocalEmptyPageId();
            emptyId.ShouldBe(513);
        }

        [Fact]
        public void TestFindEmptyId2()
        {
            using IBufferSegment bufferSegment = new BufferSegment(Constants.PAGE_SIZE);
            var dataArea = bufferSegment.Span.Slice(Constants.PAGE_HEADER_SIZE);
            dataArea.Clear();
            GlobalAllocationMapPage globalAllocationMapPage = new GlobalAllocationMapPage(bufferSegment);

            ref byte b = ref dataArea[0];
            b = (byte) (b | (1 << 0));
            var emptyId = globalAllocationMapPage.FindLocalEmptyPageId();
            emptyId.ShouldBe(2);

            b = (byte)(b | (1 << 1));
            emptyId = globalAllocationMapPage.FindLocalEmptyPageId();
            emptyId.ShouldBe(3);

            dataArea.WriteByte(0, 0xFF);
            b = ref dataArea[1];
            b = (byte)(b | (1 << 0));
            emptyId = globalAllocationMapPage.FindLocalEmptyPageId();
            emptyId.ShouldBe(10);

            for (int i = 0; i < 64; i++)
            {
                dataArea.WriteByte(i, 0xFF);
            }

            emptyId = globalAllocationMapPage.FindLocalEmptyPageId();
            emptyId.ShouldBe(513);

            b = ref dataArea[64];
            b = (byte)(b | (1 << 0));
            b = (byte)(b | (1 << 1));
            emptyId = globalAllocationMapPage.FindLocalEmptyPageId();
            emptyId.ShouldBe(515);
        }

        [Fact]
        public void AcquireIdTest()
        {
            using IBufferSegment bufferSegment = new BufferSegment(Constants.PAGE_SIZE);
            var dataArea = bufferSegment.Span.Slice(Constants.PAGE_HEADER_SIZE);
            GlobalAllocationMapPage globalAllocationMapPage = new GlobalAllocationMapPage(bufferSegment, 1);

            var id = globalAllocationMapPage.AcquirePageId();
            id.ShouldBe((uint)2);
            var nextId = (uint) globalAllocationMapPage.FindLocalEmptyPageId() + 1;
            nextId.ShouldBe<uint>(id + 1);

            id = globalAllocationMapPage.AcquirePageId();
            nextId = (uint)globalAllocationMapPage.FindLocalEmptyPageId() + 1;
            nextId.ShouldBe<uint>(id + 1);
        }

        [Fact]
        public void AcquireIdTestNotFirstGam()
        {
            using IBufferSegment bufferSegment = new BufferSegment(Constants.PAGE_SIZE);
            var dataArea = bufferSegment.Span.Slice(Constants.PAGE_HEADER_SIZE);
            GlobalAllocationMapPage globalAllocationMapPage = new GlobalAllocationMapPage(bufferSegment, 1 + GlobalAllocationMapPage.PAGES_PER_GAM + 1);

            var id = globalAllocationMapPage.AcquirePageId();
            var nextId = (uint)globalAllocationMapPage.FindLocalEmptyPageId() + 1 + GlobalAllocationMapPage.PAGES_PER_GAM + 1;
            nextId.ShouldBe<uint>(id + 1);

            id = globalAllocationMapPage.AcquirePageId();
            nextId = (uint)globalAllocationMapPage.FindLocalEmptyPageId() + 1 + GlobalAllocationMapPage.PAGES_PER_GAM + 1;
            nextId.ShouldBe<uint>(id + 1);
        }

        [Fact]
        public void AcquireIdCompleteGamTest()
        {
            using IBufferSegment bufferSegment = new BufferSegment(Constants.PAGE_SIZE);
            bufferSegment.Clear();
            var dataArea = bufferSegment.Span.Slice(Constants.PAGE_HEADER_SIZE);
            GlobalAllocationMapPage globalAllocationMapPage = new GlobalAllocationMapPage(bufferSegment, 1);
            uint id = 0u;
            List<uint> list = new List<uint>(GlobalAllocationMapPage.PAGES_PER_GAM);
            while (!globalAllocationMapPage.IsFull)
            {
                id = globalAllocationMapPage.AcquirePageId();
                list.Add(id);
                var nextId = globalAllocationMapPage.FindLocalEmptyPageId() + 1;
                if (nextId == 1841)
                    Debugger.Break();
                if (globalAllocationMapPage.IsFull)
                {
                    nextId.ShouldBe(0);
                    continue;
                }
             
                // works only for GAM 1
                ((uint)nextId).ShouldBe<uint>(id + 1);
            }

            var controllList = Enumerable.Range(2, GlobalAllocationMapPage.PAGES_PER_GAM).Select(i => (uint)i).ToList();
            list.ShouldBe(controllList);
        }
    }
}