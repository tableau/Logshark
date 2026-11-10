using System.Collections.Generic;
using LogShark.Containers;
using LogShark.Shared;
using LogShark.Shared.LogReading.Containers;
using LogShark.Writers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LogShark.Plugins.Backgrounder
{
    public class BackgrounderPlugin : IPlugin
    {
        public IList<LogType> ConsumedLogTypes => new List<LogType> {LogType.BackgrounderJava, LogType.BackgrounderCpp};
        public string Name => "Backgrounder";

        private BackgrounderEventParser _backgrounderEventParser;
        private IBackgrounderEventPersister _backgrounderEventPersister;
        private IProcessingNotificationsCollector _processingNotificationsCollector;

        public void Configure(IPluginWriterProvider writerProvider, IConfiguration pluginConfig, IProcessingNotificationsCollector processingNotificationsCollector, ILoggerFactory loggerFactory)
        {
            _backgrounderEventPersister = new BackgrounderEventPersister(writerProvider);
            _backgrounderEventParser = new BackgrounderEventParser(_backgrounderEventPersister, processingNotificationsCollector);
            _processingNotificationsCollector = processingNotificationsCollector;
        }

        public void ProcessLogLine(LogLine logLine, LogType logType)
        {
            if (logType == LogType.BackgrounderCpp)
            {
                if (!(logLine.LineContents is NativeJsonLogsBaseEvent baseEvent))
                {
                    _processingNotificationsCollector.ReportError("Failed to parse log line as NativeJsonLogsBaseEvent", logLine, nameof(BackgrounderPlugin));
                    return;
                }

                _backgrounderEventParser.ParseAndPersistNativeApiLine(logLine, baseEvent);
                return;
            }

            var logLineText = logLine.LineContents as string;

            if (logLineText == null)
            {
                _processingNotificationsCollector.ReportError("Failed to parse log line as string", logLine, nameof(BackgrounderPlugin));
                return;
            }

            _backgrounderEventParser.ParseAndPersistLine(logLine, logLineText);
        }

        public SinglePluginExecutionResults CompleteProcessing()
        {
            var writersLineCounts = _backgrounderEventPersister.DrainEvents();
            return new SinglePluginExecutionResults(writersLineCounts);
        }
        
        public void Dispose()
        {
            _backgrounderEventPersister.Dispose();
        }
    }
}
