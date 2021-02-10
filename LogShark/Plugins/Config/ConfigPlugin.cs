using System;
using System.Collections.Generic;
using System.Linq;
using LogShark.Containers;
using LogShark.Plugins.Config.Models;
using LogShark.Shared;
using LogShark.Shared.LogReading.Containers;
using LogShark.Writers;
using LogShark.Writers.Containers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LogShark.Plugins.Config
{
    public class ConfigPlugin : IPlugin
    {
        private static readonly DataSetInfo ConfigEntriesOutputInfo = new DataSetInfo("Config", "ConfigEntries");
        private static readonly DataSetInfo ProcessTopologyOutputInfo = new DataSetInfo("Config", "ProcessTopology");

        private static readonly List<LogType> ConsumedLogTypesStatic = new List<LogType> {LogType.TabsvcYml, LogType.WorkgroupYml};

        public IList<LogType> ConsumedLogTypes => ConsumedLogTypesStatic;
        public string Name => "Config";

        private IWriter<ConfigEntry> _configEntriesWriter;
        private IWriter<ConfigProcessInfo> _processTopologyWriter;
        private IList<ConfigFile> _processedConfigFiles;
        private ILogger _logger;
        private IProcessingNotificationsCollector _processingNotificationsCollector;

        public void Configure(IWriterFactory writerFactory, IConfiguration pluginConfig, IProcessingNotificationsCollector processingNotificationsCollector, ILoggerFactory loggerFactory)
        {
            _configEntriesWriter = writerFactory.GetWriter<ConfigEntry>(ConfigEntriesOutputInfo);
            _processTopologyWriter = writerFactory.GetWriter<ConfigProcessInfo>(ProcessTopologyOutputInfo);
            _processedConfigFiles = new List<ConfigFile>();
            _logger = loggerFactory.CreateLogger<ConfigPlugin>();
            _processingNotificationsCollector = processingNotificationsCollector;
        }

        // This plugin is a little special, as it does part of its work at dispose.
        // It writes all config entries as it encounters them, but also keeps cache of all processed config files inside Dictionary. This works fine because config files are fairly small.
        // Then on Dispose it analyzes collected files to generate and write Process Topology information
        public void ProcessLogLine(LogLine logLine, LogType logType)
        {
            if (logLine.LineContents is Dictionary<string, string> configContents)
            {
                var configFile = new ConfigFile(logLine.LogFileInfo, configContents, logType);
                var configEntriesWritten = WriteConfigFileEntries(configFile);
                
                if (configEntriesWritten > 0)
                {
                    _processedConfigFiles.Add(configFile);
                    _logger.LogInformation("{configEntriesWritten} config entries processed and written", configEntriesWritten);
                }
            }
            else
            {
                const string error = "Failed to interpret input as Dictionary<string, string>. Either log set contains empty/corrupt yaml config files or incorrect log reader is used for the plugin";
                _processingNotificationsCollector.ReportError(error, logLine, nameof(ConfigPlugin));
            }
        }

        public SinglePluginExecutionResults CompleteProcessing()
        {
            var allTags = new List<string>();
            try
            {
                var processTopologyRecordsGenerated = GenerateAndWriteProcessTopology();
                _logger.LogInformation("{processTopologyEntriesWritten} process topology entries generated and written", processTopologyRecordsGenerated);

                var buildTags = ExtractConfigValuesIntoTags("version.rstr");
                var versionTags = ExtractConfigValuesIntoTags("version.external");
                allTags = buildTags.Concat(versionTags).ToList();
            }
            catch (Exception ex)
            {
                var error =
                    "Uncaught exception occurred while processing topology entries. Results might be incomplete or empty. " +
                    "This most likely means that configuration yml files (workgroup.yml, tabsvc.yml) were corrupt or in a wrong format. " +
                    $"Exception message: {ex.Message}";
                _processingNotificationsCollector.ReportError(error, nameof(ConfigPlugin));
            }
            
            var writersLineCounts = new List<WriterLineCounts>
            {
                _configEntriesWriter.Close(),
                _processTopologyWriter.Close()
            };
            
            return new SinglePluginExecutionResults(writersLineCounts, allTags);
        }

        public void Dispose()
        {
            _configEntriesWriter?.Dispose();
            _processTopologyWriter?.Dispose();
        }
        
        private int WriteConfigFileEntries(ConfigFile configFile)
        {
            var configEntries = configFile.Values
                .Select(pair =>
                {
                    var (configKey, configValue) = pair;
                    var firstDot = configKey.IndexOf('.');
                    var rootKey = configKey.Substring(0, firstDot > 0 ? firstDot : configKey.Length);
                    return new ConfigEntry(configFile.LogFileInfo, configKey, rootKey, configValue);
                })
                .ToList();
            
            _configEntriesWriter.AddLines(configEntries);
            return configEntries.Count;
        }

        private int GenerateAndWriteProcessTopology()
        {
            _logger.LogInformation("Starting to process configuration data received");

            if (_processedConfigFiles.Count == 0)
            {
                // This is not really a "bad" condition, i.e. Desktop logs don't have config data
                _logger.LogInformation("{pluginName} was not able to process any configuration data. Either config files are missing or incorrect/unknown file format", nameof(ConfigPlugin));
                return 0;
            }

            // All workgroup.yml and tabsvc.yml files are supposed to be the same, so ANY would do
            var workgroupYmlFile = _processedConfigFiles.FirstOrDefault(config => config.LogType == LogType.WorkgroupYml);
            var tabsvcYmlFile = _processedConfigFiles.FirstOrDefault(config => config.LogType == LogType.TabsvcYml);

            var processInfoParser = new ProcessInfoExtractor(workgroupYmlFile, tabsvcYmlFile, _processingNotificationsCollector);
            var processInfoRecords = processInfoParser.GenerateProcessInfoRecords();
            _processTopologyWriter.AddLines(processInfoRecords);
            _logger.LogInformation($"{processInfoRecords.Count} process info records processed and written");
            return processInfoRecords.Count;
        }

        private IEnumerable<string> ExtractConfigValuesIntoTags(string configKey)
        {
            return _processedConfigFiles
                .Where(configFile => configFile.Values.ContainsKey(configKey))
                .Select(configFile => configFile.Values[configKey])
                .Distinct();
        }
    }
}