using System.Collections.Generic;
using LogShark.Containers;
using LogShark.Writers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LogShark.Plugins.Backgrounder
{
    public class BackgrounderPlugin : IPlugin
    {
        public IList<LogType> ConsumedLogTypes => new List<LogType> {LogType.BackgrounderJava};
        public string Name => "Backgrounder";

        private BackgrounderEventParser _backgrounderEventParser;
        private IBackgrounderEventPersister _backgrounderEventPersister;
        private IProcessingNotificationsCollector _processingNotificationsCollector;

        public void Configure(IWriterFactory writerFactory, IConfiguration pluginConfig, IProcessingNotificationsCollector processingNotificationsCollector, ILoggerFactory loggerFactory)
        {
            _backgrounderEventPersister = new BackgrounderEventPersister(writerFactory);
            _backgrounderEventParser = new BackgrounderEventParser(_backgrounderEventPersister, processingNotificationsCollector);
            _processingNotificationsCollector = processingNotificationsCollector;
        }

        public void ProcessLogLine(LogLine logLine, LogType logType)
        {
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
