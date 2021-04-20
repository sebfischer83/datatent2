// # SPDX-License-Identifier: MIT
// # Copyright 2021
// # Sebastian Fischer sebfischer@gmx.net

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dawn;

namespace Datatent2.Core.Services.Disk
{
    internal class FileDiskService : DiskService
    {
        private readonly DatatentSettings _datatentSettings;

        public static FileDiskService Create(DatatentSettings settings)
        {
            Guard.Argument(settings.InMemory).False();
            Guard.Argument(settings.Path).NotWhiteSpace();
            FileStream fileStream = new FileStream(settings.Path!, FileMode.OpenOrCreate, FileAccess.ReadWrite,
                FileShare.Read, Constants.PAGE_SIZE,
                FileOptions.RandomAccess);

            FileDiskService diskService = new(fileStream, settings);
            return diskService;
        }

        protected FileDiskService(Stream stream, DatatentSettings datatentSettings) : base(stream)
        {
            _datatentSettings = datatentSettings;
        }
    }
}
