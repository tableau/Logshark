using LogShark.Containers;
using LogShark.Exceptions;
using LogShark.Extensions;
using LogShark.Shared;
using LogShark.Shared.LogReading.Containers;
using LogShark.Writers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace LogShark.Plugins.TabadminController
{
    public class TabadminControllerPlugin : IPlugin
    {
        private static readonly DataSetInfo BuildsInfo = new DataSetInfo("TabadminController", "TabadminControllerBuildRecords");
        private static readonly DataSetInfo EventsInfo = new DataSetInfo("TabadminController", "TabadminControllerEvents");

        public IList<LogType> ConsumedLogTypes => new List<LogType>
        {
            LogType.ControlLogsJava,
            LogType.TabadminAgentJava,
            LogType.TabadminControllerJava
        };

        public string Name => "TabadminController";

        private IBuildTracker _buildTracker;
        private IProcessingNotificationsCollector _processingNotificationsCollector;
        private TabadminControllerEventParser _tabadminControllerEventParser;
        private TabadminAgentEventParser _tabadminAgentEventParser;
        
        private IWriter<TabadminControllerBuildRecord> _buildsWriter;
        private IWriter<TabadminControllerEvent> _eventsWriter;

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
            _buildTracker = new BuildTracker(processingNotificationsCollector);

            _tabadminControllerEventParser = new TabadminControllerEventParser(_buildTracker, _processingNotificationsCollector);
            _tabadminAgentEventParser = new TabadminAgentEventParser(_processingNotificationsCollector);

            _buildsWriter = writerFactory.GetWriter<TabadminControllerBuildRecord>(BuildsInfo);
            _eventsWriter = writerFactory.GetWriter<TabadminControllerEvent>(EventsInfo);
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
                LogType.TabadminAgentJava => _tabadminAgentEventParser.ParseEvent(logLine, javaLineMatchResult),
                LogType.TabadminControllerJava => _tabadminControllerEventParser.ParseEvent(logLine,
                    javaLineMatchResult),
                _ => throw new LogSharkProgramLogicException(
                    $"{nameof(TabadminControllerPlugin)} received log line of `{logType}` type, but is not configured to process it")
            };

            if (parsedEvent != null)
            {
                _eventsWriter.AddLine(parsedEvent);
            }
        }

        public SinglePluginExecutionResults CompleteProcessing()
        {
            _buildsWriter.AddLines(_buildTracker.GetBuildRecords());

            return new SinglePluginExecutionResults(new[]
            {
                _buildsWriter.Close(),
                _eventsWriter.Close()
            });
        }

        public void Dispose()
        {
            _buildsWriter?.Dispose();
            _eventsWriter?.Dispose();
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