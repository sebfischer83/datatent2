// # SPDX-License-Identifier: MIT
// # Copyright 2021
// # Sebastian Fischer sebfischer@gmx.net

using System.Buffers;

namespace Datatent2.Core.Memory
{
    internal abstract class BufferPoolBase : MemoryPool<byte>
    {
        public abstract void Return(IBufferSegment segment);
        public abstract override IBufferSegment Rent(int minBufferSize = -1);
    }
}