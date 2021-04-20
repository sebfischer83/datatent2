// # SPDX-License-Identifier: MIT
// # Copyright 2021
// # Sebastian Fischer sebfischer@gmx.net

using System;
using System.Buffers;

namespace Datatent2.Core.Memory
{
    internal class BufferPool : BufferPoolBase
    {
        public override int MaxBufferSize => Int32.MaxValue;
        public override void Return(IBufferSegment segment)
        {
            segment.Dispose();
        }

        public new static Impl Shared { get; } = new();

        protected override void Dispose(bool disposing) { }

        public override IBufferSegment Rent(int minBufferSize = -1) => RentCore(minBufferSize);

        private IBufferSegment RentCore(int minBufferSize) => new BufferSegment(minBufferSize);

        public sealed class Impl : BufferPool
        {
            public new IBufferSegment Rent(int minBufferSize) => RentCore(minBufferSize);
        }
    }
}
