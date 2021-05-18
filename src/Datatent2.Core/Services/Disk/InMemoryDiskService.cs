// # SPDX-License-Identifier: MIT
// # Copyright 2021
// # Sebastian Fischer sebfischer@gmx.net

using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Datatent2.Core.Services.Disk
{
    internal class InMemoryDiskService : DiskService
    {
        public InMemoryDiskService(DatatentSettings settings) : base(settings, NullLogger.Instance)
        {
            Stream = new MemoryStream();
        }
    }
}
