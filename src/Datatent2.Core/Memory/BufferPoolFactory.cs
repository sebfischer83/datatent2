using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Datatent2.Core.Memory
{
    internal static class BufferPoolFactory
    {
        public static BufferPoolBase Get()
        {
            if (Constants.BUFFER_POOL_IMPLEMENTATION == BufferPoolImplementation.Unmanaged)
                return UnmanagedBufferPool.Shared;
            if (Constants.BUFFER_POOL_IMPLEMENTATION == BufferPoolImplementation.Managed)
                return BufferPool.Shared;
        }
    }
}
