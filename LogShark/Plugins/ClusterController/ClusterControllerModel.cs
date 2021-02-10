using System;
using LogShark.Containers;
using LogShark.Shared.LogReading.Containers;

namespace LogShark.Plugins.ClusterController
{
   public class ClusterControllerDiskSpaceSample : BaseEvent
    {
        public ClusterControllerDiskSpaceSample(LogLine logLine, DateTime timestamp)
            : base(logLine, timestamp) { }

        public string Disk { get; set; }
        public long? TotalSpace { get; set; }
        public long? UsedSpace { get; set; }
    }

    public class ClusterControllerDiskIoSample : BaseEvent
    {
        public ClusterControllerDiskIoSample(LogLine logLine, DateTime timestamp)
            : base(logLine, timestamp) { }

        public string Device { get; set; }
        public double? QueueLength { get; set; }
        public double? ReadBytesPerSec { get; set; }
        public double? ReadsPerSec { get; set; }
        public double? WriteBytesPerSec { get; set; }
        public double? WritesPerSec { get; set; }
    }

    public class ClusterControllerError : BaseEvent
    {
        public string Class { get; }
        public string Message { get; }
        public string Severity { get; }

        public ClusterControllerError(LogLine logLine, JavaLineMatchResult javaLineMatchResult)
            : base(logLine, javaLineMatchResult.Timestamp)
        {
            Class = javaLineMatchResult.Class;
            Message = javaLineMatchResult.Message;
            Severity = javaLineMatchResult.Severity;
        }
    }

    public class ClusterControllerPostgresAction : BaseEvent
    {
        public ClusterControllerPostgresAction(LogLine logLine, DateTime timestamp)
            : base(logLine, timestamp) { }

        public string Action { get; set; }
    }

    public class ZookeeperError : BaseEvent
    {
        public string Class { get; }
        public string Message { get; }
        public string Severity { get; }

        public ZookeeperError(LogLine logLine, JavaLineMatchResult javaLineMatchResult)
            : base(logLine, javaLineMatchResult.Timestamp)
        {
            Class = javaLineMatchResult.Class;
            Message = javaLineMatchResult.Message;
            Severity = javaLineMatchResult.Severity;
        }
    }

    public class ZookeeperFsyncLatency : BaseEvent
    {
        public ZookeeperFsyncLatency(LogLine logLine, DateTime timestamp)
            : base(logLine, timestamp) { }

        public int? FsyncLatencyMs { get; set; }
    }
}