using System.Collections.Generic;
using LogShark.Containers;
using LogShark.Extensions;
using LogShark.Plugins.Shared;
using LogShark.Shared;
using LogShark.Shared.Extensions;
using LogShark.Shared.LogReading.Containers;
using LogShark.Writers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LogShark.Plugins.Vizportal
{
    public class VizportalPlugin : IPlugin
    {
        private static readonly DataSetInfo OutputInfo = new DataSetInfo("Vizportal", "VizportalEvents");
        
        private static readonly List<LogType> ConsumedLogTypesInternal = new List<LogType> { LogType.VizportalJava };
        public IList<LogType> ConsumedLogTypes => ConsumedLogTypesInternal;

        public string Name => "Vizportal";

        private IWriter<VizportalEvent> _writer;
        private IProcessingNotificationsCollector _processingNotificationsCollector;

        public void Configure(IWriterFactory writerFactory, IConfiguration pluginConfig, IProcessingNotificationsCollector processingNotificationsCollector, ILoggerFactory loggerFactory)
        {
            _writer = writerFactory.GetWriter<VizportalEvent>(OutputInfo);
            _processingNotificationsCollector = processingNotificationsCollector;
        }

        public void ProcessLogLine(LogLine logLine, LogType logType)
        {
            var javaLineMatchResult = logLine.LineContents.MatchJavaLineWithSessionInfo(SharedRegex.JavaLogLineRegex);
            if (!javaLineMatchResult.SuccessfulMatch)
            {
                _processingNotificationsCollector.ReportError("Failed to parse Vizportal Java event from log line", logLine, nameof(VizportalPlugin));
                return;
            }

            var @event = new VizportalEvent(logLine, javaLineMatchResult);
            _writer.AddLine(@event);
        }

        public SinglePluginExecutionResults CompleteProcessing()
        {
            var writerStatistics = _writer.Close();
            return new SinglePluginExecutionResults(writerStatistics);
        }

        public void Dispose()
        {
            _writer?.Dispose();
        }
    }
}