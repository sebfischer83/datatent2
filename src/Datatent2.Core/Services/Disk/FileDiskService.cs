// # SPDX-License-Identifier: MIT
// # Copyright 2021
// # Sebastian Fischer sebfischer@gmx.net

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Datatent2.Contracts;
using Dawn;
using Microsoft.Extensions.Logging.Abstractions;

namespace Datatent2.Core.Services.Disk
{
    internal class FileDiskService : DiskService
    {
        public FileDiskService(DatatentSettings datatentSettings) : base(datatentSettings, NullLogger.Instance)
        {
            Stream = new FileStream(datatentSettings.DatabasePath!, FileMode.OpenOrCreate, FileAccess.ReadWrite,
                FileShare.Read, Constants.PAGE_SIZE,
                FileOptions.RandomAccess);
        }
    }
}
