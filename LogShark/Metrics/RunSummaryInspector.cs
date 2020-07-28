using LogShark.Containers;
using LogShark.Metrics.Models;
using LogShark.Writers.Containers;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LogShark.Metrics
{
    public class RunSummaryInspector : Inspector
    {
        private readonly TelemetryLevel _telemetryLevel;
        private RunSummary _runSummary;

        public RunSummaryInspector(ILoggerFactory loggerFactory, TelemetryLevel telemetryLevel)
        {
            _logger = loggerFactory.CreateLogger<RunSummaryInspector>();
            _telemetryLevel = telemetryLevel;
        }

        public void Parse(RunSummary runSummary)
        {
            _runSummary = runSummary;
        }

        public IEnumerable<EndMetrics.ContextModel.CompletedWorkbookModel> GetCompletedWorkbooks()
        {
            return GetMetric(() =>
            {
                var completedWorkbooksInfo = _runSummary?.WorkbookGeneratorResults?.CompletedWorkbooks ?? Enumerable.Empty<CompletedWorkbookInfo>();
                var completedWorkbooks = completedWorkbooksInfo.Select(cw => new EndMetrics.ContextModel.CompletedWorkbookModel()
                {
                    ExceptionMessage = cw.Exception?.Message,
                    FinalWorkbookName = cw.FinalWorkbookName,
                    GeneratedSuccessfully = cw.GeneratedSuccessfully,
                    HasAnyData = cw.HasAnyData,
                    OriginalWorkbookName = cw.OriginalWorkbookName,
                }).ToList();
                return completedWorkbooks;
            });
        }

        public TimeSpan GetElapsed()
        {
            return GetMetric(() => _runSummary.Elapsed);
        }

        public long? GetFullLogSetSizeBytes()
        {
            return GetMetric(() => _runSummary.ProcessLogSetResult?.FullLogSetSizeBytes);
        }

        public bool GetIsSuccess()
        {
            return GetMetric(() => _runSummary.IsSuccess);
        }

        public IEnumerable<EndMetrics.ContextModel.LogProcessingStatistic> GetLogProcessingStatistics()
        {
            return GetMetric(() =>
            {
                var logProcessingStatistics = _runSummary.ProcessLogSetResult?.LogProcessingStatistics?.Select(lps => new EndMetrics.ContextModel.LogProcessingStatistic()
                {
                    Elapsed = lps.Value.Elapsed,
                    FilesProcessed = lps.Value.FilesProcessed,
                    FilesSizeBytes = lps.Value.FilesSizeBytes,
                    LinesProcessed = lps.Value.LinesProcessed,
                    LogType = lps.Key.ToString(),
                }).ToList();
                return logProcessingStatistics;
            });
        }

        public string GetLogReadingError()
        {
            return GetMetric(() => _runSummary.ProcessLogSetResult?.ErrorMessage);
        }
        
        public ExitReason? GetLogReadingExitReason()
        {
            return GetMetric(() => _runSummary.ProcessLogSetResult?.ExitReason);
        }

        public IEnumerable<EndMetrics.ContextModel.PluginModel> GetLoadedPlugins()
        {
            return GetMetric(() =>
            {
                var loadedPluginsData = _runSummary.ProcessLogSetResult?.LoadedPlugins ?? Enumerable.Empty<string>();
                var loadedPlugins = loadedPluginsData.Select(plugin => new EndMetrics.ContextModel.PluginModel()
                {
                    PluginName = plugin,
                    ReceivedData = _runSummary.ProcessLogSetResult.PluginsReceivedAnyData.Contains(plugin),
                }).ToList();
                return loadedPlugins;
            });
        }

        public IEnumerable<EndMetrics.ContextModel.ProcessingNotification> GetProcessingErrors()
        {
            return GetMetric(() =>
            {
                var processingErrors = _runSummary.ProcessingNotificationsCollector.ProcessingErrorsDetails.Select(pe => new EndMetrics.ContextModel.ProcessingNotification()
                {
                    Message = pe.Message,
                    FilePath = pe.FilePath,
                    LineNumber = pe.LineNumber,
                    ReportedBy = pe.ReportedBy,
                }).ToList();
                return processingErrors;
            });
        }

        public IEnumerable<EndMetrics.ContextModel.ProcessingNotificationByReporter> GetProcessingErrorsByReporter()
        {
            return GetMetric(() =>
            {
                var processingErrorsByReporter = _runSummary.ProcessingNotificationsCollector.ErrorCountByReporter.Select(ec => new EndMetrics.ContextModel.ProcessingNotificationByReporter()
                {
                    ProcessingNotificationCount = ec.Value,
                    ReportedBy = ec.Key,
                }).ToList();
                return processingErrorsByReporter;
            });
        }

        public int GetProcessingErrorsCount()
        {
            return GetMetric(() => _runSummary.ProcessingNotificationsCollector.TotalErrorsReported);
        }

        public IEnumerable<EndMetrics.ContextModel.ProcessingNotification> GetProcessingWarnings()
        {
            return GetMetric(() =>
            {
                var processingWarnings = _runSummary.ProcessingNotificationsCollector.ProcessingWarningsDetails.Select(pe => new EndMetrics.ContextModel.ProcessingNotification()
                {
                    Message = pe.Message,
                    FilePath = pe.FilePath,
                    LineNumber = pe.LineNumber,
                    ReportedBy = pe.ReportedBy,
                }).ToList();
                return processingWarnings;
            });
        }

        public IEnumerable<EndMetrics.ContextModel.ProcessingNotificationByReporter> GetProcessingWarningsByReporter()
        {
            return GetMetric(() =>
            {
                var processingWarningsByReporter = _runSummary.ProcessingNotificationsCollector.WarningCountByReporter.Select(ec => new EndMetrics.ContextModel.ProcessingNotificationByReporter()
                {
                    ProcessingNotificationCount = ec.Value,
                    ReportedBy = ec.Key,
                }).ToList();
                return processingWarningsByReporter;
            });
        }

        public int GetProcessingWarningsCount()
        {
            return GetMetric(() => _runSummary.ProcessingNotificationsCollector.TotalWarningsReported);
        }

        public bool? GetPublisherCreatedProjectSuccessfully()
        {
            return GetMetric(() => _telemetryLevel == TelemetryLevel.Full 
                ? _runSummary.PublisherResults?.CreatedProjectSuccessfully 
                : null);
        }

        public string GetPublisherCreatedProjectExceptionMessage()
        {
            return GetMetric(() => _telemetryLevel == TelemetryLevel.Full
                ? _runSummary.PublisherResults?.ExceptionCreatingProject?.Message
                : null);
        }

        public string GetPublisherProjectName()
        {
            return GetMetric(() => _telemetryLevel == TelemetryLevel.Full
                ? _runSummary.PublisherResults?.ProjectName
                : null);
        }

        public IEnumerable<EndMetrics.ContextModel.PublishedWorkbookModel> GetPublishedWorkbooks()
        {
            return GetMetric(() =>
            {
                if (_telemetryLevel == TelemetryLevel.Full)
                {
                    var publishedWorkbooksInfo = _runSummary.PublisherResults?.PublishedWorkbooksInfo ?? Enumerable.Empty<WorkbookPublishResult>();
                    var publishedWorkbooks = publishedWorkbooksInfo.Select(wpr => new EndMetrics.ContextModel.PublishedWorkbookModel()
                    {
                        ExceptionMessage = wpr.Exception?.Message,
                        PublishedSuccessfully = wpr.PublishedSuccessfully,
                        WorkbookName = wpr.PublishedWorkbookName,
                    }).ToList();
                    return publishedWorkbooks;
                }
                return null;
            });
        }

        public string GetReasonForFailure()
        {
            return GetMetric(() => _runSummary.ReasonForFailure);
        }

        public string GetRunId()
        {
            return GetMetric(() => _telemetryLevel == TelemetryLevel.Full 
                ? _runSummary.RunId 
                : null);
        }

        public IEnumerable<EndMetrics.ContextModel.WriterStatisticModel> GetWriterStatistics()
        {
            return GetMetric(() =>
            {
                if (_telemetryLevel == TelemetryLevel.Full)
                {
                    var dataSets = _runSummary.ProcessLogSetResult?.PluginsExecutionResults?.GetWritersStatistics()?.DataSets;
                    if (dataSets == null)
                    {
                        return Enumerable.Empty<EndMetrics.ContextModel.WriterStatisticModel>();
                    }
                    var writerStatistics = dataSets.Select(ds => new EndMetrics.ContextModel.WriterStatisticModel()
                    {
                        DataSetGroup = ds.Key.Group,
                        DataSetName = ds.Key.Name,
                        LinesPersisted = ds.Value.LinesPersisted,
                        NullLinesIgnored = ds.Value.NullLinesIgnored,
                    }).ToList();
                    return writerStatistics;
                }
                return null;
            });
        }
    }
}
