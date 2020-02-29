using System;
using LogShark.Containers;
using LogShark.Plugins.Shared;
using Newtonsoft.Json;

namespace LogShark.Plugins.VizqlDesktop.Model
{
    public class VizqlPerformanceEvent : VizqlDesktopBaseEvent
    {
        public double? ElapsedSeconds { get; }
        public string Value { get; }

        public VizqlPerformanceEvent(
            LogLine logLine,
            NativeJsonLogsBaseEvent baseEvent,
            string sessionId,
            double? elapsedSeconds) 
            : base(logLine, baseEvent, sessionId)
        {
            ElapsedSeconds = elapsedSeconds;
            Value = baseEvent.EventPayload.ToString(Formatting.None);
        }
    }
}