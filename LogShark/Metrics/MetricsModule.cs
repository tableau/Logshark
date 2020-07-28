using System.Diagnostics;
using System.Threading.Tasks;
using LogShark.Containers;
using LogShark.Metrics.Models;
using Microsoft.Extensions.Logging;

namespace LogShark.Metrics
{
    public class MetricsModule
    {
        private readonly MetricsConfig _config;
        private readonly UserInspector _userInspector;
        private readonly ProcessInspector _processInspector;
        private readonly HardwareInspector _hardwareInspector;
        private readonly RunSummaryInspector _runSummaryInspector;

        public MetricsModule(MetricsConfig config, ILoggerFactory loggerFactory)
        {
            _config = config;
            
            if (IsUploadEnabled())
            {
                _userInspector = new UserInspector(loggerFactory, config.TelemetryLevel);
                _processInspector = new ProcessInspector(loggerFactory);
                _hardwareInspector = new HardwareInspector(loggerFactory);
                _runSummaryInspector = new RunSummaryInspector(loggerFactory, config.TelemetryLevel);
            }
        }

        public async Task ReportStartMetrics(LogSharkConfiguration config)
        {
            if (!IsUploadEnabled())
            {
                return;
            }

            var startMetrics = new StartMetrics
            {
                Context = new StartMetrics.ContextModel
                {
                    RequestedPlugins = config.RequestedPlugins,
                    RequestedWriter = config.RequestedWriter,
                    UserProvidedRunId = config.UserProvidedRunId,
                },
                System = new StartMetrics.SystemModel
                {
                    DebuggerIsAttached = Debugger.IsAttached,
                    DomainName = _userInspector.GetDomainName(),
                    LogSharkVersion = _processInspector.GetLogSharkVersion(),
                    MachineName = _userInspector.GetMachineName(),
                    OSArchitecture = _hardwareInspector.GetOSArchitecture(),
                    OSDescription = _hardwareInspector.GetOSDescription(),
                    OSVersion = _hardwareInspector.GetOSVersion(),
                    ProcessorCount = _hardwareInspector.GetProcessorCount(),
                    TelemetryLevel = _config.TelemetryLevel,
                    Username = _userInspector.GetUsername(),
                }
            };
            
            await _config.MetricUploader.Upload(startMetrics, "logshark.start");
        }

        public async Task ReportEndMetrics(RunSummary runSummary) 
        {
            if (!IsUploadEnabled())
            {
                return;
            }

            _runSummaryInspector.Parse(runSummary);
            var endMetrics = new EndMetrics
            {
                Context = new EndMetrics.ContextModel
                    {
                        CompletedWorkbooks = _runSummaryInspector.GetCompletedWorkbooks(),
                        Elapsed = _runSummaryInspector.GetElapsed(),
                        FullLogSetSizeBytes = _runSummaryInspector.GetFullLogSetSizeBytes(),
                        IsSuccess = _runSummaryInspector.GetIsSuccess(),
                        LogProcessingStatistics = _runSummaryInspector.GetLogProcessingStatistics(),
                        LogReadingError = _runSummaryInspector.GetLogReadingError(),
                        LogReadingExitReason = _runSummaryInspector.GetLogReadingExitReason(),
                        LoadedPlugins = _runSummaryInspector.GetLoadedPlugins(),
                        ProcessingErrors = _runSummaryInspector.GetProcessingErrors(),
                        ProcessingErrorsByReporter = _runSummaryInspector.GetProcessingErrorsByReporter(),
                        ProcessingErrorsCount = _runSummaryInspector.GetProcessingErrorsCount(),
                        ProcessingWarnings = _runSummaryInspector.GetProcessingWarnings(),
                        ProcessingWarningsByReporter = _runSummaryInspector.GetProcessingWarningsByReporter(),
                        ProcessingWarningsCount = _runSummaryInspector.GetProcessingWarningsCount(),
                        PublisherCreatedProjectSuccessfully = _runSummaryInspector.GetPublisherCreatedProjectSuccessfully(),
                        PublisherCreatedProjectExceptionMessage = _runSummaryInspector.GetPublisherCreatedProjectExceptionMessage(),
                        PublisherProjectName = _runSummaryInspector.GetPublisherProjectName(),
                        PublishedWorkbooks = _runSummaryInspector.GetPublishedWorkbooks(),
                        ReasonForFailure = _runSummaryInspector.GetReasonForFailure(),
                        RunId = _runSummaryInspector.GetRunId(),
                        WriterStatistics = _runSummaryInspector.GetWriterStatistics(),
                    },
                    System = new EndMetrics.SystemModel
                    {
                        DebuggerIsAttached = Debugger.IsAttached,
                        PeakWorkingSet = _processInspector.GetPeakWorkingSet(),
                        TelemetryLevel = _config.TelemetryLevel,
                    },
            };

            await _config.MetricUploader.Upload(endMetrics, "logshark.end");
        }

        public async Task ReportMetric(object metricBody, string eventType)
        {
            if (!IsUploadEnabled())
            {
                return;
            }
            await _config.MetricUploader.Upload(metricBody, eventType);
        }

        private bool IsUploadEnabled()
        {
            return _config != null && _config.TelemetryLevel != TelemetryLevel.None;
        }
    }
}
