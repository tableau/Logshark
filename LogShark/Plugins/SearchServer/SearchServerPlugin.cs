using System.Collections.Generic;
using System.Text.RegularExpressions;
using LogShark.Containers;
using LogShark.Plugins.Shared;
using LogShark.Shared;
using LogShark.Shared.LogReading.Containers;
using LogShark.Writers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LogShark.Plugins.SearchServer
{
    public class SearchServerPlugin : IPlugin
    {
        private IWriter<SearchServerEvent> _writer;
        private IProcessingNotificationsCollector _processingNotificationsCollector;

        private readonly Regex _regex = new Regex(@"^
            (?<ts>\d{4}-\d{2}-\d{2}\s\d{2}:\d{2}:\d{2}.\d{3})\s
            (?<ts_offset>.+?)\s
            \((?<site>.*?), (?<user>.*?), (?<sess>.*?), (?<req>.*?)\)\s
            (?<thread>.*?)\s
            :\s
            (?<sev>[A-Z]+)(\s+)
            (?<class>.*?)\s-\s
            (?<message>(.|\n)*)",
        RegexOptions.ExplicitCapture | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);

        private static readonly DataSetInfo OutputInfo = new DataSetInfo("SearchServer", "SearchServerEvents");

        public IList<LogType> ConsumedLogTypes => new List<LogType> { LogType.SearchServer };
        public string Name => "SearchServer";

        public void Configure(IWriterFactory writerFactory, IConfiguration pluginConfig, IProcessingNotificationsCollector processingNotificationsCollector, ILoggerFactory loggerFactory)
        {
            _writer = writerFactory.GetWriter<SearchServerEvent>(OutputInfo);
            _processingNotificationsCollector = processingNotificationsCollector;
        }

        public SinglePluginExecutionResults CompleteProcessing()
        {
            var writerStatistics = _writer.Close();
            return new SinglePluginExecutionResults(writerStatistics);
        }

        public void Dispose()
        {
            _writer.Dispose();
        }

        public void ProcessLogLine(LogLine logLine, LogType logType)
        {
            if (!(logLine.LineContents is string logString))
            {
                _processingNotificationsCollector.ReportError("Failed to parse log line as string", logLine, nameof(SearchServerPlugin));
                return;
            }

            var match = _regex.Match(logString);

            if (!match.Success)
            {
                _processingNotificationsCollector.ReportError("Failed to parse log line as SearchServer event", logLine, nameof(SearchServerPlugin));
                return;
            }
            
            var parsed = new SearchServerEvent
            {
                Class = match.Groups["class"].Value,
                File = logLine.LogFileInfo.FileName,
                FilePath = logLine.LogFileInfo.FilePath,
                LineNumber = logLine.LineNumber,
                Message = match.Groups["message"].Value,
                Severity = match.Groups["sev"].Value,
                Timestamp = TimestampParsers.ParseJavaLogsTimestamp(match.Groups["ts"].Value),
                Worker = logLine.LogFileInfo.Worker,
            };

            _writer.AddLine(parsed);
        }
    }
}