using System;
using System.Collections.Generic;

namespace LogShark.Metrics.Models
{
    public class EndMetrics
    {
        public ContextModel Context { get; set; }
        public SystemModel System { get; set; }

        public class ContextModel
        {
            public IEnumerable<CompletedWorkbookModel> CompletedWorkbooks { get; set; }
            public TimeSpan Elapsed { get; set; }
            public bool IsSuccess { get; set; }
            public IEnumerable<string> LogReadingErrors { get; set; }
            public IEnumerable<LogProcessingStatistic> LogProcessingStatistics { get; set; }
            public long? FullLogSetSizeBytes { get; set; }
            public IEnumerable<PluginModel> LoadedPlugins { get; set; }
            public IEnumerable<ProcessingNotification> ProcessingErrors { get; set; }
            public int ProcessingErrorsCount { get; set; }
            public IEnumerable<ProcessingNotificationByReporter> ProcessingErrorsByReporter { get; set; }
            public IEnumerable<ProcessingNotification> ProcessingWarnings { get; set; }
            public int ProcessingWarningsCount { get; set; }
            public IEnumerable<ProcessingNotificationByReporter> ProcessingWarningsByReporter { get; set; }
            public bool? PublisherCreatedProjectSuccessfully { get; set; }
            public string PublisherCreatedProjectExceptionMessage { get; set; }
            public string PublisherProjectName { get; set; }
            public IEnumerable<PublishedWorkbookModel> PublishedWorkbooks { get; set; }
            public string ReasonForFailure { get; set; }
            public string RunId { get; set; }
            public IEnumerable<WriterStatisticModel> WriterStatistics { get; set; }

            public class LogProcessingStatistic
            {
                public TimeSpan Elapsed { get; set; }
                public int FilesProcessed { get; set; }
                public long FilesSizeBytes { get; set; }
                public long LinesProcessed { get; set; }
                public string LogType { get; set; }
            }

            public class PluginModel
            {
                public string PluginName { get; set; }
                public bool ReceivedData { get; set; }
            }

            public class ProcessingNotification
            {
                public string Message { get; set; }
                public string FilePath { get; set; }
                public int LineNumber { get; set; }
                public string ReportedBy { get; set; }
            }

            public class ProcessingNotificationByReporter
            {
                public string ReportedBy { get; set; }
                public int ProcessingNotificationCount { get; set; }
            }

            public class PublishedWorkbookModel
            {
                public string ExceptionMessage { get; set; }
                public bool PublishedSuccessfully { get; set; }
                public string WorkbookName { get; set; }
            }

            public class CompletedWorkbookModel
            {
                public bool HasAnyData { get; set; }
                public string FinalWorkbookName { get; set; }
                public string OriginalWorkbookName { get; set; }
                public bool GeneratedSuccessfully { get; set; }
                public string ExceptionMessage { get; set; }
            }

            public class WriterStatisticModel
            {
                public string DataSetGroup { get; set; }
                public string DataSetName { get; set; }
                public long LinesPersisted { get; set; }
                public long NullLinesIgnored { get; set; }
            }
        }

        public class SystemModel
        {
            public bool? DebuggerIsAttached { get; set; }
            public long? PeakWorkingSet { get; set; }
            public TelemetryLevel TelemetryLevel { get; set; }
        }
    }
}
