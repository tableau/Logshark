using System.Collections.Generic;
using LogShark.Containers;
using LogShark.Extensions;
using LogShark.Plugins.Shared;
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
            var match = logLine.LineContents.CastToStringAndRegexMatch(SharedRegex.JavaLogLineRegex);
            if (match == null || !match.Success)
            {
                _processingNotificationsCollector.ReportError("Failed to parse Vizportal Java event from log line", logLine, nameof(VizportalPlugin));
                return;
            }

            var @event = new VizportalEvent()
            {
                Class = match.GetString("class"),
                File = logLine.LogFileInfo.FileName,
                FilePath = logLine.LogFileInfo.FilePath,
                LineNumber = logLine.LineNumber,
                Message = match.GetString("message"),
                RequestId = match.GetString("req"),
                SessionId = match.GetString("sess"),
                Severity = match.GetString("sev"),
                Site = match.GetNullableString("site"),
                Timestamp = TimestampParsers.ParseJavaLogsTimestamp(match.GetString("ts")),
                User = match.GetString("user"),
                Worker = logLine.LogFileInfo.Worker,
            };
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