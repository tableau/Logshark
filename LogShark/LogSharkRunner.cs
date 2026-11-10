using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using LogShark.Containers;
using LogShark.Extensions;
using LogShark.Metrics;
using LogShark.Plugins;
using LogShark.Shared.Extensions;
using LogShark.Shared.HostExtensions;
using LogShark.Shared.LogReading;
using LogShark.Writers;
using LogShark.Writers.Containers;
using LogShark.Writers.Csv;
using LogShark.Writers.Hyper;
using LogShark.Writers.Sql;
using LogShark.Writers.Sql.Models;
using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace LogShark
{
    public class LogSharkRunner
    {
        private readonly LogSharkConfiguration _config;

        private readonly PublisherSettings _publisherSettings;
        private readonly ProcessingNotificationsCollector _processingNotificationsCollector;
        private readonly MetricsModule _metricsModule;

        private readonly IArtifactPublisher _artifactPublisher;


        private readonly ILogger _logger;
        private readonly ILoggerFactory _loggerFactory;

        public LogSharkRunner(LogSharkConfiguration config, MetricsModule metricsModule, IArtifactPublisher artifactPublisher, ILoggerFactory loggerFactory)
        {
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
            _artifactPublisher = artifactPublisher ?? new NullArtifactPublisher();

            _loggerFactory = loggerFactory;
            _logger = loggerFactory.CreateLogger<LogSharkRunner>();

            _config = config;

            _processingNotificationsCollector = new ProcessingNotificationsCollector(_config.NumberOfErrorDetailsToKeep);

            if (_config.PublishWorkbooks)
            {
                _publisherSettings = config.GetPublisherSettings();
            }
            
            _metricsModule = metricsModule;
        }

        public async Task<RunSummary> Run(IWriterFactory customWriterFactory = null)
        {
            var startMetricsTask = _metricsModule.ReportStartMetrics(_config);
            var processLogTask = ProcessLog(customWriterFactory);
            await Task.WhenAll(startMetricsTask, processLogTask);

            var runSummary = await processLogTask;
            await _metricsModule.ReportEndMetrics(runSummary);

            return runSummary;
        }

        private async Task<RunSummary> ProcessLog(IWriterFactory customWriterFactory)
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
                return RunSummary.FailedRunSummary(runId, error, ExitReason.BadLogSet, _processingNotificationsCollector, totalElapsedTimer.Elapsed);
            }

            IWriterFactory writerFactory;
            try
            {
                writerFactory = customWriterFactory ?? GetWriterFactory(runId);
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("Context: 0x86a93465"))
                {
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        _logger.LogError("Microsoft Visual C++ Runtime required to run LogShark. Please download and install it from 'https://support.microsoft.com/en-us/help/2977003/the-latest-supported-visual-c-downloads' and rerun application");
                    }
                    else
                    {
                        _logger.LogError("Hyper failed to start. On macOS/Linux this is usually caused by the 'hyperd' binary missing execute permissions (run 'chmod +x' on hyperd and its shared library in the LogShark install directory) or by an incompatible CPU architecture. Check the hyperd log for more details");
                    }
                }

                var error = $"Exception occurred while initializing writer factory for writer '{_config.RequestedWriter ?? "null"}'. Exception message: {ex.Message}";
                _logger.LogError(ex, error);

                return RunSummary.FailedRunSummary(runId, error, ExitReason.UnclassifiedError, _processingNotificationsCollector, totalElapsedTimer.Elapsed);
            }
            
            using (_logger.BeginScope(runId))
            using (writerFactory)
            {
                ProcessLogSetResult logSetProcessingResults = null;

                // Wrap processing in Spectre.Console LiveDisplay if requested
                if (_config.ShowProgress)
                {
                    var renderer = new Progress.ConsoleProgressRenderer(Program.SpectreLoggerProvider);
                    var progressTracker = new Progress.ProgressTracker(renderer);

                    await AnsiConsole.Live(renderer.Render())
                        .AutoClear(false)
                        .StartAsync(async ctx =>
                        {
                            // Set up a background task to refresh the display periodically
                            var refreshTimer = new System.Threading.Timer(_ =>
                            {
                                ctx.UpdateTarget(renderer.Render());
                            }, null, TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(100));

                            try
                            {
                                using (var pluginManager = new PluginManager(_config, _loggerFactory))
                                {
                                    var logTypeDetails = new LogTypeDetails(_processingNotificationsCollector);
                                    using var tableauLogsReader = new TableauLogsProcessor(_config, pluginManager, writerFactory, logTypeDetails, _processingNotificationsCollector, _loggerFactory, progressTracker);
                                    logSetProcessingResults = await tableauLogsReader.ProcessLogSet();
                                }

                                progressTracker.Complete();
                                ctx.UpdateTarget(renderer.Render());
                            }
                            finally
                            {
                                refreshTimer?.Dispose();
                            }
                        });

                    // LiveDisplay has ended - flush logs and switch to direct output mode
                    Program.SpectreLoggerProvider?.OnLiveDisplayEnded();
                }
                else
                {
                    using (var pluginManager = new PluginManager(_config, _loggerFactory))
                    {
                        var logTypeDetails = new LogTypeDetails(_processingNotificationsCollector);
                        using var tableauLogsReader = new TableauLogsProcessor(_config, pluginManager, writerFactory, logTypeDetails, _processingNotificationsCollector, _loggerFactory);
                        logSetProcessingResults = await tableauLogsReader.ProcessLogSet();
                    }
                }

                if (logSetProcessingResults.IsSuccessful)
                {
                    var pluginsReceivedAnyDataForLogs = string.Join(", ",
                        logSetProcessingResults.PluginsReceivedAnyData.OrderBy(name => name));
                    _logger.LogInformation("Plugins that had any data sent to them: {pluginsReceivedAnyData}", pluginsReceivedAnyDataForLogs);
                }
                else
                {
                    _logger.LogDebug($"Log processing failed with error: {logSetProcessingResults.ErrorMessage}");
                    return RunSummary.FailedRunSummary(runId, logSetProcessingResults.ErrorMessage, logSetProcessingResults.ExitReason, _processingNotificationsCollector, totalElapsedTimer.Elapsed, logSetProcessingResults);
                }

                var workbookGenerator = writerFactory.GetWorkbookGenerator();
                var generatorResults = workbookGenerator.CompleteWorkbooksWithResults(logSetProcessingResults.PluginsExecutionResults.GetWritersStatistics());

                if (!GeneratedAnyWorkbooks(generatorResults.CompletedWorkbooks) && workbookGenerator.GeneratesWorkbooks)
                {
                    const string errorMessage = "No workbooks were generated successfully.";
                    _logger.LogError(errorMessage);
                    return RunSummary.FailedRunSummary(runId, errorMessage, ExitReason.UnclassifiedError, _processingNotificationsCollector, totalElapsedTimer.Elapsed, logSetProcessingResults, generatorResults);
                }

                PublisherResults publisherResults = null;
                ArtifactPublishResults artifactPublishResult = null;
                if (_config.PublishWorkbooks)
                {
                    if (_artifactPublisher.IsEnabled)
                    {
                        _logger.LogInformation("Publishing large workbooks to artifact store");
                        var artifactWorkbooks = generatorResults.CompletedWorkbooks
                            .Select(workbook => new ArtifactWorkbook(workbook.FinalWorkbookName, workbook.WorkbookPath, workbook.HasAnyData))
                            .ToList();
                        artifactPublishResult = await _artifactPublisher.PublishLargeWorkbooks(runId, artifactWorkbooks);
                    }
                    else
                    {
                        _logger.LogInformation("Skipping publishing to artifact store");
                    }

                    var workbookPublisher = writerFactory.GetWorkbookPublisher(_publisherSettings);
                    
                    publisherResults = await workbookPublisher.PublishWorkbooks(
                        runId,
                        generatorResults.CompletedWorkbooks,
                        logSetProcessingResults.PluginsExecutionResults.GetSortedTagsFromAllPlugins());
                    if (!publisherResults.CreatedProjectSuccessfully)
                    {
                        var errorMessage = $"Workbook publisher failed to connect to the Tableau Server or create project for the results. Exception message: {publisherResults.ExceptionCreatingProject.Message}";
                        _logger.LogError(errorMessage);
                        return RunSummary.FailedRunSummary(runId, errorMessage, ExitReason.UnclassifiedError, _processingNotificationsCollector, totalElapsedTimer.Elapsed, logSetProcessingResults, generatorResults, publisherResults);
                    }
                }

                _logger.LogInformation("Done processing {logSetPath}. Whole run took {fullRunElapsedTime}", _config.LogSetLocation, totalElapsedTimer.Elapsed);
                return RunSummary.SuccessfulRunSummary(runId, totalElapsedTimer.Elapsed, _processingNotificationsCollector, logSetProcessingResults, generatorResults, publisherResults, artifactPublishResult);
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
                    return new HyperWriterFactory(runId, _config, _loggerFactory, _processingNotificationsCollector);
                case "csv":
                    _logger.LogInformation("CSV Writer requested. Initializing writer factory...");
                    return new CsvWriterFactory(runId, _config, _loggerFactory);
                case "postgres":
                    _logger.LogInformation("Postgres Writer requested. Initializing writer factory...");
                    var logSharkRunRecord = new LogSharkRunModel
                    {
                        LogSetLocation = _config.LogSetLocation,
                        RunId = runId,
                        StartTimestamp = DateTime.UtcNow,
                    };
                    var sqlWriterFactory = new PostgresWriterFactory<LogSharkRunModel>(runId, _config, _loggerFactory, logSharkRunRecord, LogSharkRunModel.RunSummaryIdColumnName);
                    sqlWriterFactory.InitializeDatabase().Wait();
                    return sqlWriterFactory;
                default:
                    var message = $"{_config.RequestedWriter} is not an acceptable value for writer. To use default writer, skip specifying writer type completely.";
                    _logger.LogError(message);
                    throw new ArgumentException(message);
            }
        }

        private static bool GeneratedAnyWorkbooks(IEnumerable<CompletedWorkbookInfo> completedWorkbooks)
        {
            return completedWorkbooks?.Any(completedWorkbookInfo => completedWorkbookInfo.GeneratedSuccessfully) ?? false;
        }
    }
}