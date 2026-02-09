using System.Collections.Generic;
using LogShark.Containers;
using LogShark.Extensions;
using LogShark.Plugins.Shared;
using LogShark.Shared;
using LogShark.Shared.LogReading.Containers;
using LogShark.Writers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace LogShark.Plugins.ResourceManager
{
    public class ResourceManagerPlugin : IPlugin
    {
        private static readonly List<LogType> ConsumedLogTypesStatic = new List<LogType>
        {
            LogType.BackgrounderCpp,
            LogType.DataserverCpp,
            LogType.Hyper,
            LogType.ProtocolServer,
            LogType.VizportalCpp,
            LogType.VizqlserverCpp,
            LogType.flowprocessorCpp,
            LogType.VizdataCpp
        };

        public IList<LogType> ConsumedLogTypes => ConsumedLogTypesStatic;
        public string Name => "ResourceManager";

        private ResourceManagerEventsProcessor _eventsProcessor;
        private IProcessingNotificationsCollector _processingNotificationsCollector;
        
        public void Configure(IWriterFactory writerFactory, IConfiguration pluginConfig, IProcessingNotificationsCollector processingNotificationsCollector, ILoggerFactory loggerFactory)
        {
            _eventsProcessor = new ResourceManagerEventsProcessor(writerFactory, processingNotificationsCollector);
            _processingNotificationsCollector = processingNotificationsCollector;
        }

        public void ProcessLogLine(LogLine logLine, LogType logType)
        {
            if (!(logLine.LineContents is NativeJsonLogsBaseEvent baseEvent))
            {
                var errorMessage = $"Was not able to cast line contents as {nameof(NativeJsonLogsBaseEvent)}";
                _processingNotificationsCollector.ReportError(errorMessage, logLine, nameof(ResourceManagerPlugin));
                return;
            }
           
            if (!CanContainResourceManagerInfo(baseEvent, logType))
            {
                return;
            }
            
            var message = GetSrmMessage(baseEvent, logType, logLine);
            var processName = ProcessInfoParser.GetProcessName(logType);
            _eventsProcessor.ProcessEvent(baseEvent, message, logLine, processName);

           
        }

        public SinglePluginExecutionResults CompleteProcessing()
        {
            var writersLineCounts = _eventsProcessor.CompleteProcessing();
            return new SinglePluginExecutionResults(writersLineCounts);
        }

        public void Dispose()
        {
            _eventsProcessor?.Dispose();
        }

        private static bool CanContainResourceManagerInfo(NativeJsonLogsBaseEvent baseEvent, LogType logType)
        {
            var vizqlLogAndMeetsRequirements = logType != LogType.Hyper
                                             && baseEvent.EventType == "msg"
                                             && baseEvent.EventPayload.Type == JTokenType.String;

            var hyperLogAndMeetsRequirements = logType == LogType.Hyper
                                               && (baseEvent.EventType == "srm-internal" || baseEvent.EventType == "resource-metrics");
            

            var prepLogAndMeetsRequirements = logType == LogType.flowprocessorCpp 
                                                && baseEvent.EventType == "qp-minerva-service" 
                                                && baseEvent.EventPayload.Type == JTokenType.String;

            return vizqlLogAndMeetsRequirements || hyperLogAndMeetsRequirements || prepLogAndMeetsRequirements;
        }

        private string GetSrmMessage(NativeJsonLogsBaseEvent baseEvent, LogType logType, LogLine logLine)
        {
            if (logType != LogType.Hyper)
            {
                return baseEvent.EventPayload.ToObject<string>();
            }

            // Handle different Hyper event types
            if (baseEvent.EventType == "srm-internal")
            {
                // For srm-internal events, extract the msg property
                var message = baseEvent.EventPayload["msg"]?.ToObject<string>();
                if (message == null)
                {
                    const string errorMessage = "Failed to read \"msg\" property as string from srm-internal payload";
                    _processingNotificationsCollector.ReportError(errorMessage, logLine, nameof(ResourceManagerPlugin));
                }
                return message ?? string.Empty;
            }
            else if (baseEvent.EventType == "resource-metrics")
            {
                // For resource-metrics events, return the entire payload as JSON string for processing
                return baseEvent.EventPayload.ToString();
            }

            return string.Empty;
        }
    }
}