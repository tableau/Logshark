using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LogShark.Containers;
using LogShark.Extensions;
using LogShark.LogParser;
using LogShark.Metrics;
using LogShark.Plugins;
using LogShark.Writers;
using LogShark.Writers.Containers;
using LogShark.Writers.Csv;
using LogShark.Writers.Hyper;
using LogShark.Writers.Sql;
using Microsoft.Extensions.Logging;
using Tools.TableauServerRestApi.Containers;

namespace LogShark
{
    public class LogSharkRunner
    {
        private readonly LogTypeDetails _logTypeDetails;
        private readonly LogSharkConfiguration _config;

        private readonly PublisherSettings _publisherSettings;
        private readonly ProcessingNotificationsCollector _processingNotificationsCollector;
        private readonly MetricsModule _metricsModule;

        private readonly ILogger _logger;
        private readonly ILoggerFactory _loggerFactory;

        public LogSharkRunner(LogSharkConfiguration config, MetricsModule metricsModule, ILoggerFactory loggerFactory)
        {
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;

            _loggerFactory = loggerFactory;
            _logger = loggerFactory.CreateLogger<LogSharkRunner>();

            _config = config;

            _processingNotificationsCollector = new ProcessingNotificationsCollector(_config.NumberOfErrorDetailsToKeep);
            _logTypeDetails = new LogTypeDetails(_processingNotificationsCollector);

            if (_config.PublishWorkbooks)
            {
                var tableauServerInfo = new TableauServerInfo(
                    _config.TableauServerUrl,
                    _config.TableauServerSite,
                    _config.TableauServerUsername,
                    _config.TableauServerPassword,
                    _config.TableauServerTimeout);
                _publisherSettings = new PublisherSettings(
                    tableauServerInfo,
                    _config.GroupsToProvideWithDefaultPermissions,
                    _config.ApplyPluginProvidedTagsToWorkbooks,
                    _config.ParentProjectId,
                    _config.ParentProjectName);
            }
            _metricsModule = metricsModule;
        }

        public async Task<RunSummary> Run()
        {
            var startMetricsTask = _metricsModule.ReportStartMetrics(_config);
            var processLogTask = ProcessLog();
            await Task.WhenAll(startMetricsTask, processLogTask);

            var runSummary = await processLogTask;
            await _metricsModule.ReportEndMetrics(runSummary);

            return runSummary;
        }

        private async Task<RunSummary> ProcessLog()
        {
            _logger.LogInformation("Starting to process Log set {logSetPath}", _config.LogSetLocation);
            var totalElapsedTimer = Stopwatch.StartNew();

            if (_config.OutputDirMaxResultsToKeep != null)
            {
                OutputDirTrimmer.TrimOldResults(_config.OutputDir, _config.OutputDirMaxResultsToKeep.Value, _loggerFactory.CreateLogger(nameof(OutputDirTrimmer)));
            }

            var runId = GetRunId(_config.ForceRunId);

            var fileCheckResult = TableauLogsExtractor.FileCanBeOpened(_config.LogSetLocation, _logger); 
            if (!fileCheckResult.FileCanBeOpened)
            {
                var error = $"Path `{_config.LogSetLocation}` does not exist or LogShark cannot open it. Make sure `LogSetLocation` parameter points to the valid zip files with Tableau logs or unzipped copy of it. Exception message was: {fileCheckResult.ErrorMessage ?? "(null)"}";
                _logger.LogInformation(error);
                return new RunSummary(runId, totalElapsedTimer.Elapsed, _processingNotificationsCollector, null, null, null, error);
            }

            IWriterFactory writerFactory;
            try
            {
                writerFactory = GetWriterFactory(runId);
            }
            catch (Exception ex)
            {
                var error = $"Exception occurred while initializing writer factory for writer '{_config.RequestedWriter ?? "null"}'. Exception message: {ex.Message}";
                _logger.LogError(ex, error);
                return new RunSummary(runId, totalElapsedTimer.Elapsed, _processingNotificationsCollector, null, null, null, error);
            }
            
            using (_logger.BeginScope(runId))
            using (writerFactory)
            {
                var logReadingResults = ReadLogsAndGenerateDataSets(writerFactory);
                
                if (ProcessedAnyFiles(logReadingResults))
                {
                    var pluginsReceivedAnyDataForLogs = string.Join(", ", logReadingResults.PluginsReceivedAnyData.OrderBy(name => name));
                    _logger.LogInformation("Plugins that had any data sent to them: {pluginsReceivedAnyData}", pluginsReceivedAnyDataForLogs);
                }
                else if (logReadingResults.LoadedPlugins.Count > 0)
                {
                    var selectedPluginsPart = string.IsNullOrWhiteSpace(_config.RequestedPlugins)
                        ? "No plugins were explicitly requested, so LogShark ran with all plugins enabled."
                        : $"Plugins requested: `{_config.RequestedPlugins}`.";
                    var errorMessage = $"Did not find any Tableau log files associated with requested plugins in `{_config.LogSetLocation}`. " +
                                       $"{selectedPluginsPart} Please make sure that provided path is a zip file generated " +
                                       "by Tableau Server, or unzipped copy of the same file (with file structure preserved), or zip file with Desktop logs, " +
                                       "or crash package generated by tabcrashreporter";
                    _logger.LogInformation(errorMessage); // This is not really an error - just incorrect log set structure.
                    return new RunSummary(runId, totalElapsedTimer.Elapsed, _processingNotificationsCollector, logReadingResults, null, null, errorMessage);
                }
                else
                {
                    var errorMessage = "Did not find any plugins matching name(s) provided. Use \"LogShark --listplugins\" option to see available plugins. " +
                                       $"Plugins requested: {_config.RequestedPlugins}";
                    _logger.LogError(errorMessage);
                    return new RunSummary(runId, totalElapsedTimer.Elapsed, _processingNotificationsCollector, logReadingResults, null, null, errorMessage);
                }

                var workbookGenerator = writerFactory.GetWorkbookGenerator();
                var generatorResults = workbookGenerator.CompleteWorkbooksWithResults(logReadingResults.PluginsExecutionResults.GetWritersStatistics());

                if (!GeneratedAnyWorkbooks(generatorResults.CompletedWorkbooks) && workbookGenerator.GeneratesWorkbooks)
                {
                    const string errorMessage = "No workbooks were generated successfully.";
                    _logger.LogError(errorMessage);
                    return new RunSummary(runId, totalElapsedTimer.Elapsed, _processingNotificationsCollector, logReadingResults, generatorResults, null, errorMessage);
                }

                PublisherResults publisherResults = null;
                if (_config.PublishWorkbooks)
                {
                    var projectDescription = GetProjectDescription();
                    var workbookPublisher = writerFactory.GetWorkbookPublisher(_publisherSettings);
                    
                    publisherResults = await workbookPublisher.PublishWorkbooks(
                        runId,
                        projectDescription,
                        generatorResults.CompletedWorkbooks,
                        logReadingResults.PluginsExecutionResults.GetSortedTagsFromAllPlugins());
                    if (!publisherResults.CreatedProjectSuccessfully)
                    {
                        var errorMessage = $"Workbook publisher failed to connect to the Tableau Server or create project for the results. Exception message: {publisherResults.ExceptionCreatingProject.Message}";
                        _logger.LogError(errorMessage);
                        return new RunSummary(runId, totalElapsedTimer.Elapsed, _processingNotificationsCollector, logReadingResults, generatorResults, publisherResults, errorMessage);
                    }
                }

                _logger.LogInformation("Done processing {logSetPath}. Whole run took {fullRunElapsedTime}", _config.LogSetLocation, totalElapsedTimer.Elapsed);
                return new RunSummary(runId, totalElapsedTimer.Elapsed, _processingNotificationsCollector, logReadingResults, generatorResults, publisherResults);
            }
        }

        private string GetRunId(bool forceRunId)
        {
            var timestamp = DateTime.Now.ToString("yyMMddHHmmssff");
            
            if (!string.IsNullOrWhiteSpace(_config.UserProvidedRunId))
            {
                var runId = forceRunId 
                    ? _config.UserProvidedRunId 
                    : $"{timestamp}-{_config.UserProvidedRunId}"; 
                _logger.LogInformation("Using user-provided Run ID: {runId}", runId);
                return runId;
            }

            var fileName = Path.GetFileNameWithoutExtension(_config.LogSetLocation);
            var newRunId = $"{timestamp}-{Environment.MachineName}-{fileName}"
                .ToLower()
                .EnforceMaxLength(60);
            _logger.LogInformation("Generated Run ID: {runId}", newRunId);
            return newRunId;
        }
        
        private IWriterFactory GetWriterFactory(string runId)
        {
            switch (_config.RequestedWriter)
            {
                case null:
                case "":
                case "hyper":
                    _logger.LogInformation("Hyper Writer requested. Initializing writer factory...");
                    return new HyperWriterFactory(runId, _config, _loggerFactory);
                case "csv":
                    _logger.LogInformation("CSV Writer requested. Initializing writer factory...");
                    return new CsvWriterFactory(runId, _config, _loggerFactory);
                case "postgres":
                    _logger.LogInformation("Postgres Writer requested. Initializing writer factory...");
                    var sqlWriterFactory = new PostgresWriterFactory(runId, _config, _loggerFactory);
                    sqlWriterFactory.InitializeDatabase().Wait();
                    return sqlWriterFactory;
                default:
                    var message = $"{_config.RequestedWriter} is not an acceptable value for writer. To use default writer, skip specifying writer type completely.";
                    _logger.LogError(message);
                    throw new ArgumentException(message);
            }
        }

        private LogReadingResults ReadLogsAndGenerateDataSets(IWriterFactory writerFactory)
        {
            _logger.LogInformation("Using temp folder `{tempDir}`", _config.TempDir);
            
            using (var logsExtractor = new TableauLogsExtractor(_config.LogSetLocation, _config.TempDir, _processingNotificationsCollector, _loggerFactory.CreateLogger<TableauLogsExtractor>()))
            using (var pluginInitializer = new PluginInitializer(writerFactory, _config, _processingNotificationsCollector, _loggerFactory, _config.UsePluginsFromLogSharkAssembly))
            {
                var loadedPlugins = pluginInitializer.LoadedPlugins.Select(plugin => plugin.Name).ToHashSet();

                var requiredLogTypes = pluginInitializer.LoadedPlugins
                    .SelectMany(plugin => plugin.ConsumedLogTypes)
                    .Distinct()
                    .ToList();
                var processedLogTypes = string.Join(", ", requiredLogTypes.OrderBy(name => name));
                _logger.LogInformation("Based on requested plugins, the following log types will be processed: {processedLogTypes}", processedLogTypes);

                var logsReader = new TableauLogsReader(_loggerFactory.CreateLogger<TableauLogsReader>());

                var logProcessingErrors = new List<string>();
                var logProcessingStatistics = new Dictionary<LogType, LogProcessingStatistics>();
                var pluginsReceivedAnyData = new HashSet<string>();
                foreach (var logType in requiredLogTypes)
                {
                    _logger.LogInformation("Starting to process {logType} logs", logType);
                    
                    var logTypeInfo = _logTypeDetails.GetInfoForLogType(logType);
                    var applicablePlugins = pluginInitializer.LoadedPlugins
                        .Where(plugin => plugin.ConsumedLogTypes.Contains(logType))
                        .ToList();

                    try
                    {
                        var processingResult = logsReader.ProcessLogs(logsExtractor.LogSetParts, logTypeInfo, applicablePlugins);
                        logProcessingStatistics.Add(logType, processingResult);

                        _logger.LogInformation("Done processing {logType} logs. {processingResult}", logType, processingResult);

                        if (processingResult.FilesProcessed > 0)
                        {
                            foreach (var plugin in applicablePlugins)
                            {
                                pluginsReceivedAnyData.Add(plugin.Name);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        var errorMessage = $"Exception occurred while processing log type {logType}. Exception: {ex.Message}";
                        _logger.LogError(errorMessage);
                        logProcessingErrors.Add(errorMessage);
                    }
                }
                
                _logger.LogInformation("Telling all plugins to complete processing of any cached data");
                var pluginsExecutionResults = pluginInitializer.SendCompleteProcessingSignalToPlugins();
                _logger.LogInformation("Completed reading log set and generating data");

                return new LogReadingResults(
                    logProcessingErrors,
                    logsExtractor.LogSetSizeBytes,
                    logsExtractor.IsDirectory,
                    loadedPlugins,
                    logProcessingStatistics,
                    pluginsExecutionResults,
                    pluginsReceivedAnyData);
            }
        }

        private string GetProjectDescription()
        {
            var projectDescription = new StringBuilder();
            projectDescription.Append($"Generated from log set <b>'{_config.OriginalLocation}'</b> on {DateTime.Now:M/d/yy} by {Environment.UserName}.<br>");
            projectDescription.Append("<br>");
            projectDescription.Append($" Plugins Requested: <b>{_config.RequestedPlugins}</b>");
            return projectDescription.ToString();
        }

        private static bool ProcessedAnyFiles(LogReadingResults logReadingResults)
        {
            return logReadingResults.LogProcessingStatistics.Any(pair => pair.Value.FilesProcessed > 0);
        }

        private static bool GeneratedAnyWorkbooks(IEnumerable<CompletedWorkbookInfo> completedWorkbooks)
        {
            return completedWorkbooks?.Any(completedWorkbookInfo => completedWorkbookInfo.GeneratedSuccessfully) ?? false;
        }
    }
}