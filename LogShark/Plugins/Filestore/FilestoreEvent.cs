using LogShark.Containers;
using LogShark.Shared.LogReading.Containers;

namespace LogShark.Plugins.Filestore
{
    public class FilestoreEvent : BaseEvent
    {
        public string Class { get; }
        public string Message { get; }
        public string Severity { get; }

        public FilestoreEvent(
            LogLine logLine,
            JavaLineMatchResult javaLineMatchResult) 
            : base(logLine, javaLineMatchResult.Timestamp)
        {
            Severity = javaLineMatchResult.Severity;
            Message = javaLineMatchResult.Message;
            Class = javaLineMatchResult.Class;
        }
    }
}