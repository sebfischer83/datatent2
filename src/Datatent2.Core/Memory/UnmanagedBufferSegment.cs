// # SPDX-License-Identifier: MIT
// # Copyright 2021
// # Sebastian Fischer sebfischer@gmx.net

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Datatent2.Core.Memory
{
    /// <summary>
    /// An unmanaged buffer
    /// </summary>
    internal class UnmanagedBufferSegment : IBufferSegment
    {
        /// <summary>
        /// The key represents the position in the unmanaged memory block
        /// </summary>
        public int Key { get; }
        private readonly UnmanagedBufferPool _pool;
        private bool _disposed;

        /// <summary>
        /// Gets a raw pointer to the pool for this segment
        /// </summary>
        /// <returns></returns>
        public unsafe byte* GetPointer()
        {
            return (byte*)_pool.GetPointerToSlot(Key);
        }

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="memory"></param>
        /// <param name="key"></param>
        /// <param name="pool"></param>
        public UnmanagedBufferSegment(Memory<byte> memory, int key, UnmanagedBufferPool pool)
        {
            Memory = memory;
            Key = key;
            _pool = pool;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(UnmanagedBufferSegment));

            _pool.Return(this);
            _disposed = true;
        }

        /// <inheritdoc />
        public Memory<byte> Memory { get; }

        /// <inheritdoc />
        public void Clear()
        {
            Memory.Span.Clear();
        }

        /// <inheritdoc />
        public uint Length => (uint)Memory.Length;

        /// <inheritdoc />
        public Span<byte> Span => Memory.Span;
    }
}
