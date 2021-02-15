using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Datatent2.Core.Memory
{
    internal class BufferPool : MemoryPool<byte>
    {
        public override int MaxBufferSize => Int32.MaxValue;

        public new static Impl Shared { get; } = new BufferPool.Impl();

        protected override void Dispose(bool disposing) { }

        public override BufferSegment Rent(int minBufferSize = -1) => RentCore(minBufferSize);

        private BufferSegment RentCore(int minBufferSize) => new BufferSegment(minBufferSize);

        public sealed class Impl : BufferPool
        {
            public new BufferSegment Rent(int minBufferSize) => RentCore(minBufferSize);
        }
    }
}
