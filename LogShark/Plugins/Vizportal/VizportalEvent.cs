using System;
using System.Collections.Generic;
using System.Text;
using LogShark.Containers;
using LogShark.Shared.LogReading.Containers;

namespace LogShark.Plugins.Vizportal
{
    public class VizportalEvent : BaseEvent
    {
        public string RequestId { get; }
        public string User { get; }
        public string SessionId { get; }
        public string Site { get; }
        public string Severity { get; }
        public string Class { get; }
        public string Message { get; }

        public VizportalEvent(LogLine logLine, JavaLineMatchResult javaLineMatchResult)
        : base(logLine, javaLineMatchResult.Timestamp)
        {
            Class = javaLineMatchResult.Class;
            Message = javaLineMatchResult.Message;
            RequestId = javaLineMatchResult.RequestId;
            SessionId = javaLineMatchResult.SessionId;
            Severity = javaLineMatchResult.Severity;
            Site = javaLineMatchResult.Site;
            User = javaLineMatchResult.User;
        }
    }
}
