using LogShark.Containers;
using LogShark.Plugins.Shared;
using LogShark.Writers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using LogShark.Writers.Containers;

namespace LogShark.Plugins.ServerTelemetry
{
    public class ServerTelemetryPlugin : IPlugin
    {
        private static readonly Regex ProcessRegex = new Regex(@"^[A-F0-9]+-\d+:(?<process>\d+)$", RegexOptions.Compiled);

        private IWriter<ServerTelemetryEvent> _eventWriter;
        private IWriter<ServerTelemetryMetric> _metricWriter;
        private IProcessingNotificationsCollector _processingNotificationsCollector;

        public IList<LogType> ConsumedLogTypes => new List<LogType> { LogType.VizqlserverCpp, };

        public string Name => "ServerTelemetry";

        private static readonly DataSetInfo MetricsDsi = new DataSetInfo("ServerTelemetry", "ServerTelemetryMetrics");
        private static readonly DataSetInfo EventsDsi = new DataSetInfo("ServerTelemetry", "ServerTelemetryEvents");
        
        public void Configure(IWriterFactory writerFactory, IConfiguration pluginConfig, IProcessingNotificationsCollector processingNotificationsCollector, ILoggerFactory loggerFactory)
        {
            _eventWriter = writerFactory.GetWriter<ServerTelemetryEvent>(EventsDsi);
            _metricWriter = writerFactory.GetWriter<ServerTelemetryMetric>(MetricsDsi);
            _processingNotificationsCollector = processingNotificationsCollector;
        }

        public void ProcessLogLine(LogLine logLine, LogType logType)
        {
            if (!(logLine.LineContents is NativeJsonLogsBaseEvent jsonEvent))
            {
                var errorMessage = $"Was not able to cast line contents as {nameof(NativeJsonLogsBaseEvent)}";
                _processingNotificationsCollector.ReportError(errorMessage, logLine, nameof(ServerTelemetryPlugin));
                return;
            }

            if (jsonEvent.EventType != "server-telemetry")
            {
                return;
            }

            if (jsonEvent.EventPayload == null)
            {
                _processingNotificationsCollector.ReportError("ServerTelemetry event has no payload", logLine, nameof(ServerTelemetryPlugin));
                return;
            }

            var jsonMessage = jsonEvent.EventPayload.ToObject<ServerTelemetryEventMessageJson>();

            var jsonMetrics = ParseMetrics(jsonMessage, jsonMessage.RequestInfo.RequestId);
            _metricWriter.AddLines(jsonMetrics);

            var processString = ProcessRegex.Match(jsonMessage.SessionId).Groups["process"].Value;
            var process = int.TryParse(processString, out int p) ? p : (int?)null;

            var @event = new ServerTelemetryEvent(logLine, jsonEvent.Timestamp)
            {
                ActionName = jsonMessage.RequestInfo.ActionName,
                ActionSizeBytes = jsonMessage.RequestInfo.ActionSizeBytes,
                ActionType = jsonMessage.RequestInfo.ActionType,
                AnnotationCount = jsonMessage.RequestInfo.AnnotationCount,
                ClientRenderMode = jsonMessage.RequestInfo.ClientRenderMode,
                CustomShapeCount = jsonMessage.RequestInfo.CustomShapeCount,
                CustomShapePixelCount = jsonMessage.RequestInfo.CustomShapePixelCount,
                DevicePixelRatio = jsonMessage.DevicePixelRatio,
                DsdDeviceType = jsonMessage.DsdDeviceType,
                EncodingCount = jsonMessage.RequestInfo.EncodingCount,
                FilterFieldCount = jsonMessage.RequestInfo.FilterFieldCount,
                Height = jsonMessage.RequestInfo.Height,
                IsDashboard = jsonMessage.RequestInfo.IsDashboard,
                MarkCount = jsonMessage.RequestInfo.MarkCount,
                MarkLabelCount = jsonMessage.RequestInfo.MarkLabelCount,
                NodeCount = jsonMessage.RequestInfo.NodeCount,
                NumViews = jsonMessage.RequestInfo.NumViews,
                NumZones = jsonMessage.RequestInfo.NumZones,
                PaneCount = jsonMessage.RequestInfo.PaneCount,
                Process = process,
                ProcessId = jsonEvent.ProcessId,
                ReflineCount = jsonMessage.RequestInfo.ReflineCount,
                RepositoryURL = jsonMessage.RequestInfo.RepositoryURL,
                RequestId = jsonMessage.RequestInfo.RequestId,
                SessionId = jsonEvent.SessionId,
                SessionIdInMessage = jsonMessage.SessionId,
                SessionState = jsonMessage.RequestInfo.SessionState,
                SheetName = jsonMessage.RequestInfo.SheetName,
                SiteName = jsonMessage.SiteName,
                TextMarkCount = jsonMessage.RequestInfo.TextMarkCount,
                ThreadId = jsonEvent.ThreadId,
                TooltipCount = jsonMessage.RequestInfo.TooltipCount,
                TransparentLinemarkCount = jsonMessage.RequestInfo.TransparentLinemarkCount,
                UserAgent = jsonMessage.UserAgent,
                UserName = jsonMessage.UserName,
                VertexCount = jsonMessage.RequestInfo.VertexCount,
                Width = jsonMessage.RequestInfo.Width,
                WorkbookName = jsonMessage.WorkbookName,
            };
            
            _eventWriter.AddLine(@event);
        }

        public SinglePluginExecutionResults CompleteProcessing()
        {
            var writersLineCounts = new List<WriterLineCounts>
            {
                _eventWriter.Close(),
                _metricWriter.Close()
            };
            return new SinglePluginExecutionResults(writersLineCounts);
        }

        public void Dispose()
        {
            _eventWriter.Dispose();
            _metricWriter.Dispose();
        }

        private static IList<ServerTelemetryMetric> ParseMetrics(ServerTelemetryEventMessageJson jsonEvent, string requestId)
        {
            var requestInfoMetrics = jsonEvent?.RequestInfo?.Metrics;
            if (requestInfoMetrics == null)
            {
                return null;
            }

            IList<ServerTelemetryMetric> metrics = new List<ServerTelemetryMetric>();

            var metricsObject = requestInfoMetrics.Value<JObject>();

            foreach (var property in metricsObject.Properties())
            {
                var value = property.Value;

                metrics.Add(new ServerTelemetryMetric(
                    metricName: property.Name,
                    requestId: requestId,
                    sessionId: jsonEvent.SessionId,
                    count: (int)value["count"],
                    maxSeconds: (double)value["max"] / 1000,
                    minSeconds: (double)value["min"] / 1000,
                    totalTimeSeconds: (double)value["total-time-ms"] / 1000
                ));
            }

            return metrics;
        }
    }
}
