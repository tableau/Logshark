using ServiceStack.DataAnnotations;

namespace Logshark.Plugins.Vizql.Models.Events.Performance
{
    public class VizqlPerformanceEvent : VizqlEvent
    {
        public double? ElapsedSeconds { get; set; }

        [Index]
        public new string KeyType { get; set; }
        
        public VizqlPerformanceEvent() { }

        public VizqlPerformanceEvent(VizqlEvent vizqlEvent)
        {
            VizqlSessionId = vizqlEvent.VizqlSessionId;
            ApacheRequestId = vizqlEvent.ApacheRequestId;
            ThreadId = vizqlEvent.ThreadId;
            ProcessId = vizqlEvent.ProcessId;
            EventTimestamp = vizqlEvent.EventTimestamp;
            KeyType = vizqlEvent.KeyType;
            ElapsedSeconds = vizqlEvent.GetElapsedTimeInSeconds();
        }
    }
}
