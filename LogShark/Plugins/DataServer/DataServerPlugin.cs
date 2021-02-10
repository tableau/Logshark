using System.Collections.Generic;
using LogShark.Containers;
using LogShark.Exceptions;
using LogShark.Extensions;
using LogShark.Plugins.DataServer.Model;
using LogShark.Plugins.Shared;
using LogShark.Shared;
using LogShark.Shared.Extensions;
using LogShark.Shared.LogReading.Containers;
using LogShark.Writers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LogShark.Plugins.DataServer
{
    public class DataServerPlugin : IPlugin
    {
        private static readonly DataSetInfo OutputInfo = new DataSetInfo("DataServer", "DataServerEvents");

        public IList<LogType> ConsumedLogTypes => new List<LogType> { LogType.DataserverCpp, LogType.DataserverJava };
        public string Name => "DataServer";
        
        private IProcessingNotificationsCollector _processingNotificationsCollector;
        private IWriter<DataServerEvent> _writer;
        
        public void Configure(IWriterFactory writerFactory, IConfiguration pluginConfig, IProcessingNotificationsCollector processingNotificationsCollector, ILoggerFactory loggerFactory)
        {
            _processingNotificationsCollector = processingNotificationsCollector;
            _writer = writerFactory.GetWriter<DataServerEvent>(OutputInfo);
        }

        public void ProcessLogLine(LogLine logLine, LogType logType)
        {
            switch (logType)
            {
                case LogType.DataserverCpp:
                    ProcessCppLine(logLine);
                    break;
                case LogType.DataserverJava:
                    ProcessJavaLine(logLine);
                    break;
                default:
                    throw new LogSharkProgramLogicException($"{nameof(DataServerPlugin)} received line of type {logType}, however it does not have a logic to process such line");
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

        private void ProcessCppLine(LogLine logLine)
        {
            if (!(logLine.LineContents is NativeJsonLogsBaseEvent baseEvent))
            {
                _processingNotificationsCollector.ReportError($"Was not able to cast line contents as {nameof(NativeJsonLogsBaseEvent)}", logLine, nameof(DataServerPlugin));
                return;
            }

            if (baseEvent.EventPayload == null)
            {
                _processingNotificationsCollector.ReportError("Log line does not contain event payload (\"v\" key in json). Skipping this line", logLine, nameof(DataServerPlugin));
                return;
            }
            
            if (ShouldSkip(baseEvent))
            {
                return;
            }
            
            try
            {
                var @event = new DataServerEvent(logLine, baseEvent);
                _writer.AddLine(@event);
            }
            catch (JsonException ex)
            {
                var message = $"Exception occurred while parsing log line. Most likely - something is wrong with JSON format. Event type: `{baseEvent?.EventType ?? "(null)"}`. Exception message: `{ex.Message}`";
                _processingNotificationsCollector.ReportError(message, logLine, nameof(DataServerPlugin));
            }
        }
        
        private static bool ShouldSkip(NativeJsonLogsBaseEvent baseEvent)
        {
            if (baseEvent.EventType != "msg" || baseEvent.EventPayload.Type != JTokenType.String)
            {
                return false;
            }

            var payloadStringValue = baseEvent.EventPayload.Value<string>();
            if (payloadStringValue.StartsWith("ACTION: Lock Data Server session") ||
                payloadStringValue.StartsWith("ACTION: Unlock Data Server session")) 
            {
                return true; // These are noisy events that are not useful for troubleshooting
            }

            return false;
        }

        private void ProcessJavaLine(LogLine logLine)
        {
            var javaLineMatchResult = logLine.LineContents.MatchJavaLineWithSessionInfo(SharedRegex.JavaLogLineRegex);
            if (!javaLineMatchResult.SuccessfulMatch)
            {
                _processingNotificationsCollector.ReportError("Failed to parse Data Server Java event from log line", logLine, nameof(DataServerPlugin));
                return;
            }
            
            var @event = new DataServerEvent(logLine, javaLineMatchResult);
            _writer.AddLine(@event);
        }
    }
}