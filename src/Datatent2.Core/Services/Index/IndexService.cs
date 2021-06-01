using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Datatent2.Core.Services.Disk;
using Datatent2.Core.Services.Page;
using Microsoft.Extensions.Logging;

namespace Datatent2.Core.Services.Index
{
    internal class IndexService
    {
        private readonly PageService _pageService;
        private readonly ILogger _logger;

        public IndexService(PageService pageService, ILogger logger)
        {
            _pageService = pageService;
            _logger = logger;
        }


    }
}
