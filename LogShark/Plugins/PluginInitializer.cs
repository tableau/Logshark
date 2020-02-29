using LogShark.Writers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using LogShark.Containers;
using LogShark.Extensions;

namespace LogShark.Plugins
{
    public class PluginInitializer : IDisposable
    {
        public IList<IPlugin> LoadedPlugins { get; }

        private const string AllMarker = "all";
        private readonly ILogger _logger;
        private bool _completeProcessingCalled;

        public PluginInitializer(
            IWriterFactory writerFactory,
            LogSharkConfiguration config,
            IProcessingNotificationsCollector processingNotificationsCollector,
            ILoggerFactory loggerFactory,
            bool usePluginsFromLogSharkAssembly = true)
        {
            _logger = loggerFactory.CreateLogger<PluginInitializer>();
            LoadedPlugins = new List<IPlugin>();
            _completeProcessingCalled = false;
            
            var semicolonSeparatedPlugList = config.RequestedPlugins;
            _logger.LogInformation("Starting to load plugins for string: {pluginsRequestedString}", semicolonSeparatedPlugList ?? "(null)");
            var getAll = string.IsNullOrWhiteSpace(semicolonSeparatedPlugList) || semicolonSeparatedPlugList.ToLower() == AllMarker;
            var requestedPluginNames = semicolonSeparatedPlugList?.Split(';', StringSplitOptions.RemoveEmptyEntries).Distinct().ToHashSet();
            
            foreach (var plugin in GetPlugins(getAll, requestedPluginNames, config.PluginsToRunByDefault, config.PluginsToExcludeFromDefaultSet, usePluginsFromLogSharkAssembly))
            {
                try
                {
                    plugin.Configure(writerFactory, config.GetPluginConfiguration(plugin.Name), processingNotificationsCollector, loggerFactory);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Exception occurred while configuring plugin {pluginFailedToInitialize}", plugin.Name);
                    throw;
                }
                LoadedPlugins.Add(plugin);
            }

            var loadedPluginNames = LoadedPlugins
                .Select(plugin => plugin.Name)
                .OrderBy(pluginName => pluginName);
            _logger.LogInformation("{numberOfLoadedPlugins} plugins loaded: {loadedPlugins}", LoadedPlugins.Count, string.Join(", ", loadedPluginNames));
        }

        public PluginsExecutionResults SendCompleteProcessingSignalToPlugins()
        {
            _completeProcessingCalled = true;
            var pluginsExecutionResults = new PluginsExecutionResults();

            foreach (var plugin in LoadedPlugins)
            {
                try
                {
                    var result = plugin.CompleteProcessing();
                    pluginsExecutionResults.AddSinglePluginResults(plugin.Name, result);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Exception occurred while {pluginFailedToCompleteProcessing} plugin was completing processing", plugin.Name);
                }
            }

            return pluginsExecutionResults;
        }

        public void Dispose()
        {
            if (!_completeProcessingCalled)
            {
                var message = $"Dispose method was called on {nameof(PluginInitializer)} before {nameof(SendCompleteProcessingSignalToPlugins)} method. " +
                              "This will cause some plugins to generate incomplete or corrupt result. Please contact LogShark developers if you ever see this message!";
                _logger.LogError(message);
                throw new Exception(message);
            }

            foreach (var plugin in LoadedPlugins)
            {
                try
                {
                    plugin.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Exception occurred while disposing plugin {pluginFailedToDispose}", plugin.Name);
                }
            }
        }
        
        public static IEnumerable<string> GetAllAvailablePluginNames(bool usePluginsFromLogSharkAssembly = true)
        {
            return GetKnownPlugins(usePluginsFromLogSharkAssembly)
                .Select(pluginKvp => pluginKvp.Key)
                .OrderBy(key => key);
        }

        private static IEnumerable<IPlugin> GetPlugins(
            bool defaultPluginsWereRequested,
            ICollection<string> explicitlyRequestedPlugins,
            ICollection<string> defaultSet,
            ICollection<string> excludedPlugins,
            bool usePluginsFromLogSharkAssembly)
        {
            var knownPlugins = GetKnownPlugins(usePluginsFromLogSharkAssembly);
            IEnumerable<string> pluginNamesToLoad;
            if (defaultPluginsWereRequested)
            {
                pluginNamesToLoad = 
                    (defaultSet ?? knownPlugins.Keys)
                    .Except(excludedPlugins ?? Enumerable.Empty<string>());
            }
            else
            {
                pluginNamesToLoad = explicitlyRequestedPlugins;
            }

            var results = new List<IPlugin>();
            foreach (var pluginName in pluginNamesToLoad)
            {
                try
                {
                    if (knownPlugins.ContainsKey(pluginName))
                    {
                        var pluginType = knownPlugins[pluginName];
                        var plugin = Activator.CreateInstance(pluginType) as IPlugin;
                        results.Add(plugin);
                    }
                    else
                    {
                        throw new Exception($"Requested plugin `{pluginName}` has not been loaded.");
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception($"Exception occurred while loading plugin {pluginName}. See inner exception for details.", ex);
                }
            }

            return results;
        }

        private static Dictionary<string, Type> GetKnownPlugins(bool usePluginsFromLogSharkAssembly)
        {
            var knownPlugins = new Dictionary<string, Type>();
            var assembly = usePluginsFromLogSharkAssembly
                ? Assembly.GetExecutingAssembly()
                : Assembly.GetEntryAssembly();
            foreach (var typeInfo in assembly?.DefinedTypes ?? new List<TypeInfo>())
            {
                if (!typeInfo.ImplementedInterfaces.Contains(typeof(IPlugin)))
                {
                    continue;
                }

                var typeNameMinusPlugin = typeInfo.Name.EndsWith("Plugin")
                    ? typeInfo.Name.Substring(0, typeInfo.Name.LastIndexOf("Plugin", StringComparison.Ordinal))
                    : typeInfo.Name;

                knownPlugins[typeNameMinusPlugin] = typeInfo.AsType();
            }
            return knownPlugins;
        }
    }
}