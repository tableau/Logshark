using System;
using LogShark.Containers;
using LogShark.Shared.LogReading.Containers;

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
            JavaLineMatchResult javaLineMatchResult) 
            : base(logLine, javaLineMatchResult.Timestamp)
        {
            EventKey = javaLineMatchResult.Class;
            ProcessId = javaLineMatchResult.ProcessId;
            RequestId = javaLineMatchResult.RequestId;
            SessionId = javaLineMatchResult.SessionId;
            Severity = javaLineMatchResult.Severity;
            Site = javaLineMatchResult.Site;
            ThreadId = javaLineMatchResult.Thread;
            User = javaLineMatchResult.User;
        }
    }
}