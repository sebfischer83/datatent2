// # SPDX-License-Identifier: MIT
// # Copyright 2021
// # Sebastian Fischer sebfischer@gmx.net

using System;
using System.Buffers;

namespace Datatent2.Core.Memory
{
    /// <summary>
    /// The buffer that is returned by the pool
    /// </summary>
    internal interface IBufferSegment : IMemoryOwner<byte>
    {
        /// <summary>
        /// Clears the buffer
        /// </summary>
        public void Clear();

        /// <summary>
        /// The length of the buffer
        /// </summary>
        public uint Length { get; }

        /// <summary>
        /// The buffer as a span
        /// </summary>
        public Span<byte> Span { get; }
    }
}