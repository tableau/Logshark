using System;
using LogShark.Containers;
using LogShark.Plugins.Shared;

namespace LogShark.Plugins
{
    public abstract class BaseCppEvent : BaseEvent
    {
        public string EventKey { get; } // k
        public int? ProcessId { get; } // pid
        public string RequestId { get; } // req
        public string SessionId { get; } // sess
        public string Severity { get; } // sev
        public string Site { get; } // site
        public string ThreadId { get; } // tid
        public string User { get; } // user
        
        public BaseCppEvent(LogLine logLine, NativeJsonLogsBaseEvent baseEvent) : base(logLine, baseEvent.Timestamp)
        {
            EventKey = baseEvent.EventType;
            ProcessId = baseEvent.ProcessId;
            RequestId = baseEvent.RequestId;
            SessionId = baseEvent.SessionId;
            Severity = baseEvent.Severity;
            Site = baseEvent.Site;
            ThreadId = baseEvent.ThreadId;
            User = baseEvent.Username;
        }

        public BaseCppEvent(
            LogLine logLine,
            DateTime timestamp,
            string eventKey,
            int? processId,
            string requestId,
            string sessionId,
            string severity,
            string site,
            string threadId,
            string user) 
            : base(logLine, timestamp)
        {
            EventKey = eventKey;
            ProcessId = processId;
            RequestId = requestId;
            SessionId = sessionId;
            Severity = severity;
            Site = site;
            ThreadId = threadId;
            User = user;
        }
    }
}