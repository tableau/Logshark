using LogShark.Writers;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using LogShark.Shared;

namespace LogShark.Plugins
{
    public class PluginManager : IPluginManager
    {
        private const string AllMarker = "all";

        private readonly LogSharkConfiguration _config;
        private readonly PluginFactory _pluginFactory;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger _logger;

        private readonly List<IPlugin> _plugins;
        private bool _completeProcessingCalled;
        private bool _runAborted;

        public PluginManager(LogSharkConfiguration config, ILoggerFactory loggerFactory)
        {
            _config = config;
            _pluginFactory = new PluginFactory(_config);
            _loggerFactory = loggerFactory;
            _logger = loggerFactory.CreateLogger<PluginManager>();

            _plugins = new List<IPlugin>();
            _completeProcessingCalled = false;
            _runAborted = false;
        }

        public bool IsValidPluginConfiguration(out IEnumerable<string> badPluginNames)
        {
            var pluginNamesToLoad = GetPluginNamesToLoad();
            var knownPluginNames = GetKnownPluginNames().Select(name => name.ToLower()).ToHashSet();
            var badPluginNamesTemp = pluginNamesToLoad
                .Where(name => !knownPluginNames.Contains(name.ToLower()))
                .ToList();
            
            badPluginNames = badPluginNamesTemp;
            return !badPluginNames.Any();
        }

        public IEnumerable<IPlugin> CreatePlugins(IWriterFactory writerFactory, IProcessingNotificationsCollector processingNotificationsCollector)
        {
            var pluginNamesToLoad = GetPluginNamesToLoad().ToList();
            _logger.LogInformation($"Loading plugins: {string.Join(';', pluginNamesToLoad)}");

            foreach (var pluginName in pluginNamesToLoad)
            {
                IPlugin plugin;
                try
                {
                    plugin = _pluginFactory.CreatePlugin(pluginName);
                }
                catch (Exception ex)
                {
                   _logger.LogError($"Exception occurred when creating plugin '{pluginName}'. See inner exception for details.", ex);
                   throw;
                }

                try
                {
                    var pluginConfiguration = _config.GetPluginConfiguration(plugin.Name);
                    plugin.Configure(writerFactory, pluginConfiguration, processingNotificationsCollector, _loggerFactory);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Exception occurred when configuring plugin '{pluginName}'. See inner exception for details.");
                    throw;
                }
                _plugins.Add(plugin);
            }

            return _plugins;
        }

        public IEnumerable<IPlugin> GetPlugins()
        {
            return _plugins;
        }

        public IEnumerable<LogType> GetRequiredLogTypes()
        {
            return _plugins.SelectMany(p => p.ConsumedLogTypes).Distinct().ToList();
        }

        public PluginsExecutionResults SendCompleteProcessingSignalToPlugins(bool runAborted = false)
        {
            _completeProcessingCalled = true;
            var pluginsExecutionResults = new PluginsExecutionResults();
            
            if (runAborted)
            {
                _runAborted = true;
                return pluginsExecutionResults; // We don't want plugins to try to do any more work (and potentially make things worse) if we're aborting the run
            }

            foreach (var plugin in _plugins)
            {
                try
                {
                    var result = plugin.CompleteProcessing();
                    pluginsExecutionResults.AddSinglePluginResults(plugin.Name, result);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Exception occurred while {plugin.Name} plugin was completing processing");
                }
            }

            return pluginsExecutionResults;
        }

        private IEnumerable<string> GetPluginNamesToLoad()
        {
            var semicolonSeparatedPlugList = _config.RequestedPlugins;
            var defaultPluginsWereRequested = string.IsNullOrWhiteSpace(semicolonSeparatedPlugList) || semicolonSeparatedPlugList.ToLower() == AllMarker;

            IEnumerable<string> result;
            if (defaultPluginsWereRequested)
            {
                var knownPluginNames = GetKnownPluginNames();
                result =
                    (_config.PluginsToRunByDefault ?? knownPluginNames)
                    .Except(_config.PluginsToExcludeFromDefaultSet ?? Enumerable.Empty<string>())
                    .Distinct();
            }
            else
            {
                result = semicolonSeparatedPlugList
                    ?.Split(';', StringSplitOptions.RemoveEmptyEntries)
                    .Distinct();
            }

            return result;
        }

        public IEnumerable<string> GetKnownPluginNames()
        {
            return _pluginFactory.GetKnownPluginNames().OrderBy(x => x);
        }

        public void Dispose()
        {
            if (!_plugins.Any() || _runAborted) // If we aborting the run, writers and/or plugins could be in a bad shape and we don't want to make things worse by asking them to do more work
            {
                return;
            }

            if (!_completeProcessingCalled)
            {
                var message = 
                    $"Dispose method was called on {nameof(PluginManager)} before {nameof(SendCompleteProcessingSignalToPlugins)} method. " +
                    "This will cause some plugins to generate incomplete or corrupt result. Please contact LogShark developers if you ever see this message!";
                _logger.LogError(message);
                throw new Exception(message);
            }

            foreach (var plugin in _plugins)
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
    }
}
