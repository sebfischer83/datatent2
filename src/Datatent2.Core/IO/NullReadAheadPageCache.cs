using Datatent2.Core.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Datatent2.Core.IO
{
    internal class NullReadAheadPageCache : IReadAheadPageCache
    {
        public void Add(uint pageId, IBufferSegment segment)
        {
            
        }

        public bool Contains(uint pageId)
        {
            return false;
        }

        public IBufferSegment? GetIfExists(uint pageId)
        {
            return null;
        }

        public void Remove(uint pageId, bool free = false)
        {
           
        }
    }
}
