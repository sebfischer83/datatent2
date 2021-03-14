using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Datatent2.Core.Memory;
using Shouldly;
using Xunit;

namespace Datatent2.Core.Tests.Memory
{
    public class UnmanagedBufferPoolTest
    {
        [Fact]
        public void RentAndReturnTest()
        {
            UnmanagedBufferPool unmanagedBufferPool = UnmanagedBufferPool.Shared;

            var rented = unmanagedBufferPool.Rent();
            rented.ShouldNotBeNull();
            rented.Length.ShouldBe((uint)Constants.PAGE_SIZE);
            rented.Dispose();
            
        }

        [Fact]
        public unsafe void GetPointerTest()
        {
            UnmanagedBufferPool unmanagedBufferPool = UnmanagedBufferPool.Shared;

            var rented = (UnmanagedBufferSegment) unmanagedBufferPool.Rent();
            var rented2 = (UnmanagedBufferSegment) unmanagedBufferPool.Rent();
            var ptr1 = unmanagedBufferPool.GetPointerToSlot(rented.Key);
            var ptr2 = unmanagedBufferPool.GetPointerToSlot(rented2.Key);
            ptr1.ShouldNotBe(IntPtr.Zero);
            ptr2.ShouldNotBe(IntPtr.Zero);
            
            Unsafe.InitBlock(ptr1.ToPointer(), 0x0F, Constants.PAGE_SIZE);
            Unsafe.InitBlock(ptr2.ToPointer(), 0xFF, Constants.PAGE_SIZE);
            rented.Span.ToArray().ShouldAllBe(b => b == 0x0F);
            rented2.Span.ToArray().ShouldAllBe(b => b == 0xFF);

            rented.Dispose();

        }
    }
}
