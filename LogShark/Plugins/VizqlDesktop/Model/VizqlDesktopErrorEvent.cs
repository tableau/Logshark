using LogShark.Shared.LogReading.Containers;
using Newtonsoft.Json;

namespace LogShark.Plugins.VizqlDesktop.Model
{
    public class VizqlDesktopErrorEvent : VizqlDesktopBaseEvent
    {
        public string Message { get; }
        public string Severity { get; }

        public VizqlDesktopErrorEvent(
            NativeJsonLogsBaseEvent baseEvent,
            LogLine logLine,
            string sessionId) 
            : base(logLine, baseEvent, sessionId)
        {
            Severity = baseEvent.Severity;
            Message = baseEvent.EventPayload.ToString(Formatting.None);
        }
    }
}