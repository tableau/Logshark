using System.Collections.Generic;
using System.Text.RegularExpressions;
using LogShark.Containers;
using LogShark.Extensions;
using LogShark.Shared;
using LogShark.Shared.LogReading.Containers;
using LogShark.Writers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LogShark.Plugins.Filestore
{
    public class FilestorePlugin : IPlugin
    {
        private static readonly DataSetInfo OutputInfo = new DataSetInfo("Filestore", "Filestore");
        private static readonly List<LogType> ConsumedLogTypesStatic = new List<LogType>{ LogType.Filestore };

        public IList<LogType> ConsumedLogTypes => ConsumedLogTypesStatic;
        public string Name => "Filestore";

        private IWriter<FilestoreEvent> _writer;
        private IProcessingNotificationsCollector _processingNotificationsCollector;
        // 2024.2 added optional "pid" to match 2024.2
        private readonly Regex _regex = 
            new Regex(@"^
                        (?<ts>\d{4}-\d{2}-\d{2}\s\d{2}:\d{2}:\d{2}.\d{3})\s
                        (?<ts_offset>.+?)\s
                        ?(?<pid>\d+)?\s
                        (?<thread>.*?)\s+
                        (?<sev>[A-Z]+)(\s+)
                        :\s
                        (?<class>.*?)\s-\s
                        (?<message>(.|\n)*)",
            RegexOptions.ExplicitCapture | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);
        
        public void Configure(IWriterFactory writerFactory, IConfiguration pluginConfig, IProcessingNotificationsCollector processingNotificationsCollector, ILoggerFactory loggerFactory)
        {
            _writer = writerFactory.GetWriter<FilestoreEvent>(OutputInfo);
            _processingNotificationsCollector = processingNotificationsCollector;
        }

        public void ProcessLogLine(LogLine logLine, LogType logType)
        {
            var javaLineMatchResult = logLine.LineContents.MatchJavaLine(_regex);
            if (!javaLineMatchResult.SuccessfulMatch)
            {
                _processingNotificationsCollector.ReportError($"Failed to parse Filestore event from log line", logLine, nameof(FilestorePlugin));
                return;
            }
            
            var @event = new FilestoreEvent(logLine, javaLineMatchResult);
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