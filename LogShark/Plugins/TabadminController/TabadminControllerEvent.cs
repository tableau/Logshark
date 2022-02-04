using LogShark.Containers;
using LogShark.Shared.LogReading.Containers;

namespace LogShark.Plugins.TabadminController
{
    public class TabadminControllerEvent : BaseEvent
    {
        public string EventType { get; }
        public string Class { get; }
        public string Message { get; }
        public int? ProcessId { get; }
        public string Severity { get; }
        public string Thread { get; }
        
        public string ConfigParametersChanging { get; set; }
        public long? JobId { get; set; }
        public string JobStatus { get; set; }
        public string JobType { get; set; }
        public string LoginClientId { get; set; }
        public string LoginClientIp { get; set; }
        public string LoginUserId { get; set; }
        public string LoginUsername { get; set; }
        public string StatusProcess { get; set; }
        public string StatusMessage { get; set; }
        public string DetailMessage { get; set; }
        public string StepMessage { get; set; }
        public string StepName { get; set; }
        public string StepStatus { get; set; }

        public TabadminControllerEvent(string eventType,
            LogLine logLine,
            JavaLineMatchResult javaLineMatchResult)
            : base(logLine, javaLineMatchResult.Timestamp)
        {
            EventType = eventType;

            Class = javaLineMatchResult.Class;
            Message = javaLineMatchResult.Message;
            ProcessId = javaLineMatchResult.ProcessId;
            Severity = javaLineMatchResult.Severity;
            Thread = javaLineMatchResult.Thread;
        }
    }
}