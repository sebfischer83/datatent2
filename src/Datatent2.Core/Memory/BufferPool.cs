using System;
using System.Buffers;

namespace Datatent2.Core.Memory
{
    internal class BufferPool : MemoryPool<byte>
    {
        public override int MaxBufferSize => Int32.MaxValue;

        public new static Impl Shared { get; } = new();

        protected override void Dispose(bool disposing) { }

        public override BufferSegment Rent(int minBufferSize = -1) => RentCore(minBufferSize);

        private BufferSegment RentCore(int minBufferSize) => new(minBufferSize);

        public sealed class Impl : BufferPool
        {
            public new BufferSegment Rent(int minBufferSize) => RentCore(minBufferSize);
        }
    }
}
