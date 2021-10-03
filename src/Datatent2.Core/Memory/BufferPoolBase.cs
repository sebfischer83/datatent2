// # SPDX-License-Identifier: MIT
// # Copyright 2021
// # Sebastian Fischer sebfischer@gmx.net

using System.Buffers;

namespace Datatent2.Core.Memory
{
    /// <summary>
    /// The buffer pool base class
    /// </summary>
    internal abstract class BufferPoolBase : MemoryPool<byte>
    {
        /// <summary>
        /// Return a buffer to the pool
        /// </summary>
        /// <param name="segment"></param>
        public abstract void Return(IBufferSegment segment);

        /// <summary>
        /// Rent a buffer from  the pool
        /// </summary>
        /// <param name="minBufferSize"></param>
        /// <returns></returns>
        public abstract override IBufferSegment Rent(int minBufferSize = -1);
    }
}