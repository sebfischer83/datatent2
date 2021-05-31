using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Datatent2.Core.Services.Disk;
using Microsoft.Extensions.Logging;

namespace Datatent2.Core.Services.Index
{
    internal class IndexService
    {
        private readonly DiskService _diskService;
        private readonly ILogger _logger;

        public IndexService(DiskService diskService, ILogger logger)
        {
            _diskService = diskService;
            _logger = logger;
        }


    }
}
