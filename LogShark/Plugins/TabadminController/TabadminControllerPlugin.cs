using System.Collections.Generic;
using System.Text.RegularExpressions;
using LogShark.Containers;
using LogShark.Exceptions;
using LogShark.Extensions;
using LogShark.Shared;
using LogShark.Shared.LogReading.Containers;
using LogShark.Writers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LogShark.Plugins.TabadminController
{
    public class TabadminControllerPlugin : IPlugin
    {
        private static readonly DataSetInfo OutputInfo =
            new DataSetInfo("TabadminController", "TabadminControllerEvents");

        public IList<LogType> ConsumedLogTypes => new List<LogType>
        {
            LogType.ControlLogsJava,
            LogType.TabadminAgentJava,
            LogType.TabadminControllerJava
        };

        public string Name => "TabadminController";

        private IProcessingNotificationsCollector _processingNotificationsCollector;
        private TabadminControllerEventParser _tabadminControllerEventParser;
        private IWriter<TabadminControllerEvent> _writer;

        #region Regex

        // Example 1 - Tabadmin controller - no pid logged
        //   2020-09-30 18:24:10.172 +0000 pool-21-thread-1 : INFO com.tableausoftware.tabadmin.webapp.asyncjobs.AsyncJobService - Running job 12 of type RestartServerJob
        // ts^              ts_offset^ thread^             sev^ class^                                                        message^
        // Example 2 - Control log
        // 2020-09-28 00:06:23.382 -0500 4016 main : INFO  org.apache.zookeeper.ZooKeeper - Client environment:zookeeper.version=3.5.7-f0fdd52973d373ffd9c86b81d99842dc2c7f660e, built on 02/10/2020 11:30 GMT
        // ts^            ts_offset^  pid^ thread^ sev^ class^                             message^
        private static readonly Regex TabadminControllerAndAgentJavaRegex = new Regex(@"^
            (?<ts>\d{4}-\d{2}-\d{2}\s\d{2}:\d{2}:\d{2}.\d{3})\s
            (?<ts_offset>.+?)\s
            (?<pid>.+?)?\s
            (?<thread>.*?)\s
            :\s
            (?<sev>[A-Z]+)(\s+)
            (?<class>.*?)\s-\s
            (?<message>(.|\n)*)",
            RegexOptions.ExplicitCapture | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);

        #endregion Regex

        public void Configure(IWriterFactory writerFactory, IConfiguration pluginConfig, IProcessingNotificationsCollector processingNotificationsCollector, ILoggerFactory loggerFactory)
        {
            _processingNotificationsCollector = processingNotificationsCollector;
            _tabadminControllerEventParser = new TabadminControllerEventParser(
                new BuildTracker(processingNotificationsCollector),
                _processingNotificationsCollector);
            _writer = writerFactory.GetWriter<TabadminControllerEvent>(OutputInfo);
        }

        public void ProcessLogLine(LogLine logLine, LogType logType)
        {
            var javaLineMatchResult = logLine.LineContents.MatchJavaLine(TabadminControllerAndAgentJavaRegex);
            if (!javaLineMatchResult.SuccessfulMatch)
            {
                _processingNotificationsCollector.ReportError($"Failed to parse `{logType}` event from log line", logLine, nameof(TabadminControllerPlugin));
                return;
            }

            var parsedEvent = logType switch
            {
                LogType.ControlLogsJava => ParseError(logLine, javaLineMatchResult, "Error - Control Logs"),
                LogType.TabadminAgentJava => ParseError(logLine, javaLineMatchResult, "Error - Tabadmin Agent"),
                LogType.TabadminControllerJava => _tabadminControllerEventParser.ParseEvent(logLine,
                    javaLineMatchResult),
                _ => throw new LogSharkProgramLogicException(
                    $"{nameof(TabadminControllerPlugin)} received log line of `{logType}` type, but is not configured to process it")
            };

            if (parsedEvent != null)
            {
                _writer.AddLine(parsedEvent);
            }
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

        private static TabadminControllerEvent ParseError(LogLine logLine, JavaLineMatchResult javaLineMatchResult,
            string eventType)
        {
            return javaLineMatchResult.IsWarningPriorityOrHigher()
                ? new TabadminControllerEvent(eventType, logLine, javaLineMatchResult)
                : null;
        }
    }
}