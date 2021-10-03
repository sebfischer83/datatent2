// # SPDX-License-Identifier: MIT
// # Copyright 2021
// # Sebastian Fischer sebfischer@gmx.net

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Datatent2.Contracts;
using Datatent2.Core.Memory;
using Datatent2.Core.Services.Cache;
using Datatent2.Core.Services.Data;
using Datatent2.Core.Services.Disk;
using Datatent2.Core.Services.Page;
using Datatent2.Core.Services.Transactions;
using Datatent2.Core.Table;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Prise;
using Prise.DependencyInjection;
using Prise.Utils;

namespace Datatent2.Core
{
    public class Datatent : IAsyncDisposable
    {
        private readonly DatatentSettings _datatentSettings;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger<Datatent> _logger;
        private IPluginLoader? _loader;
        private IList<AssemblyScanResult>? _availablePlugins;
        private PageService? _pageService;
        private DataService? _dataService;
        private CacheService _cacheService;
        private TransactionManager _transactionManager;

        public Datatent(DatatentSettings datatentSettings, ILoggerFactory loggerFactory)
        {
            BufferPoolFactory.Init(datatentSettings, loggerFactory.CreateLogger("BufferPool"));
            _datatentSettings = datatentSettings;
            _loggerFactory = loggerFactory;
            _availablePlugins = null;
            _logger = loggerFactory.CreateLogger<Datatent>();
            _logger.LogInformation(Environment.NewLine + Figgle.FiggleFonts.Epic.Render("Datatent2"));
            _logger.LogInformation(typeof(Datatent).Assembly.GetName().Version!.ToString());
            var infVersion = typeof(Datatent).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                ?.InformationalVersion;
            if (!string.IsNullOrWhiteSpace(infVersion))
                _logger.LogInformation(infVersion);
            _cacheService = new CacheService();

            _logger.LogInformation($".Net {Environment.Version}");
            _logger.LogInformation($"64Bit {Environment.Is64BitOperatingSystem} {Environment.Is64BitProcess}");
            _logger.LogInformation($"Operating System {Environment.OSVersion}");
        }

        private async Task Init()
        {
            _logger.LogInformation(_datatentSettings.ToString());
            await LoadPlugins().ConfigureAwait(false);
            var compressionPlugins = await Task.WhenAll(
                _availablePlugins!.Select(async result =>
                {
                    _logger.LogInformation($"Plugin {result.PluginType.Name} ({result.ContractType.Name}) found");
                    if (result.ContractType == typeof(ICompressionService))
                    {
                        var x = await _loader!.LoadPlugin<ICompressionService>(result, configure: (context) =>
                        {
                            context.IgnorePlatformInconsistencies = true;
                        }).ConfigureAwait(false);
                        return x;
                    }

                    return null;
                })).ConfigureAwait(false);

            var nopCompressionService =
                compressionPlugins.First(service => service?.Id == Constants.NopCompressionPluginId);

            _transactionManager = new TransactionManager(_loggerFactory.CreateLogger("Transactions"));

            _pageService = await
                PageService.Create(DiskService.Create(_datatentSettings, _loggerFactory.CreateLogger("DiskService")), _cacheService,
                    _loggerFactory.CreateLogger<PageService>()).ConfigureAwait(false);

            _dataService = new DataService(nopCompressionService!, _pageService, _transactionManager, _loggerFactory.CreateLogger<DataService>());
        }

        private async Task LoadPlugins()
        {
            var mainServiceCollection = new ServiceCollection()
                .AddSingleton<IConfiguration>(new ConfigurationBuilder()
                    .Build())
                .AddPriseNugetPackages();

            var serviceProvider = mainServiceCollection.BuildServiceProvider();

            var pathToDist = _datatentSettings.PluginPath;

            var hostFramework = HostFrameworkUtils.GetHostframeworkFromType(typeof(Datatent));
            _logger.LogInformation($"Search plugins at {pathToDist}");

            _loader = serviceProvider.GetRequiredService<IPluginLoader>();

            _availablePlugins = (await _loader.FindPlugins<ICompressionService>(pathToDist).ConfigureAwait(false)).ToList();
            _logger.LogInformation($"Found {_availablePlugins.Count} plugins");
        }

        public static async Task<Datatent> Create(DatatentSettings datatentSettings, ILoggerFactory loggerFactory)
        {
            var datatent = new Datatent(datatentSettings, loggerFactory);
            await datatent.Init().ConfigureAwait(false);

            return datatent;
        }

        public async Task<Table<TValue, TKey>> GetTable<TValue, TKey>(string name) where TValue : class
        {
            var table = await Table<TValue, TKey>.Get(name, _dataService!, _pageService!, _cacheService, _loggerFactory.CreateLogger("Table")).ConfigureAwait(false);

            return table;
        }

        public void GetDatabaseLayout()
        {

        }

        /// <summary>
        /// Disposes the instance.
        /// </summary>
        /// <returns>A ValueTask.</returns>
        public async ValueTask DisposeAsync()
        {
            if (_pageService != null)
                await (_pageService.CheckPoint()).ConfigureAwait(false);
        }
    }
}
