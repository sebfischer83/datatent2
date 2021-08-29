using System;
using Datatent2.Core.Page.Table;
using Datatent2.Core.Services.Cache;
using Datatent2.Core.Services.Data;
using Datatent2.Core.Services.Page;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Datatent2.Contracts;
using Datatent2.Core.Index;
using Datatent2.Core.Index.Heap;
using Datatent2.Core.Page;
using Datatent2.Core.Services.Index;

namespace Datatent2.Core.Table
{
    public sealed partial class Table<T>: IAsyncDisposable where T : class
    {
        private uint _mainIndexPageAddress;

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

        private bool IsCreated()
        {
            var created = _tablePage.MainIndexPageAddress != 0;
#if DEBUG
            _logger.LogDebug($"Table seems to have already initialized: {created}");
#endif
            return created;
        }

        private async Task LoadPageDataOrGenerate()
        {
            var created = IsCreated();
            // marker bit, shows page has data
            if (created)
            {
                LoadPage();
            }
            else
            {
                await GeneratePage();
            }
        }

        private async Task GeneratePage()
        {
            var index = await Services.Index.IndexService.CreateIndex(_pageService, IndexType.Heap, _logger);
            _mainIndexPageAddress = index.PageIndex;
            WritePage();
        }

        private void LoadPage()
        {
            _mainIndexPageAddress = _tablePage.MainIndexPageAddress;
        }

        private void WritePage()
        {
            _tablePage.MainIndexPageAddress = _mainIndexPageAddress;
            _tablePage.Save();
        }

        internal static async Task<Table<T>> Get(string name, DataService dataService, PageService pageService, CacheService cacheService, ILogger<Table<T>> logger)
        {
            var tablePage = await pageService.GetTablePageForTable(name);
#if DEBUG
            logger.LogDebug($"Retrieved TablePage {tablePage.Id}");
#endif
            Table<T> table = new Table<T>(name, dataService, pageService, cacheService, tablePage, logger);
            await table.LoadPageDataOrGenerate();

            return table;
        }

        public ValueTask DisposeAsync()
        {
            WritePage();
            return ValueTask.CompletedTask;
        }
    }

    public record TableOptions
    {

    }
}
