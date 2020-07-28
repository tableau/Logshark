using LogShark.Containers;
using LogShark.Plugins.Shared;

namespace LogShark.Plugins.VizqlDesktop.Model
{
    public abstract class VizqlDesktopBaseEvent : BaseEvent
    {
        public string KeyType { get; }
        public int ProcessId { get; }
        public string ThreadId { get; }
        public string SessionId { get; }

        protected VizqlDesktopBaseEvent(LogLine logLine, NativeJsonLogsBaseEvent baseEvent, string sessionId) : base(logLine, baseEvent.Timestamp)
        {
            KeyType = baseEvent.EventType;
            ProcessId = baseEvent.ProcessId;
            ThreadId = baseEvent.ThreadId;
            SessionId = sessionId;
        }
    }
}