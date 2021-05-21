// # SPDX-License-Identifier: MIT
// # Copyright 2021
// # Sebastian Fischer sebfischer@gmx.net

using Datatent2.Core.Memory;

namespace Datatent2.Core.IO
{
    internal interface IReadAheadPageCache
    {
        public IBufferSegment? GetIfExists(uint pageId);
        public void Add(uint pageId, IBufferSegment segment);
        public void Remove(uint pageId, bool free = false);
        public bool Contains(uint pageId);
    }
}
