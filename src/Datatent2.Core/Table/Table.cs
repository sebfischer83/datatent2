using Datatent2.Core.Page.Table;
using Datatent2.Core.Services.Cache;
using Datatent2.Core.Services.Data;
using Datatent2.Core.Services.Page;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace Datatent2.Core.Table
{
    public sealed partial class Table<T> where T : class
    {
        public string Name => _name;

        private readonly string _name;
        private readonly DataService _dataService;
        private readonly PageService _pageService;
        private readonly CacheService _cacheService;
        private readonly TablePage _tablePage;
        private readonly ILogger<Table<T>> _logger;

        internal Table(string name,
                           DataService dataService,
                           PageService pageService,
                           CacheService cacheService,
                           TablePage tablePage,
                           ILogger<Table<T>> logger)
        {
            _name = name;
            _dataService = dataService;
            _pageService = pageService;
            _cacheService = cacheService;
            _tablePage = tablePage;
            _logger = logger;
        }

        internal static async Task<Table<T>> GetAsync(string name, DataService dataService, PageService pageService, CacheService cacheService, ILogger<Table<T>> logger)
        {
            var tablePage = await pageService.GetTablePageForTable(name);
                        

            throw new System.Exception();
        }
    }
}
