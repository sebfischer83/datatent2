// # SPDX-License-Identifier: MIT
// # Copyright 2021
// # Sebastian Fischer sebfischer@gmx.net

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using ConsoleTableExt;
using Datatent2.Contracts;
using Datatent2.Contracts.Exceptions;
using Datatent2.Contracts.Scripting;
using Datatent2.Core.Memory;
using Datatent2.Core.Page;
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
        private List<AssemblyScanResult>? _availablePlugins;
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

            LogEnvironment();

            _cacheService = new CacheService();
        }

        private void LogEnvironment()
        {
            var infVersion = typeof(Datatent).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                ?.InformationalVersion;

            var infos = HardwareInformation.MachineInformationGatherer.GatherInformation(true);

            var tableData = new List<List<object>>()
            {
                new List<object>{"Assembly", typeof(Datatent).Assembly.GetName().Version!.ToString()},
                new List<object>{"Version", infVersion ?? "" },
                new List<object>{".Net Runtime", RuntimeInformation.FrameworkDescription },
                new List<object>{ "Operating System", RuntimeInformation.OSDescription },
                new List<object>{ "Architecture", RuntimeInformation.ProcessArchitecture.ToString()  },

                new List<object>{ "CPU", infos.Cpu.Name },
                new List<object>{ "CPU Features 1", infos.Cpu.FeatureFlagsOne },
                new List<object>{ "CPU Features 2", infos.Cpu.FeatureFlagsTwo },
            };

            var table = ConsoleTableBuilder
                .From(tableData)
                .WithColumn("Property", "Value").Export().ToString();

            _logger.LogInformation(table);
        }

        private async Task Init()
        {
            _logger.LogInformation(_datatentSettings.ToString());
            await LoadPlugins().ConfigureAwait(false);
            var compressionPlugins = await Task.WhenAll(
                _availablePlugins!.Select(async result =>
                {
                    if (result.ContractType == typeof(ICompressionService))
                    {
                        _logger.LogInformation($"Plugin {result.PluginType.Name} ({result.ContractType.Name}) found");

                        var x = await _loader!.LoadPlugin<ICompressionService>(result, configure: (context) =>
                        {
                            context.IgnorePlatformInconsistencies = true;
                        }).ConfigureAwait(false);
                        return x;
                    }

                    return null;
                })).ConfigureAwait(false);

            var scriptingEngines = await Task.WhenAll(
                _availablePlugins!.Select(async result =>
                {
                    if (result.ContractType == typeof(IMultithreadedScriptingEngine))
                    {
                        _logger.LogInformation($"Plugin {result.PluginType.Name} ({result.ContractType.Name}) found");

                        var x = await _loader!.LoadPlugin<IMultithreadedScriptingEngine>(result, configure: (context) =>
                        {
                            context.IgnorePlatformInconsistencies = true;
                            
                        }).ConfigureAwait(false);
                        return (IScriptingEngine) x;
                    }

                    if (result.ContractType == typeof(IScriptingEngine))
                    {
                        _logger.LogInformation($"Plugin {result.PluginType.Name} ({result.ContractType.Name}) found");

                        var x = await _loader!.LoadPlugin<IScriptingEngine>(result, configure: (context) =>
                        {
                            context.IgnorePlatformInconsistencies = true;

                        }).ConfigureAwait(false);
                        return x;
                    }

                    return null;
                })).ConfigureAwait(false);

            _transactionManager = new TransactionManager(_loggerFactory.CreateLogger("Transactions"));

            _pageService = await
                PageService.Create(_datatentSettings, DiskService.Create(_datatentSettings, _loggerFactory.CreateLogger("DiskService")), _cacheService,
                    _loggerFactory.CreateLogger<PageService>()).ConfigureAwait(false);

            var compressionService =
              compressionPlugins.FirstOrDefault(service => service?.Id == _datatentSettings.Plugins.CompressionAlgorithm);

            if (compressionService == null)
            {
                throw new InvalidEngineStateException($"Missing plugin with id {_datatentSettings.Plugins.CompressionAlgorithm} as compression service.");
            }

            _dataService = new DataService(compressionService!, _pageService, _transactionManager, _loggerFactory.CreateLogger<DataService>());
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
            _availablePlugins.AddRange((await _loader.FindPlugins<IScriptingEngine>(pathToDist).ConfigureAwait(false)).ToList());
            _availablePlugins.AddRange((await _loader.FindPlugins<IMultithreadedScriptingEngine>(pathToDist).ConfigureAwait(false)).ToList());
            _logger.LogInformation($"Found {_availablePlugins.Count} plugins");
        }

        public void GetAvailableScriptingEngines()
        {
            var scriptingEngines = _availablePlugins?.Where(t => t.ContractType.IsAssignableFrom(typeof(IScriptingEngine))).ToList();
            if (scriptingEngines != null)
            {
                //scriptingEngines[0]
            }
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
