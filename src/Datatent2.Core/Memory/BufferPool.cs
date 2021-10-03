// # SPDX-License-Identifier: MIT
// # Copyright 2021
// # Sebastian Fischer sebfischer@gmx.net

using System;
using System.Buffers;

namespace Datatent2.Core.Memory
{
    /// <summary>
    /// A managed memory buffer pool implementation
    /// </summary>
    internal class BufferPool : BufferPoolBase
    {
        /// <inheritdoc />
        public override int MaxBufferSize => Int32.MaxValue;
        /// <inheritdoc />
        public override void Return(IBufferSegment segment)
        {
            segment.Dispose();
        }

        /// <summary>
        /// The shared instance
        /// </summary>
        public new static Impl Shared { get; } = new();

        /// <inheritdoc />
        protected override void Dispose(bool disposing) { }

        /// <inheritdoc />
        public override IBufferSegment Rent(int minBufferSize = -1) => RentCore(minBufferSize);

        /// <summary>
        /// Rent the buffer from the pool
        /// </summary>
        /// <param name="minBufferSize"></param>
        /// <returns></returns>
        private IBufferSegment RentCore(int minBufferSize) => new BufferSegment(minBufferSize);

        /// <summary>
        /// The implementation
        /// </summary>
        public sealed class Impl : BufferPool
        {
            /// <summary>
            /// Rent the buffer
            /// </summary>
            /// <param name="minBufferSize"></param>
            /// <returns></returns>
            public new IBufferSegment Rent(int minBufferSize) => RentCore(minBufferSize);
        }
    }
}
