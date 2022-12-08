using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using LogShark.Containers;
using LogShark.Plugins.ClusterController;
using LogShark.Shared;
using LogShark.Shared.LogReading.Containers;
using LogShark.Tests.Plugins.Helpers;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LogShark.Tests.Plugins
{
    public class ClusterControllerPluginTests : InvariantCultureTestsBase
    {
        private readonly LogFileInfo _testClusterControllerLogFileInfo = new LogFileInfo("clustercontroller.log", @"folder1/clustercontroller.log", "worker1", new DateTime(2019, 04, 12, 13, 33, 31));
        private readonly LogFileInfo _testZookeeperLogFileInfo = new LogFileInfo("appzookeeper_node1-0.log", @"folder1/appzookeeper_node1-0.log", "worker1", new DateTime(2019, 04, 12, 13, 33, 31));
        private readonly int _testWriterCount = 6;

        [Fact]
        public void BadInput()
        {
            var processingNotificationsCollector = new ProcessingNotificationsCollector(10);
            var testWriterFactory = new TestWriterFactory();
            using (var plugin = new ClusterControllerPlugin())
            {
                plugin.Configure(testWriterFactory, null, processingNotificationsCollector, new NullLoggerFactory());
                
                var ccWrongContentFormat = new LogLine(new ReadLogLineResult(123, 1234), _testClusterControllerLogFileInfo);
                var ccNullContent = new LogLine(new ReadLogLineResult(123, null), _testClusterControllerLogFileInfo);
                var ccIncorrectString = new LogLine(new ReadLogLineResult(123, "I am not a Java log!"), _testClusterControllerLogFileInfo);
                var zkWrongContentFormat = new LogLine(new ReadLogLineResult(123, 1234), _testZookeeperLogFileInfo);
                var zkNullContent = new LogLine(new ReadLogLineResult(123, null), _testZookeeperLogFileInfo);
                var zkIncorrectString = new LogLine(new ReadLogLineResult(123, "I am not a Java log!"), _testZookeeperLogFileInfo);

                plugin.ProcessLogLine(ccWrongContentFormat, LogType.ClusterController);
                plugin.ProcessLogLine(ccNullContent, LogType.ClusterController);
                plugin.ProcessLogLine(ccIncorrectString, LogType.ClusterController);
                plugin.ProcessLogLine(zkWrongContentFormat, LogType.Zookeeper);
                plugin.ProcessLogLine(zkNullContent, LogType.Zookeeper);
                plugin.ProcessLogLine(zkIncorrectString, LogType.Zookeeper);
            }

            testWriterFactory.AssertAllWritersAreDisposedAndEmpty(_testWriterCount);
            processingNotificationsCollector.TotalErrorsReported.Should().Be(_testWriterCount);
        }
        
        [Fact]
        public void ClusterControllerPluginErrorTest()
        {
            var testCases = new List<PluginTestCase>
            {
                new PluginTestCase
                {
                    LogType = LogType.ClusterController,
                    LogContents = "2018-08-08 11:10:12.705 +1000 pool-18-thread-1   ERROR : com.tableausoftware.cluster.http.HttpServiceMonitor - IOException connecting to HTTP server at http://localhost:8000/favicon.ico",
                    LogFileInfo = new LogFileInfo("clustercontroller.log", @"folder1/clustercontroller.log", "worker1", new DateTime(2019, 04, 12, 13, 33, 31)),
                    LineNumber = 10,
                    ExpectedOutput = new
                    {
                        FileName = "clustercontroller.log",
                        FilePath =  @"folder1/clustercontroller.log",
                        LineNumber = 10,
                        Timestamp = new DateTime(2018, 08, 08, 11, 10, 12, 705),
                        Worker = "worker1",
                        Class = "com.tableausoftware.cluster.http.HttpServiceMonitor",
                        Message = "IOException connecting to HTTP server at http://localhost:8000/favicon.ico",
                        Severity = "ERROR",
                    }
                },
            };

            var testWriterFactory = new TestWriterFactory();
            using (var plugin = new ClusterControllerPlugin())
            {
                plugin.Configure(testWriterFactory, null, null, new NullLoggerFactory());

                foreach (var testCase in testCases)
                {
                    var logLine = testCase.GetLogLine();
                    plugin.ProcessLogLine(logLine, testCase.LogType);
                }
            }

            var expectedOutput = testCases.Select(testCase => testCase.ExpectedOutput).ToList();
            var cceDs = new DataSetInfo("ClusterController", "ClusterControllerErrors");
            var testWriter = testWriterFactory.Writers[cceDs] as TestWriter<ClusterControllerError>;

            testWriterFactory.Writers.Count.Should().Be(_testWriterCount);
            testWriter.WasDisposed.Should().Be(true);
            testWriter.ReceivedObjects.Should().BeEquivalentTo(expectedOutput);
        }

        [Fact]
        public void ClusterControllerPluginPostgresActionTest()
        {
            var testCases = new List<PluginTestCase>
            {
                new PluginTestCase
                {
                    LogType = LogType.ClusterController,
                    LogContents = "2018-08-08 15:04:51.901 +1000 Thread-6   INFO : com.tableausoftware.cluster.postgres.PostgresManager - PostgresManager stop",
                    LogFileInfo = _testClusterControllerLogFileInfo,
                    LineNumber = 101,
                    ExpectedOutput = new
                    {
                        FileName = _testClusterControllerLogFileInfo.FileName,
                        FilePath =  _testClusterControllerLogFileInfo.FilePath,
                        LineNumber = 101,
                        Timestamp = new DateTime(2018, 08, 08, 15, 04, 51, 901),
                        Worker = _testClusterControllerLogFileInfo.Worker,
                        Action = "Stop",
                    }
                },
            };

            var testWriterFactory = new TestWriterFactory();
            using (var plugin = new ClusterControllerPlugin())
            {
                plugin.Configure(testWriterFactory, null, null, new NullLoggerFactory());

                foreach (var testCase in testCases)
                {
                    var logLine = testCase.GetLogLine();
                    plugin.ProcessLogLine(logLine, testCase.LogType);
                }
            }

            var expectedOutput = testCases.Select(testCase => testCase.ExpectedOutput).ToList();
            var ccpaDs = new DataSetInfo("ClusterController", "ClusterControllerPostgresActions");
            var testWriter = testWriterFactory.Writers[ccpaDs] as TestWriter<ClusterControllerPostgresAction>;

            testWriterFactory.Writers.Count.Should().Be(_testWriterCount);
            testWriter.WasDisposed.Should().Be(true);
            testWriter.ReceivedObjects.Should().BeEquivalentTo(expectedOutput);
        }

        [Fact]
        public void ClusterControllerPluginDiskSpaceSamplesTest()
        {
            var testCases = new List<PluginTestCase>
            {
                new PluginTestCase // missing disk with total in disk spot
                {
                    LogType = LogType.ClusterController,
                    LogContents = "2020-09-20 17:08:40.163 +0000 pool-37-thread-1   INFO  : com.tableausoftware.cluster.storage.DiskSpaceMonitor - disk 428901134336: total space=217610723328 used space={}",
                    LogFileInfo = _testClusterControllerLogFileInfo,
                    LineNumber = 262,
                    ExpectedOutput = new
                    {
                        FileName = _testClusterControllerLogFileInfo.FileName,
                        FilePath =  _testClusterControllerLogFileInfo.FilePath,
                        LineNumber = 262,
                        Timestamp = new DateTime(2020, 09, 20, 17, 08, 40, 163),
                        Worker = _testClusterControllerLogFileInfo.Worker,
                        Disk = (string)null,
                        TotalSpace = 428901134336,
                        UsedSpace = 217610723328,
                    }
                },
                new PluginTestCase // correct format
                {
                    LogType = LogType.ClusterController,
                    LogContents = "2020-09-20 17:08:40.164 +0000 pool-37-thread-1   INFO  : com.tableausoftware.cluster.storage.DiskSpaceMonitor - disk SomeDiskString: total space=428901134336 used space=217610723328",
                    LogFileInfo = _testClusterControllerLogFileInfo,
                    LineNumber = 5,
                    ExpectedOutput = new
                    {
                        FileName = _testClusterControllerLogFileInfo.FileName,
                        FilePath =  _testClusterControllerLogFileInfo.FilePath,
                        LineNumber = 5,
                        Timestamp = new DateTime(2020, 09, 20, 17, 08, 40, 164),
                        Worker = _testClusterControllerLogFileInfo.Worker,
                        Disk = "SomeDiskString",
                        TotalSpace = 428901134336,
                        UsedSpace = 217610723328,
                    }
                },
                new PluginTestCase // correct format with no disk number
                {
                    LogType = LogType.ClusterController,
                    LogContents = "2020-09-20 17:08:40.164 +0000 pool-37-thread-1 INFO : com.tableausoftware.cluster.storage.DiskSpaceMonitor - disk: total space=428901134336 used space=217610723328",
                    LogFileInfo = _testClusterControllerLogFileInfo,
                    LineNumber = 5,
                    ExpectedOutput = new
                    {
                        FileName = _testClusterControllerLogFileInfo.FileName,
                        FilePath =  _testClusterControllerLogFileInfo.FilePath,
                        LineNumber = 5,
                        Timestamp = new DateTime(2020, 09, 20, 17, 08, 40, 164),
                        Worker = _testClusterControllerLogFileInfo.Worker,
                        Disk = "",
                        TotalSpace = 428901134336,
                        UsedSpace = 217610723328,
                    }
                },
            };

            var testWriterFactory = new TestWriterFactory();
            using (var plugin = new ClusterControllerPlugin())
            {
                plugin.Configure(testWriterFactory, null, null, new NullLoggerFactory());

                foreach (var testCase in testCases)
                {
                    var logLine = testCase.GetLogLine();
                    plugin.ProcessLogLine(logLine, testCase.LogType);
                }
            }

            var expectedOutput = testCases.Select(testCase => testCase.ExpectedOutput).ToList();
            var ccDiskSpaceDs = new DataSetInfo("ClusterController", "ClusterControllerDiskSpaceSamples");
            var testWriter = testWriterFactory.Writers[ccDiskSpaceDs] as TestWriter<ClusterControllerDiskSpaceSample>;

            testWriterFactory.Writers.Count.Should().Be(_testWriterCount);
            testWriter.WasDisposed.Should().Be(true);
            testWriter.ReceivedObjects.Should().BeEquivalentTo(expectedOutput);
        }

        [Fact]
        public void ClusterControllerPluginDiskIoSamplesTest()
        {
            var testCases = new List<PluginTestCase>
            {
                new PluginTestCase
                {
                    LogType = LogType.ClusterController,
                    LogContents = "2018-08-08 12:11:23.531 +1000 pool-25-thread-1   INFO  : com.tableausoftware.cluster.storage.DiskIOMonitor - disk I/O 1min avg > device:/dev/vda1, reads:0.00, readBytes:0.00, writes:0.35, writeBytes:7645.87, queue:0.00",
                    LogFileInfo = _testClusterControllerLogFileInfo,
                    LineNumber = 262,
                    ExpectedOutput = new
                    {
                        FileName = _testClusterControllerLogFileInfo.FileName,
                        FilePath =  _testClusterControllerLogFileInfo.FilePath,
                        LineNumber = 262,
                        Timestamp = new DateTime(2018, 08, 08, 12, 11, 23, 531),
                        Worker = _testClusterControllerLogFileInfo.Worker,
                        Device = "/dev/vda1",
                        ReadsPerSec = 0.00,
                        ReadBytesPerSec = 0.00,
                        WritesPerSec = 0.35,
                        WriteBytesPerSec = 7645.87,
                        QueueLength = 0.0,
                    }
                },
                new PluginTestCase
                {
                    LogType = LogType.ClusterController,
                    LogContents = "2018-10-03 00:02:46.613 +0000 pool-24-thread-1   INFO  : com.tableausoftware.cluster.storage.DiskIOMonitor - disk I/O 1min avg > device:C:\\, reads:0.00, readBytes:0.00, writes:0.00, writeBytes:0.00, queue:0.00",
                    LogFileInfo = _testClusterControllerLogFileInfo,
                    LineNumber = 5,
                    ExpectedOutput = new
                    {
                        FileName = _testClusterControllerLogFileInfo.FileName,
                        FilePath =  _testClusterControllerLogFileInfo.FilePath,
                        LineNumber = 5,
                        Timestamp = new DateTime(2018, 10, 03, 00, 02, 46, 613),
                        Worker = _testClusterControllerLogFileInfo.Worker,
                        Device = @"C:\",
                        ReadsPerSec = 0.00,
                        ReadBytesPerSec = 0.00,
                        WritesPerSec = 0.0,
                        WriteBytesPerSec = 0.0,
                        QueueLength = 0.0,
                    }
                },
                new PluginTestCase // Comma as decimal delimiter
                {
                    LogType = LogType.ClusterController,
                    LogContents = "2019-06-28 02:05:02.555 +0200 pool-36-thread-1   INFO  : com.tableausoftware.cluster.storage.DiskIOMonitor - disk I/O 1min avg > device:E:\\, reads:0,33, readBytes:81373,87, writes:10,20, writeBytes:228710,40, queue:0,02",
                    LogFileInfo = _testClusterControllerLogFileInfo,
                    LineNumber = 12,
                    ExpectedOutput = new
                    {
                        FileName = _testClusterControllerLogFileInfo.FileName,
                        FilePath =  _testClusterControllerLogFileInfo.FilePath,
                        LineNumber = 12,
                        Timestamp = new DateTime(2019, 06, 28, 2, 5, 2, 555),
                        Worker = _testClusterControllerLogFileInfo.Worker,
                        Device = @"E:\",
                        ReadsPerSec = 0.33,
                        ReadBytesPerSec = 81373.87,
                        WritesPerSec = 10.2,
                        WriteBytesPerSec = 228710.4,
                        QueueLength = 0.02,
                    }
                },
            };

            var testWriterFactory = new TestWriterFactory();
            using (var plugin = new ClusterControllerPlugin())
            {
                plugin.Configure(testWriterFactory, null, null, new NullLoggerFactory());

                foreach (var testCase in testCases)
                {
                    var logLine = testCase.GetLogLine();
                    plugin.ProcessLogLine(logLine, testCase.LogType);
                }
            }

            var expectedOutput = testCases.Select(testCase => testCase.ExpectedOutput).ToList();
            var ccpaDs = new DataSetInfo("ClusterController", "ClusterControllerDiskIoSamples");
            var testWriter = testWriterFactory.Writers[ccpaDs] as TestWriter<ClusterControllerDiskIoSample>;

            testWriterFactory.Writers.Count.Should().Be(_testWriterCount);
            testWriter.WasDisposed.Should().Be(true);
            testWriter.ReceivedObjects.Should().BeEquivalentTo(expectedOutput);
        }

        [Fact]
        public void ClusterControllerPluginZookeeperErrorTest()
        {
            var testCases = new List<PluginTestCase>
            {
                new PluginTestCase
                {
                    LogType = LogType.Zookeeper,
                    LogContents = "2018-08-08 11:01:48.490 +1000 14754 main : ERROR org.apache.zookeeper.server.quorum.QuorumPeerConfig - Invalid configuration, only one server specified (ignoring)",
                    LogFileInfo = _testZookeeperLogFileInfo,
                    LineNumber = 1,
                    ExpectedOutput = new
                    {
                        FileName = _testZookeeperLogFileInfo.FileName,
                        FilePath =  _testZookeeperLogFileInfo.FilePath,
                        LineNumber = 1,
                        Timestamp = new DateTime(2018, 08, 08, 11, 01, 48, 490),
                        Worker = _testZookeeperLogFileInfo.Worker,
                        Class = "org.apache.zookeeper.server.quorum.QuorumPeerConfig",
                        Message = "Invalid configuration, only one server specified (ignoring)",
                        Severity = "ERROR",
                    }
                },
            };

            var testWriterFactory = new TestWriterFactory();
            using (var plugin = new ClusterControllerPlugin())
            {
                plugin.Configure(testWriterFactory, null, null, new NullLoggerFactory());

                foreach (var testCase in testCases)
                {
                    var logLine = testCase.GetLogLine();
                    plugin.ProcessLogLine(logLine, testCase.LogType);
                }
            }

            var expectedOutput = testCases.Select(testCase => testCase.ExpectedOutput).ToList();
            var zkDs = new DataSetInfo("ClusterController", "ZookeeperErrors");
            var testWriter = testWriterFactory.Writers[zkDs] as TestWriter<ZookeeperError>;

            testWriterFactory.Writers.Count.Should().Be(_testWriterCount);
            testWriter.WasDisposed.Should().Be(true);
            testWriter.ReceivedObjects.Should().BeEquivalentTo(expectedOutput);
        }

        [Fact]
        public void ClusterControllerPluginZookeeperFsyncLatencyTest()
        {
            var testCases = new List<PluginTestCase>
            {
                new PluginTestCase
                {
                    LogType = LogType.Zookeeper,
                    LogContents = "2018-10-02 22:09:01.927 +0000  SyncThread:0 : WARN  org.apache.zookeeper.server.persistence.FileTxnLog - fsync-ing the write ahead log in SyncThread:0 took 1013ms which will adversely effect operation latency. See the ZooKeeper troubleshooting guide",
                    LogFileInfo = _testZookeeperLogFileInfo,
                    LineNumber = 1725,
                    ExpectedOutput = new
                    {
                        FileName = _testZookeeperLogFileInfo.FileName,
                        FilePath =  _testZookeeperLogFileInfo.FilePath,
                        LineNumber = 1725,
                        Timestamp = new DateTime(2018, 10, 02, 22, 09, 01, 927),
                        Worker = _testZookeeperLogFileInfo.Worker,
                        FsyncLatencyMs = 1013,
                    }
                },
            };

            var testWriterFactory = new TestWriterFactory();
            using (var plugin = new ClusterControllerPlugin())
            {
                plugin.Configure(testWriterFactory, null, null, new NullLoggerFactory());

                foreach (var testCase in testCases)
                {
                    var logLine = testCase.GetLogLine();
                    plugin.ProcessLogLine(logLine, testCase.LogType);
                }
            }

            var expectedOutput = testCases.Select(testCase => testCase.ExpectedOutput).ToList();
            var zkDs = new DataSetInfo("ClusterController", "ZookeeperFsyncLatencies");
            var testWriter = testWriterFactory.Writers[zkDs] as TestWriter<ZookeeperFsyncLatency>;

            testWriterFactory.Writers.Count.Should().Be(_testWriterCount);
            testWriter.WasDisposed.Should().Be(true);
            testWriter.ReceivedObjects.Should().BeEquivalentTo(expectedOutput);
        }
    }
}
