using LogShark.Containers;
using LogShark.Extensions;
using LogShark.Shared;
using LogShark.Shared.LogReading.Containers;
using LogShark.Writers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace LogShark.Plugins.Prep
{
    public class PrepPlugin : IPlugin
    {
        private static readonly List<LogType> ConsumedLogTypesStatic = new List<LogType>() { LogType.Prep };

        public IList<LogType> ConsumedLogTypes => ConsumedLogTypesStatic;

        public string Name => "Prep";

        private static readonly DataSetInfo OutputInfo = new DataSetInfo("Prep", "PrepEvents");

        private Dictionary<string, DateTime> flowRunStartTimes= new Dictionary<string, DateTime>();

        private IWriter<PrepEvent> _writer;

        private IProcessingNotificationsCollector _processingNotificationsCollector;

        public SinglePluginExecutionResults CompleteProcessing()
        {
            var writerStatistics = _writer.Close();
            return new SinglePluginExecutionResults(writerStatistics);
        }

        public void Configure(IWriterFactory writerFactory, IConfiguration pluginConfig, IProcessingNotificationsCollector processingNotificationsCollector, ILoggerFactory loggerFactory)
        {
            _writer = writerFactory.GetWriter<PrepEvent>(OutputInfo);
            _processingNotificationsCollector = processingNotificationsCollector;
        }

        public void ProcessLogLine(LogLine logLine, LogType logType)
        {
            var javaLineMatchResult = logLine.LineContents.MatchJavaLineWithSessionInfo(SharedRegex.JavaLogLineRegex);
            String message = String.Empty;
            PrepEvent prepEvent;

            if (!javaLineMatchResult.SuccessfulMatch)
            {
                if (!(logLine.LineContents is NativeJsonLogsBaseEvent baseEvent))
                {
                    // If line cannot be parsed by either Java format or native Json format, return.
                    _processingNotificationsCollector.ReportError(
                        $"Could not parse Prep line contents as either {nameof(NativeJsonLogsBaseEvent)} or Java log line", 
                        logLine,
                        nameof(PrepPlugin));
                    return;
                }
                else
                {
                    // If line mathces Json.
                    //
                    // Example:
                    // {"ts":"2022-06-07T22:36:37.520","pid":14220,"tid":"nativeLoomApiInitThread","sev":"info","k":"msg","v":"Native API will use language en_US"}
                    //
                    // All fields will be parsed into corresponding members in PrepEvent
                    prepEvent = new PrepEvent(logLine, baseEvent.Timestamp)
                    {
                        Class = "n/a",
                        Message = baseEvent.EventPayload.ToString(),
                        RequestId = baseEvent.RequestId,
                        SessionId = baseEvent.SessionId,
                        Severity = baseEvent.Severity.ToUpper(),
                        Site = baseEvent.Site,
                        User = baseEvent.Username,
                        TraceId = baseEvent.ContextMetrics?.TraceId,
                    };
                }
            }
            else
            {
                // If line matches Java log.
                //
                // Example:
                // 2022-06-22 19:00:30.817 -0500 (,,,421be3bc-edfc-414f-8866-7cd362f9ec13,) http-nio-8365-exec-6 :
                // INFO  com.tableau.loom.rest.logging.ApiLoggingFilter - rest-api-handler-begin :
                // {"method":"GET","path":"/flow-processor/check_status"}
                //
                // All fields will be parsed into corresponding members in PrepEvent
                prepEvent = new PrepEvent(logLine, javaLineMatchResult.Timestamp)
                {
                    Class = javaLineMatchResult.Class,
                    Message = javaLineMatchResult.Message,
                    RequestId = javaLineMatchResult.RequestId,
                    SessionId = javaLineMatchResult.SessionId,
                    Severity = javaLineMatchResult.Severity.ToUpper(),
                    Site = javaLineMatchResult.Site,
                    User = javaLineMatchResult.User,
                    TraceId = "n/a"
                };
            }

            // Parse error message from log
            this.TryParseErrorCodes(prepEvent);

            // Parse flow run related fields
            this.TryParseFlowRunAction(prepEvent);

            _writer.AddLine(prepEvent);
        }

        public void Dispose()
        {
            _writer?.Dispose();
        }

        private void TryParseErrorCodes(PrepEvent prepEvent)
        {
            prepEvent.ErrorCode = "n/a";
            prepEvent.TableauErrorCode = "n/a";
            if (prepEvent.Severity.Equals("error", StringComparison.OrdinalIgnoreCase) ||
                prepEvent.Severity.Equals("fatal", StringComparison.OrdinalIgnoreCase))
            {
                prepEvent.ErrorCode = this.ParseErrorCode(prepEvent.Message);
                prepEvent.TableauErrorCode = this.ParseTableauErrorCode(prepEvent.Message);
            }
        }

        private void TryParseFlowRunAction(PrepEvent prepEvent)
        {
            if (String.IsNullOrEmpty(prepEvent.Message))
            {
                return;
            }

            Regex r = new Regex("flow-run-plan-[a-z]{5,7} : {\"uuid\":\".*\"}");
            Match m = r.Match(prepEvent.Message);

            if (m.Success)
            {
                string action = m.Value.Split("flow-run-plan-").Last().Split(" : ").First();
                string uuid = m.Value.Split("\"")[3];

                if (action.Equals("ended", StringComparison.OrdinalIgnoreCase) || action.Equals("failed", StringComparison.OrdinalIgnoreCase))
                {
                    if(this.flowRunStartTimes.TryGetValue(uuid, out var startTime))
                    {
                        prepEvent.FlowRunTimeInMilliseconds = (prepEvent.Timestamp - startTime).TotalMilliseconds.ToString();
                    }
                }
                else
                {
                    if(action.Equals("started", StringComparison.OrdinalIgnoreCase))
                    {
                        this.flowRunStartTimes.TryAdd(uuid, prepEvent.Timestamp);
                    }
                }

                prepEvent.FlowRunAction = action;
                prepEvent.FlowRunUuid = uuid;
            }
        }

        private string ParseErrorCode(String line)
        {
            if (String.IsNullOrEmpty(line))
            {
                return string.Empty;
            }

            Regex r = new Regex(@"error code \d+");
            Match m = r.Match(line);

            if(m.Success)
            {
                return m.Value.ToString().Split(" ").Last(); // Get the last part, which is the error code.
            }

            return string.Empty;
        }

        private string ParseTableauErrorCode(String line)
        {
            if (String.IsNullOrEmpty(line))
            {
                return string.Empty;
            }

            Regex r = new Regex("TableauErrorCode: 0x[0-9A-F]{8}");
            Match m = r.Match(line);

            if (m.Success)
            {
                return m.Value.ToString().Split(" ").Last(); // Get the last part, which is the tableau error code.
            }

            return string.Empty;
        }
    }

}
