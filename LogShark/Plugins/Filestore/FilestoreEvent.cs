using System;
using LogShark.Containers;

namespace LogShark.Plugins.Filestore
{
    public class FilestoreEvent : BaseEvent
    {
        public string Class { get; }
        public string Message { get; }
        public string Severity { get; }

        public FilestoreEvent(
            LogLine logLine,
            DateTime timestamp,
            string @class,
            string message,
            string severity            
            ) : base(logLine, timestamp)
        {
            Severity = severity;
            Message = message;
            Class = @class;
        }
    }
}