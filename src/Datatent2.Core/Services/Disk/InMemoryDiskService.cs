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
        public InMemoryDiskService() : base(new MemoryStream(new byte[1024 * 1024 * 100]))
        {
        }
    }
}
