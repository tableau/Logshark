using System;

namespace LogShark.Plugins.Backgrounder.Model
{
    public class BackgrounderExtractJobDetail
    {
        public string BackgrounderJobId { get; set; }
        public string ExtractGuid { get; set; }
        public string ExtractId { get; set; }
        public long? ExtractSize { get; set; }
        public string ExtractUrl { get; set; }
        public string JobNotes { get; set; }
        public string ResourceName { get; set; }
        public string ResourceType { get; set; }
        public string ScheduleName { get; set; }
        public string Site { get; set; }
        public long? TotalSize { get; set; }
        public long? TwbSize { get; set; }
        public string VizqlSessionId { get; set; }
        public DateTime Timestamp { get; set; }
    }
}