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
        public string status { get; set; }

        public string refreshedAt { get; set; }
        public string scheduleName { get; set; }
        public string scheduleType { get; set; }

        public string jobName { get; set; }
        public string jobType { get; set; }
        public string jobLuid { get; set; }

        public long? totalTimeSeconds { get; set; }
        public long? runTimeSeconds { get; set; }

        public string queuedTime { get; set; }
        public string startedTime { get; set; }
        public string endTime { get; set; }

        public DateTime Timestamp { get; set; }
    }
}