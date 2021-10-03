// # SPDX-License-Identifier: MIT
// # Copyright 2021
// # Sebastian Fischer sebfischer@gmx.net

using System;
using System.Buffers;

namespace Datatent2.Core.Memory
{
    /// <summary>
    /// Buffer always a complete page, only dispose when not in use anymore
    /// </summary>
    internal class BufferSegment : IBufferSegment
    {
        private byte[]? _rental;

        /// <summary>
        /// The length of the buffer
        /// </summary>
        public uint Length => (uint)(_rental?.Length ?? 0);

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="minBufferSize"></param>
        public BufferSegment(int minBufferSize)
        {
            _rental = ArrayPool<byte>.Shared.Rent(minBufferSize);
        }

        /// <summary>
        /// Returns the buffer as a span
        /// </summary>
        public Span<byte> Span
        {
            get
            {
                if (_rental == null)
                    throw new ObjectDisposedException(nameof(BufferSegment));
                return new Span<byte>(_rental);
            }
        }

        /// <summary>
        /// Returns the buffer as a memory
        /// </summary>
        public Memory<byte> Memory
        {
            get
            {
                if (_rental == null)
                    throw new ObjectDisposedException(nameof(BufferSegment));

                return new Memory<byte>(_rental);
            }
        }

        /// <summary>
        /// Clears the buffer
        /// </summary>
        public void Clear()
        {
            Span.Clear();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_rental != null)
                ArrayPool<byte>.Shared.Return(_rental, true);
            _rental = null;
        }
    }
}
