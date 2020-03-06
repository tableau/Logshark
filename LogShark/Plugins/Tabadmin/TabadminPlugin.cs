using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using LogShark.Containers;
using LogShark.Extensions;
using LogShark.Plugins.Tabadmin.Model;
using LogShark.Writers;
using LogShark.Writers.Containers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LogShark.Plugins.Tabadmin
{
    public class TabadminPlugin : IPlugin
    {
        private readonly IList<Regex> _logLineRegexes = new List<Regex>
        {
            // logs\ style.
            new Regex(@"^
                (?<ts>\d{4}-\d{2}-\d{2}\s\d{2}:\d{2}:\d{2}.\d{3})\s
                (?<ts_offset>.*?)_
                (?<sev>[A-Z]+)_
                (?<address>.*?):
                (?<hostname>.*?)_:_
                pid=(?<pid>\d*)_
                (.*?)__
                user=(?<user>.*?)__
                request=(?<req>.*?)__\s
                (?<message>(.|\n)*)",
                RegexOptions.ExplicitCapture | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled),
            // tabadmin\ style.
            new Regex(@"^
                (?<ts>\d{4}-\d{2}-\d{2}\s\d{2}:\d{2}:\d{2}.\d{3})\s
                (?<ts_offset>.*?)\s
                (?<tid>.*?)\s+
                (?<sev>[A-Z]+)\s+:\s+
                (?<class>.*?)\s-\s
                (?<message>(.|\n)*)",
                RegexOptions.ExplicitCapture | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled)
        };
        private static readonly Regex TabadminActionRegex = new Regex(@"run as: <script>[\s](?<command>[\w]+)([\s](?<arguments>.*))?", RegexOptions.Compiled);
        private static readonly Regex TableauServerVersionRegex = new Regex(@"^====>> <script> (?<shortVersion>.+?) \(build: (?<longVersion>.+?)\):.*<<====$", RegexOptions.Compiled);

        private static readonly List<LogType> ConsumedLogTypesInternal = new List<LogType> { LogType.Tabadmin };
        public IList<LogType> ConsumedLogTypes => ConsumedLogTypesInternal;

        private static readonly DataSetInfo TabadminActionOutputInfo = new DataSetInfo("Tabadmin", "TabadminActions");
        private static readonly DataSetInfo TableauServerVersionOutputInfo = new DataSetInfo("Tabadmin", "TableauServerVersions");
        private static readonly DataSetInfo TabadminErrorOutputInfo = new DataSetInfo("Tabadmin", "TabadminErrors");
        
        public string Name => "Tabadmin";

        private IWriter<TabadminAction> _tabadminActionWriter;
        private IWriter<TableauServerVersion> _tableauServerVersionWriter;
        private IWriter<TabadminError> _tabadminErrorWriter;
        private IProcessingNotificationsCollector _processingNotificationsCollector;
        
        public void Configure(IWriterFactory writerFactory, IConfiguration pluginConfig, IProcessingNotificationsCollector processingNotificationsCollector, ILoggerFactory loggerFactory)
        {
            _tabadminActionWriter = writerFactory.GetWriter<TabadminAction>(TabadminActionOutputInfo);
            _tableauServerVersionWriter = writerFactory.GetWriter<TableauServerVersion>(TableauServerVersionOutputInfo);
            _tabadminErrorWriter = writerFactory.GetWriter<TabadminError>(TabadminErrorOutputInfo);
            _versionsDict = new Dictionary<string, List<TableauServerVersion>>();
            _processingNotificationsCollector = processingNotificationsCollector;
        }

        private Dictionary<string, List<TableauServerVersion>> _versionsDict;

        public void ProcessLogLine(LogLine logLine, LogType logType)
        {
            if (!(logLine.LineContents is string logLineAsString))
            {
                _processingNotificationsCollector.ReportError("Failed to parse log line as string", logLine, nameof(TabadminPlugin));
                return;
            }
            
            var logLineMatch = logLineAsString.GetRegexMatchAndMoveCorrectRegexUpFront(_logLineRegexes);
            if (logLineMatch == null || !logLineMatch.Success)
            {
                _processingNotificationsCollector.ReportError("Failed to parse log line as Tabadmin event", logLine, nameof(TabadminPlugin));
                return;
            }
            
            WriteTableauServerVersionEventIfMatch(logLine, logLineMatch);
            WriteTabadminActionEventIfMatch(logLine, logLineMatch);
            WriteTabadminErrorEventIfMatch(logLine, logLineMatch);
        }

        private void WriteTableauServerVersionEventIfMatch(LogLine logLine, Match logLineMatch)
        {
            var message = logLineMatch.GetString("message");
            var versionMatch = TableauServerVersionRegex.Match(message);
            if (!versionMatch.Success)
            {
                return;
            }
            
            var startTime = DateTime.Parse(logLineMatch.GetString("ts"));
            var offset = logLineMatch.GetString("ts_offset");
            var worker = logLine.LogFileInfo.Worker;
            var version = new TableauServerVersion()
            {
                EndDate = null,
                EndDateGmt = null,
                Id = $"{logLine.LogFileInfo.FilePath}-{logLine.LineNumber}",
                StartDate = startTime,
                StartDateGmt = startTime.Add(OffsetToTimeSpan(offset)),
                TimestampOffset = offset,
                Version = versionMatch.GetString("shortVersion"),
                VersionLong = versionMatch.GetString("longVersion"),
                Worker = worker,
            };
            if (!_versionsDict.ContainsKey(worker))
            {
                _versionsDict[worker] = new List<TableauServerVersion>() { version };
            }
            else
            {
                // As we read the log file, keep the versions dictionary up to date. This is done so that
                // when we read the other events for this plugin (TabadminAction, TabadminError), we can
                // look up their corresponding version number. Because that lookup is done with the event's
                // timestamp, we need to keep the versions timeline properly formed. That is why _versionsDict
                // is reordered and re-aggregated each time a new TableauServerVersion is found, as opposed to 
                // doing it once in CompleteProcessing.
                _versionsDict[worker].Add(version);
                var last = (TableauServerVersion)null;
                _versionsDict[worker] = _versionsDict[worker]
                    .OrderBy(v => v.StartDateGmt)
                    .Aggregate(new List<TableauServerVersion>(), (list, current) =>
                    {
                        if (!list.Any() || last.VersionLong != current.VersionLong)
                        {
                            list.Add(current);
                            if (last != null)
                            {
                                last.EndDate = current.StartDate;
                                last.EndDateGmt = current.StartDateGmt;
                            }
                            last = current;
                        }
                        return list;
                    });
            }
        }

        private void WriteTabadminActionEventIfMatch(LogLine logLine, Match logLineMatch)
        {
            var message = logLineMatch.GetString("message");
            var tabadminActionMatch = TabadminActionRegex.Match(message);
            if (!tabadminActionMatch.Success)
            {
                return;
            }
            
            var timestamp = DateTime.Parse(logLineMatch.GetString("ts"));
            var timestampOffset = logLineMatch.GetString("ts_offset");
            var timestampGmt = timestamp.Add(OffsetToTimeSpan(timestampOffset));
            var worker = logLine.LogFileInfo.Worker;
            var version = GetVersionFromDictionary(worker, timestampGmt);
            var @event = new TabadminAction()
            {
                Arguments = tabadminActionMatch.GetString("arguments"),
                Command = tabadminActionMatch.GetString("command"),
                File = logLine.LogFileInfo.FileName,
                FilePath = logLine.LogFileInfo.FilePath,
                Hostname = logLineMatch.GetString("hostname"),
                Id = $"{logLine.LogFileInfo.FilePath}-{logLine.LineNumber}",
                Line = logLine.LineNumber,
                Timestamp = timestamp,
                TimestampGmt = timestampGmt,
                TimestampOffset = timestampOffset,
                Version = version?.Version,
                VersionId = version?.Id,
                VersionLong = version?.VersionLong,
                Worker = logLine.LogFileInfo.Worker,
            };
            _tabadminActionWriter.AddLine(@event);
        }
        private void WriteTabadminErrorEventIfMatch(LogLine logLine, Match logLineMatch)
        {
            var severity = logLineMatch.GetString("sev");
            if (severity != "WARN" && severity != "ERROR" && severity != "FATAL")
            {
                return;
            }
            
            var timestamp = DateTime.Parse(logLineMatch.GetString("ts"));
            var timestampOffset = logLineMatch.GetString("ts_offset");
            var timestampGmt = timestamp.Add(OffsetToTimeSpan(timestampOffset));
            var worker = logLine.LogFileInfo.Worker;
            var version = GetVersionFromDictionary(worker, timestampGmt);
            var @event = new TabadminError()
            {
                File = logLine.LogFileInfo.FileName,
                FilePath = logLine.LogFileInfo.FilePath,
                Hostname = logLineMatch.GetString("hostname"),
                Id = $"{logLine.LogFileInfo.FilePath}-{logLine.LineNumber}",
                Line = logLine.LineNumber,
                Message = logLineMatch.GetString("message"),
                Severity = severity,
                Timestamp = timestamp,
                TimestampGmt = timestampGmt,
                TimestampOffset = timestampOffset,
                Version = version?.Version,
                VersionId = version?.Id,
                VersionLong = version?.VersionLong,
                Worker = logLine.LogFileInfo.Worker,
            };
            _tabadminErrorWriter.AddLine(@event);
        }

        public SinglePluginExecutionResults CompleteProcessing()
        {
            foreach (var versions in _versionsDict)
            {
                _tableauServerVersionWriter.AddLines(versions.Value);
            }

            var writersLineCounts = new List<WriterLineCounts>
            {
                _tabadminActionWriter.Close(),
                _tableauServerVersionWriter.Close(),
                _tabadminErrorWriter.Close()
            };
            
            return new SinglePluginExecutionResults(writersLineCounts);
        }

        public void Dispose()
        {
            _tabadminActionWriter?.Dispose();
            _tableauServerVersionWriter?.Dispose();
            _tabadminErrorWriter?.Dispose();
        }

        private static TimeSpan OffsetToTimeSpan(string offset)
        {
            // Offset expected format is "+200" or "-1000" for +2 and - 10 GMT, respectively.
            return TimeSpan.FromHours(Double.Parse(offset) / 100);
        }

        private TableauServerVersion GetVersionFromDictionary(string worker, DateTime timestampGmt)
        {
            return _versionsDict.ContainsKey(worker)
                ? _versionsDict[worker].FirstOrDefault(v => v.StartDateGmt <= timestampGmt && (v.EndDateGmt > timestampGmt || v.EndDateGmt == null))
                : null;
        }
    }
}