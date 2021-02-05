using System.Collections.Generic;
using System.Text.RegularExpressions;
using LogShark.Containers;
using LogShark.Shared;
using LogShark.Shared.LogReading;
using LogShark.Shared.LogReading.Containers;
using LogShark.Writers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LogShark.Plugins.Postgres
{
    public class PostgresPlugin : IPlugin
    {
        private static readonly Regex DurationMessageRegex = new Regex(@"^duration: (?<duration>\d+)\.", RegexOptions.ExplicitCapture | RegexOptions.Compiled);
        private static readonly DataSetInfo OutputInfo = new DataSetInfo("Postgres", "PostgresEvents");

        private static readonly List<LogType> ConsumedLogTypesStatic = new List<LogType> { LogType.PostgresCsv };
        public IList<LogType> ConsumedLogTypes => ConsumedLogTypesStatic;

        public string Name => "Postgres";

        private IWriter<PostgresEvent> _writer;
        private IProcessingNotificationsCollector _processingNotificationsCollector;

        public void Configure(IWriterFactory writerFactory, IConfiguration pluginConfig, IProcessingNotificationsCollector processingNotificationsCollector, ILoggerFactory loggerFactory)
        {
            _writer = writerFactory.GetWriter<PostgresEvent>(OutputInfo);
            _processingNotificationsCollector = processingNotificationsCollector;
        }

        public void ProcessLogLine(LogLine logLine, LogType logType)
        {
            if (!(logLine.LineContents is PostgresCsvMapping csvMapping))
            {
                _processingNotificationsCollector.ReportError($"Failed to case received line as {nameof(PostgresCsvMapping)}", logLine, nameof(PostgresPlugin));
                return;
            }

            var duration = (int?)null;
            var durationMatch = DurationMessageRegex.Match(csvMapping.Message);
            if (durationMatch.Success && durationMatch.Groups["duration"] != null)
            {
                if (int.TryParse(durationMatch.Groups["duration"].Value, out var durationInt))
                {
                    duration = durationInt;
                }
            }

            var @event = new PostgresEvent()
            {
                Duration = duration,
                File = logLine.LogFileInfo.FileName,
                FilePath = logLine.LogFileInfo.FilePath,
                LineNumber = logLine.LineNumber,
                Message = csvMapping.Message,
                ProcessId = csvMapping.Pid,
                Severity = csvMapping.Sev,
                Timestamp = csvMapping.Timestamp.ToUniversalTime(), // Postgres logs in GMT and includes timezone, so we need to save it as universal time to preserve original timestamp
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