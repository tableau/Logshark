
using LogShark.Shared.LogReading.Containers;
using LogShark.Containers;
using System;

namespace LogShark.Plugins.Prep
{
    public class PrepEvent : BaseEvent
    {
        private static readonly string NotApplicable = "n/a";
        public string RequestId { get; set; }
        public string User { get; set; }
        public string SessionId { get; set; }
        public string Site { get; set; }
        public string Severity { get; set; }
        public string Class { get; set; }
        public string TraceId { get; set; }
        public string Message { get; set; }
        public string ErrorCode { get; set; }
        public string TableauErrorCode { get; set; }
        public string FlowRunAction { get; set; }
        public string FlowRunUuid { get; set; }
        public string FlowRunTimeInMilliseconds { get; set; }

        public PrepEvent(LogLine logLine,
            DateTime timestamp)
            : base(logLine, timestamp)
        {
        }
    }
}
