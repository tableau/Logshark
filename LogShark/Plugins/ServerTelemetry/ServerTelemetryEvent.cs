using System;
using LogShark.Shared.LogReading.Containers;

namespace LogShark.Plugins.ServerTelemetry
{
    public class ServerTelemetryEvent : BaseEvent
    {
        public int? ProcessId { get; set; }
        public string ThreadId { get; set; }
        public string SessionId { get; set; }

        public double? DevicePixelRatio { get; set; }
        public string DsdDeviceType { get; set; }
        public string SessionIdInMessage { get; set; }
        public string SiteName { get; set; }
        public string WorkbookName { get; set; }
        public string UserAgent { get; set; }
        public string UserName { get; set; }
        
        public string ActionName { get; set; }
        public int? ActionSizeBytes { get; set; }
        public string ActionType { get; set; }
        public int? AnnotationCount { get; set; }
        public string ClientRenderMode { get; set; }
        public int? CustomShapeCount { get; set; }
        public int? CustomShapePixelCount { get; set; }
        public int? EncodingCount { get; set; }
        public int? FilterFieldCount { get; set; }
        public int? Height { get; set; }
        public string IsDashboard { get; set; }
        public int? NodeCount { get; set; }
        public int? NumViews { get; set; }
        public int? NumZones { get; set; }
        public int? MarkCount { get; set; }
        public int? MarkLabelCount { get; set; }
        public int? PaneCount { get; set; }     
        public int? ReflineCount { get; set; }
        public string RequestId { get; set; }
        public string RepositoryURL { get; set; }
        public string SessionState { get; set; }
        public string SheetName { get; set; }
        public int? TextMarkCount { get; set; }
        public int? TooltipCount { get; set; }
        public int? TransparentLinemarkCount { get; set; }
        public int? VertexCount { get; set; }
        public int? Width { get; set; }
        
        public int? Process { get; set; }

        public ServerTelemetryEvent(LogLine logLine, DateTime timestamp) : base(logLine, timestamp)
        {
        }
    }
}