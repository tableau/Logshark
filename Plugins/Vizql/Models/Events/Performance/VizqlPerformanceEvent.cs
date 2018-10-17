namespace Logshark.Plugins.Vizql.Models.Events.Performance
{
    public class VizqlPerformanceEvent : VizqlEvent
    {
        public double? ElapsedSeconds { get; set; }

        public new string KeyType { get; set; }

        public string Value { get; set; }

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
            Worker = vizqlEvent.Worker;
            FilePath = vizqlEvent.FilePath;
            File = vizqlEvent.File;
            LineNumber = vizqlEvent.LineNumber;

            if (vizqlEvent.ValuePayload != null)
            {
                Value = vizqlEvent.ValuePayload.ToString();
            }
        }
    }
}