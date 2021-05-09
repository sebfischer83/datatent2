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

namespace Datatent2.Core.Services.Disk
{
    internal class FileDiskService : DiskService
    {
        private readonly DatatentSettings _datatentSettings;

        public FileDiskService(Stream stream, DatatentSettings datatentSettings) : base(stream)
        {
            _datatentSettings = datatentSettings;
        }
    }
}
