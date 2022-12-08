using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using LogShark.Containers;
using LogShark.Extensions;
using LogShark.Shared;
using LogShark.Shared.Extensions;
using LogShark.Shared.LogReading.Containers;
using LogShark.Writers;
using LogShark.Writers.Containers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LogShark.Plugins.ClusterController
{
    public class ClusterControllerPlugin : IPlugin
    {
        public IList<LogType> ConsumedLogTypes => new List<LogType>
        {
            LogType.ClusterController,
            LogType.Zookeeper
        };
        public string Name => "ClusterController";
        
        private IProcessingNotificationsCollector _processingNotificationsCollector;

        public void Configure(IWriterFactory writerFactory, IConfiguration pluginConfig, IProcessingNotificationsCollector processingNotificationsCollector, ILoggerFactory loggerFactory)
        {
            _errorWriter = writerFactory.GetWriter<ClusterControllerError>(_errorDSI);
            _postgresActionWriter = writerFactory.GetWriter<ClusterControllerPostgresAction>(_postgresActionDSI);
            _diskSpaceSampleWriter = writerFactory.GetWriter<ClusterControllerDiskSpaceSample>(_diskSpaceSampleDSI);
            _diskIOSampleWriter = writerFactory.GetWriter<ClusterControllerDiskIoSample>(_diskIOSampleDSI);
            _zkErrorWriter = writerFactory.GetWriter<ZookeeperError>(_zkErrorDSI);
            _zkFsyncLatencyWriter = writerFactory.GetWriter<ZookeeperFsyncLatency>(_zkFsyncLatencyDSI);

            _processingNotificationsCollector = processingNotificationsCollector;
        }

        public SinglePluginExecutionResults CompleteProcessing()
        {
            var writersLineCounts = new List<WriterLineCounts>
            {
                _errorWriter.Close(),
                _postgresActionWriter.Close(),
                _diskSpaceSampleWriter.Close(),
                _diskIOSampleWriter.Close(),
                _zkErrorWriter.Close(),
                _zkFsyncLatencyWriter.Close()
            };
            
            return new SinglePluginExecutionResults(writersLineCounts);
        }

        public void Dispose()
        {
            _errorWriter.Dispose();
            _postgresActionWriter.Dispose();
            _diskSpaceSampleWriter.Dispose();
            _diskIOSampleWriter.Dispose();
            _zkErrorWriter.Dispose();
            _zkFsyncLatencyWriter.Dispose();
        }

        public void ProcessLogLine(LogLine logLine, LogType logType)
        {
            if (logType == LogType.ClusterController)
            {
                ProcessClusterControllerLine(logLine);
            }
            
            if (logType == LogType.Zookeeper)
            {
                ProcessZookeeperLine(logLine);
            }
        }

        private void ProcessClusterControllerLine(LogLine logLine)
        {
            var javaLineMatchResult = logLine.LineContents.MatchJavaLine(_clusterControllerLogsRegex);
            if (!javaLineMatchResult.SuccessfulMatch)
            {
                _processingNotificationsCollector.ReportError("Failed to process line as Cluster Controller event", logLine, nameof(ClusterControllerPlugin));
                return;
            }
            
            // 2018.2 linux/node1/clustercontroller_0.20182.18.0627.22308809190037074300891/logs/clustercontroller.log:333:2018-08-08 11:10:12.705 +1000 pool-18-thread-1   ERROR : com.tableausoftware.cluster.http.HttpServiceMonitor - IOException connecting to HTTP server at http://localhost:8000/favicon.ico
            if (javaLineMatchResult.IsErrorPriorityOrHigher())
            {
                var entry = new ClusterControllerError(logLine, javaLineMatchResult);
                _errorWriter.AddLine(entry);
            }

            // 2018.2 linux/node2/clustercontroller_0.20182.18.0627.22301467407848617992908/logs/clustercontroller.log:2018-08-08 15:04:51.901 +1000 Thread-6 INFO: com.tableausoftware.cluster.postgres.PostgresManager - PostgresManager stop
            if (javaLineMatchResult.Class == "com.tableausoftware.cluster.postgres.PostgresManager"
                && _postgresMessages.ContainsKey(javaLineMatchResult.Message))
            {
                var entry = new ClusterControllerPostgresAction(logLine, javaLineMatchResult.Timestamp)
                {
                    Action = _postgresMessages.TryGetValue(javaLineMatchResult.Message, out var pm) ? pm : null,
                };

                _postgresActionWriter.AddLine(entry);
            }

            if (javaLineMatchResult.Class == "com.tableausoftware.cluster.storage.DiskSpaceMonitor"
                && javaLineMatchResult.Message.StartsWith(DiskSpaceMonitorPrefix, StringComparison.Ordinal)) 
            {
                var diskSpaceStats = _diskSpaceMessageRegex.Match(javaLineMatchResult.Message);

                if (!diskSpaceStats.Success)
                {
                    _processingNotificationsCollector.ReportError("Failed to parse Cluster Controller DiskSpaceMonitor event from log line", logLine, nameof(ClusterControllerPlugin));
                    return;
                }

                long? usedSpace = diskSpaceStats.GetNullableLong("usedSpace");
                ClusterControllerDiskSpaceSample entry;

                // this if check is a workaround for the fact that DiskSpaceMonitor.java has a bug on this line
                // due to it missing the disk parameter:
                // m_logger.info("disk {}: total space={} used space={}", totalSpace, usedSpace);
                if (usedSpace != null)
                {
                    entry = new ClusterControllerDiskSpaceSample(logLine, javaLineMatchResult.Timestamp)
                    {
                        Disk = diskSpaceStats.GetString("disk"),
                        TotalSpace = diskSpaceStats.GetNullableLong("totalSpace"),
                        UsedSpace = diskSpaceStats.GetNullableLong("usedSpace"),
                    };
                } 
                else
                {
                    entry = new ClusterControllerDiskSpaceSample(logLine, javaLineMatchResult.Timestamp)
                    {
                        Disk = null,
                        TotalSpace = diskSpaceStats.GetNullableLong("disk"),
                        UsedSpace = diskSpaceStats.GetNullableLong("totalSpace"),
                    };
                }
                
                _diskSpaceSampleWriter.AddLine(entry);
            }

            // 2018.2 linux/node2/clustercontroller_0.20182.18.0627.22301467407848617992908/logs/clustercontroller.log:262:2018-08-08 12:11:23.531 +1000 pool-25-thread-1   INFO  : com.tableausoftware.cluster.storage.DiskIOMonitor - disk I/O 1min avg > device:/dev/vda1, reads:0.00, readBytes:0.00, writes:0.35, writeBytes:7645.87, queue:0.00
            // 2018.2_windows/node1/clustercontroller_0.20182.18.1001.2115746959230678183412/logs/clustercontroller.log:5:2018-10-03 00:02:46.613 +0000 pool-24-thread-1   INFO  : com.tableausoftware.cluster.storage.DiskIOMonitor - disk I/O 1min avg > device:C:\, reads:0.00, readBytes:0.00, writes:0.00, writeBytes:0.00, queue:0.00
            if (javaLineMatchResult.Severity == "INFO"
                && javaLineMatchResult.Class == "com.tableausoftware.cluster.storage.DiskIOMonitor"
                && javaLineMatchResult.Message.StartsWith(DiskIoMonitorMessagePrefix, StringComparison.Ordinal))
            {
                var diskStats = _diskIoMessageRegex.Match(javaLineMatchResult.Message);

                if (!diskStats.Success)
                {
                    _processingNotificationsCollector.ReportError("Failed to parse Cluster Controller DiskIOMonitor event from log line", logLine, nameof(ClusterControllerPlugin));
                    return;
                }
                
                // Numbers logged in this even are locale-specific, so we need to use method with normalization on them. Only ClusterController logs appear to log locale-sensitive numbers
                var entry = new ClusterControllerDiskIoSample(logLine, javaLineMatchResult.Timestamp)
                {
                    Device = diskStats.GetString("device"),
                    ReadsPerSec = diskStats.GetNullableDoubleWithDelimiterNormalization("reads"),
                    ReadBytesPerSec = diskStats.GetNullableDoubleWithDelimiterNormalization("readBytes"),
                    WritesPerSec = diskStats.GetNullableDoubleWithDelimiterNormalization("writes"),
                    WriteBytesPerSec = diskStats.GetNullableDoubleWithDelimiterNormalization("writeBytes"),
                    QueueLength = diskStats.GetNullableDoubleWithDelimiterNormalization("queue"),
                };

                _diskIOSampleWriter.AddLine(entry);
            }
        }

        private void ProcessZookeeperLine(LogLine logLine)
        {
            var javaLineMatchResult = logLine.LineContents.MatchJavaLine(_zookeeperRegex);
            if (!javaLineMatchResult.SuccessfulMatch)
            {
                _processingNotificationsCollector.ReportError("Failed to process line as Zookeeper event", logLine, nameof(ClusterControllerPlugin));
                return;
            }
            
            // ./2018.2 linux/node1/appzookeeper_1.20182.18.0627.22308155521326766729002/logs/appzookeeper_node1-0.log:13:2018-08-08 11:01:48.490 +1000 14754 main : ERROR org.apache.zookeeper.server.quorum.QuorumPeerConfig - Invalid configuration, only one server specified(ignoring)
            if (javaLineMatchResult.IsErrorPriorityOrHigher())
            {
                var entry = new ZookeeperError(logLine, javaLineMatchResult);
                _zkErrorWriter.AddLine(entry);
            }

            // ./node1/appzookeeper_0.20182.18.1001.21158905666349186534552/logs/appzookeeper_node1-0.log.2018-10-02:1725:2018-10-02 22:09:01.927 +0000  SyncThread:0 : WARN  org.apache.zookeeper.server.persistence.FileTxnLog - fsync-ing the write ahead log in SyncThread:0 took 1013ms which will adversely effect operation latency. See the ZooKeeper troubleshooting guide
            if (javaLineMatchResult.Class == "org.apache.zookeeper.server.persistence.FileTxnLog"
                && javaLineMatchResult.Message.StartsWith("fsync-ing", StringComparison.Ordinal))
            {
                var fsyncLatencyMatch = _zkFsyncLatencyRegex.Match(javaLineMatchResult.Message);
                if (fsyncLatencyMatch.Success)
                {
                    var entry = new ZookeeperFsyncLatency(logLine, javaLineMatchResult.Timestamp)
                    {
                        FsyncLatencyMs = fsyncLatencyMatch.GetNullableInt("fsync_latency"),
                    };

                    _zkFsyncLatencyWriter.AddLine(entry);
                }
            }
        }

        private const string DiskSpaceMonitorPrefix = "disk";
        private const string DiskIoMonitorMessagePrefix = "disk I/O 1min avg > ";

        private IWriter<ClusterControllerError> _errorWriter;
        private IWriter<ClusterControllerPostgresAction> _postgresActionWriter;
        private IWriter<ClusterControllerDiskSpaceSample> _diskSpaceSampleWriter;
        private IWriter<ClusterControllerDiskIoSample> _diskIOSampleWriter;
        private IWriter<ZookeeperError> _zkErrorWriter;
        private IWriter<ZookeeperFsyncLatency> _zkFsyncLatencyWriter;

        private static readonly Regex _clusterControllerLogsRegex = new Regex(@"^
            (?<ts>\d{4}-\d{2}-\d{2}\s\d{2}:\d{2}:\d{2}.\d{3})\s
            (?<ts_offset>.+?)\s
            (?<thread>.*?)\s
            (?<sev>[A-Z]+)(\s+)
            :\s
            (?<class>.*?)\s-\s
            (?<message>(.|\n)*)",
            RegexOptions.ExplicitCapture | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);

        // disk 428901134336: total space=217610723328 used space={}
        // disk 1: total space=428901134336 used space=217610723328
        private static readonly Regex _diskSpaceMessageRegex = new Regex(@"
            disk(\s(?<disk>[^:]+))?:\s
            total\sspace=(?<totalSpace>[0-9]+|\{\})\s
            used\sspace=(?<usedSpace>[0-9]+|\{\})",
            RegexOptions.ExplicitCapture | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);

        // device:C:\, reads: 0.00, readBytes: 0.00, writes: 0.35, writeBytes: 7645.87, queue: 0.00
        // device:E:\, reads:0,00, readBytes:0,00, writes:6,50, writeBytes:54254,93, queue:0,00
        private static readonly Regex _diskIoMessageRegex = new Regex(@"
            device: \s? (?<device>-?[^,]+) ,\s
            reads: \s? (?<reads>-?\d+[.,]\d+) ,\s
            readBytes: \s? (?<readBytes>-?\d+[.,]\d+) ,\s
            writes: \s? (?<writes>-?\d+[.,]\d+) ,\s
            writeBytes: \s? (?<writeBytes>-?\d+[.,]\d+) ,\s
            queue: \s? (?<queue>-?\d+[.,]\d+)",
            RegexOptions.ExplicitCapture | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);

        private static readonly Regex _zookeeperRegex = new Regex(@"^
            (?<ts>\d{4}-\d{2}-\d{2}\s\d{2}:\d{2}:\d{2}.\d{3})\s
            (?<ts_offset>.+?)\s
            (?<thread>.*?)\s
            :\s
            (?<sev>[A-Z]+)(\s+)
            (?<class>.*?)\s-\s
            (?<message>(.|\n)*)",
            RegexOptions.ExplicitCapture | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);

        private static readonly Regex _zkFsyncLatencyRegex = new Regex(@"fsync-ing the write ahead log in .* took (?<fsync_latency>\d+?)ms.*", RegexOptions.Compiled | RegexOptions.ExplicitCapture);

        // TODO: It's not clear if this list should be exhaustive? Test logs also contain a line with a messages like
        // Starting PostgresManager for worker node "<nodename>"
        // Adding a listener for master node to be selected
        // Creating worker node "<nodename>" as "CONNECTED"
        //
        // Is the PostgresController similar to PostgresManager? Should it be included too?
        // 2018.2 linux/node2/clustercontroller_0.20182.18.0627.22301467407848617992908/logs/clustercontroller.log:319:2018-08-08 12:23:31.613 +1000 pool-21-thread-2   ERROR : com.tableausoftware.cluster.postgres.PostgresController - Error in PostgresController:
        private static Dictionary<string, string> _postgresMessages = new Dictionary<string, string>
        {
            { "Starting Postgres on the current node as master", "StartAsMaster" },
            { "Failing over Postgres on this node to become master", "FailoverAsMaster" },
            { "Starting Postgres on this node as slave", "StartAsSlave" },
            { "PostgresManager stop", "Stop" },
            { "PostgresManager restart", "Restart" },
        };

        private static readonly DataSetInfo _errorDSI = new DataSetInfo("ClusterController", "ClusterControllerErrors");
        private static readonly DataSetInfo _postgresActionDSI = new DataSetInfo("ClusterController", "ClusterControllerPostgresActions");
        private static readonly DataSetInfo _diskSpaceSampleDSI = new DataSetInfo("ClusterController", "ClusterControllerDiskSpaceSamples");
        private static readonly DataSetInfo _diskIOSampleDSI = new DataSetInfo("ClusterController", "ClusterControllerDiskIoSamples");
        private static readonly DataSetInfo _zkErrorDSI = new DataSetInfo("ClusterController", "ZookeeperErrors");
        private static readonly DataSetInfo _zkFsyncLatencyDSI = new DataSetInfo("ClusterController", "ZookeeperFsyncLatencies");
    }
}