// # SPDX-License-Identifier: MIT
// # Copyright 2021
// # Sebastian Fischer sebfischer@gmx.net

using System;
using System.Threading.Tasks;
using Datatent2.Core.Page;
using Datatent2.Core.Page.Index;
using Datatent2.Core.Services.Page;
using Microsoft.Extensions.Logging;

namespace Datatent2.Core.Index.Heap
{
    internal class HeapIndex : Index
    {
        internal HeapIndex(uint firstIndexPage, PageService pageService, ILogger logger) : base(firstIndexPage, pageService, logger)
        {
        }

        public override IndexType Type => IndexType.Heap;
        public override Task<PageAddress> Find<T>(T key)
        {
            throw new NotImplementedException();
        }

        public override Task Insert<T>(T key, PageAddress pageAddress)
        {
            // get first index page and look if there is enough space for this entry

            throw new NotImplementedException();
        }

        public override Task Delete<T>(T key)
        {
            throw new NotImplementedException();
        }
    }

    internal enum HeapKeyType : byte
    {
        Numeric = 1,
        UnsignedNumeric = 2,
        String = 2,
        Guid = 3
    }
}