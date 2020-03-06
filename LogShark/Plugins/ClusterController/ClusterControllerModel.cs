using LogShark.Containers;
using System;
using System.Globalization;

namespace LogShark.Plugins.ClusterController
{
    public abstract class BaseClusterControllerEvent
    {
        public BaseClusterControllerEvent(LogLine logLine, string timestamp)
        {
            Timestamp = DateTime.ParseExact(timestamp, "yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
            Worker = logLine.LogFileInfo.Worker;
            FilePath = logLine.LogFileInfo.FilePath;
            File = logLine.LogFileInfo.FileName;
            LineNumber = logLine.LineNumber;
        }

        public string File { get; set; }
        public string FilePath { get; set; }
        public int LineNumber { get; set; }
        public DateTime Timestamp { get; set; }
        public string Worker { get; set; }
    }

    public class ClusterControllerDiskIoSample : BaseClusterControllerEvent
    {
        public ClusterControllerDiskIoSample(LogLine logLine, string timestamp)
            : base(logLine, timestamp) { }

        public string Device { get; set; }
        public double? QueueLength { get; set; }
        public double? ReadBytesPerSec { get; set; }
        public double? ReadsPerSec { get; set; }
        public double? WriteBytesPerSec { get; set; }
        public double? WritesPerSec { get; set; }
    }

    public class ClusterControllerError : BaseClusterControllerEvent
    {
        public ClusterControllerError(LogLine logLine, string timestamp)
            : base(logLine, timestamp) { }

        public string Class { get; set; }
        public string Message { get; set; }
        public string Severity { get; set; }
    }

    public class ClusterControllerPostgresAction : BaseClusterControllerEvent
    {
        public ClusterControllerPostgresAction(LogLine logLine, string timestamp)
            : base(logLine, timestamp) { }

        public string Action { get; set; }
    }

    public class ZookeeperError : BaseClusterControllerEvent
    {
        public ZookeeperError(LogLine logLine, string timestamp)
            : base(logLine, timestamp) { }

        public string Class { get; set; }
        public string Message { get; set; }
        public string Severity { get; set; }
    }

    public class ZookeeperFsyncLatency : BaseClusterControllerEvent
    {
        public ZookeeperFsyncLatency(LogLine logLine, string timestamp)
            : base(logLine, timestamp) { }

        public int? FsyncLatencyMs { get; set; }
    }
}