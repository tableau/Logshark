using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using LogShark.Containers;
using LogShark.Exceptions;
using LogShark.Plugins.TabadminController;
using LogShark.Shared;
using LogShark.Shared.LogReading.Containers;
using LogShark.Tests.Plugins.Extensions;
using LogShark.Tests.Plugins.Helpers;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace LogShark.Tests.Plugins.TabadminControllerPlugin
{
    public class TabadminControllerPluginTests
    {
        private readonly Mock<IProcessingNotificationsCollector> _processingNotificationsCollectorMock;
        
        private static readonly LogFileInfo TestLogFileInfo = new LogFileInfo("test.log", @"folder1/test.log", "node1", DateTime.MinValue);
        private static readonly DateTime Timestamp = new DateTime(2020, 10, 29, 11, 22, 15);

        public TabadminControllerPluginTests()
        {
            _processingNotificationsCollectorMock = new Mock<IProcessingNotificationsCollector>();
        }
        
        [Fact]
        public void NotAJavaLine()
        {
            var testWriterFactory = new TestWriterFactory();
            var logLine = new LogLine(new ReadLogLineResult(123, "Not a Java Log Line"), TestLogFileInfo);
            using (var plugin = new LogShark.Plugins.TabadminController.TabadminControllerPlugin())
            {
                plugin.Configure(testWriterFactory, null, _processingNotificationsCollectorMock.Object, new NullLoggerFactory());
                plugin.ProcessLogLine(logLine, LogType.TabadminControllerJava);
                plugin.CompleteProcessing();
            }

            testWriterFactory.AssertAllWritersAreDisposedAndEmpty(1);
            _processingNotificationsCollectorMock.Verify(m => m.ReportError(
                It.IsAny<string>(),
                logLine,
                nameof(LogShark.Plugins.TabadminController.TabadminControllerPlugin)),
                Times.Once);
            _processingNotificationsCollectorMock.VerifyNoOtherCalls();
        }
        
        [Fact]
        public void UnsupportedLogType()
        {
            var testWriterFactory = new TestWriterFactory();
            using (var plugin = new LogShark.Plugins.TabadminController.TabadminControllerPlugin())
            {
                plugin.Configure(testWriterFactory, null, _processingNotificationsCollectorMock.Object, new NullLoggerFactory());
                
                var someLogLine = new LogLine(
                    new ReadLogLineResult(123, "2020-09-28 17:47:01.730 -0500  pool-20-thread-1 : INFO  com.tableausoftware.tabadmin.webapp.asyncjobs.AsyncJobService - Running job 117 of type StartServerJob"),
                    TestLogFileInfo);
                Action wrongLogTypeAction = () => { plugin.ProcessLogLine(someLogLine, LogType.Apache);};
                wrongLogTypeAction.Should().Throw<LogSharkProgramLogicException>();
                
                plugin.CompleteProcessing();
            }

            testWriterFactory.AssertAllWritersAreDisposedAndEmpty(1);
            _processingNotificationsCollectorMock.VerifyNoOtherCalls();
        }
        
        [Theory]
        [MemberData(nameof(TestCases))]
        public void RunTestCases(string testName, string logLineText, DateTime timestamp, LogType logType, IDictionary<string, object> nonNullProps)
        {
            var testWriterFactory = new TestWriterFactory();
            var logLine = new LogLine(new ReadLogLineResult(123, logLineText), TestLogFileInfo);
            SinglePluginExecutionResults processingResults;
            using (var plugin = new LogShark.Plugins.TabadminController.TabadminControllerPlugin())
            {
                plugin.Configure(testWriterFactory, null, _processingNotificationsCollectorMock.Object, new NullLoggerFactory());
                plugin.ProcessLogLine(logLine, logType);
                processingResults = plugin.CompleteProcessing();
            }

            _processingNotificationsCollectorMock.VerifyNoOtherCalls();
            
            processingResults.AdditionalTags.Should().BeEmpty();
            processingResults.HasAdditionalTags.Should().BeFalse();
            processingResults.WritersStatistics.Count.Should().Be(1);

            if (nonNullProps == null)
            {
                testWriterFactory.AssertAllWritersAreDisposedAndEmpty(1);
                processingResults.WritersStatistics[0].LinesPersisted.Should().Be(0);
                return;
            }

            processingResults.WritersStatistics[0].LinesPersisted.Should().Be(1);
            var testWriter = testWriterFactory.GetOneWriterAndVerifyOthersAreEmptyAndDisposed<TabadminControllerEvent>("TabadminControllerEvents", 1);
            testWriter.ReceivedObjects.Count.Should().Be(1);
            var result = testWriter.ReceivedObjects.FirstOrDefault() as TabadminControllerEvent;
            result.Should().NotBeNull();
            AssertMethods.AssertThatAllClassOwnPropsAreAtDefaultExpectFor(result, nonNullProps, testName);
            result.VerifyBaseEventProperties(timestamp, logLine);
        }

        public static IEnumerable<object[]> TestCases => new List<object[]>
        {
            new object[]
            {
                "Valid line, but we don't parse it",
                "2020-09-28 17:44:18.273 -0500  pool-20-thread-1 : INFO  com.tableausoftware.tabadmin.webapp.asyncjobs.AsyncJobService - No runnable jobs. Waiting",
                null,
                LogType.TabadminControllerJava,
                null
            },
            
            new object[]
            {
                "Job start message - Good",
                "2020-09-30 18:24:10.172 +0000  pool-21-thread-1 : INFO  com.tableausoftware.tabadmin.webapp.asyncjobs.AsyncJobService - Running job 12 of type RestartServerJob",
                new DateTime(2020, 9, 30, 18, 24, 10, 172), 
                LogType.TabadminControllerJava,
                new Dictionary<string, object>
                {
                    { "Class", "com.tableausoftware.tabadmin.webapp.asyncjobs.AsyncJobService"},
                    { "EventType", "Job Start"},
                    { "JobId", 12 },
                    { "JobType", "RestartServerJob" },
                    { "Message", "Running job 12 of type RestartServerJob" },
                    { "Thread", "pool-21-thread-1" },
                    { "Severity", "INFO" },
                }
            },
            
            new object[]
            {
                "Loading topology event - Good",
                "2020-09-30 18:24:15.172 +0000  qtp762863421-34 : INFO  com.tableausoftware.tabadmin.configuration.builder.AppConfigurationBuilder - Loading topology settings from C:\\ProgramData\\Tableau\\Tableau Server\\data\\tabsvc\\config\\tabadmincontroller_0.20192.19.0718.1543\\topology.yml",
                new DateTime(2020, 9, 30, 18, 24, 15, 172), 
                LogType.TabadminControllerJava,
                new Dictionary<string, object>
                {
                    { "Build", "20192.19.0718.1543"},
                    { "Class", "com.tableausoftware.tabadmin.configuration.builder.AppConfigurationBuilder"},
                    { "EventType", "Loading Topology"},
                    { "Message", "Loading topology settings from C:\\ProgramData\\Tableau\\Tableau Server\\data\\tabsvc\\config\\tabadmincontroller_0.20192.19.0718.1543\\topology.yml" },
                    { "Thread", "qtp762863421-34" },
                    { "Severity", "INFO" },
                }
            },
            
            new object[]
            {
                "Job end message - Good",
                "2020-09-30 18:26:10.172 +0000  pool-21-thread-1 : INFO  com.tableausoftware.tabadmin.webapp.asyncjobs.AsyncJobService - Updated status for job 12 of type RestartServerJob to Succeeded",
                new DateTime(2020, 9, 30, 18, 26, 10, 172), 
                LogType.TabadminControllerJava,
                new Dictionary<string, object>
                {
                    { "Class", "com.tableausoftware.tabadmin.webapp.asyncjobs.AsyncJobService"},
                    { "EventType", "Job Status Update"},
                    { "JobId", 12 },
                    { "JobType", "RestartServerJob" },
                    { "JobStatus", "Succeeded" },
                    { "Message", "Updated status for job 12 of type RestartServerJob to Succeeded" },
                    { "Thread", "pool-21-thread-1" },
                    { "Severity", "INFO" },
                }
            },
            
            new object[]
            {
                "Error in Control log",
                "2020-09-28 00:00:57.753 -0500 7084 main : ERROR com.tableausoftware.tabadmin.agent.TabadminAgent - Exception while configuring process.\njava.lang.NullPointerException: null\n    at sun.nio.fs.WindowsPathParser.parse(WindowsPathParser.java:98) ~[?:1.8.0_252]",
                new DateTime(2020, 9, 28, 0, 0, 57, 753), 
                LogType.ControlLogsJava,
                new Dictionary<string, object>
                {
                    { "Class", "com.tableausoftware.tabadmin.agent.TabadminAgent"},
                    { "EventType", "Error - Control Logs"},
                    { "Message", "Exception while configuring process.\njava.lang.NullPointerException: null\n    at sun.nio.fs.WindowsPathParser.parse(WindowsPathParser.java:98) ~[?:1.8.0_252]" },
                    { "ProcessId", 7084 },
                    { "Thread", "main" },
                    { "Severity", "ERROR" },
                }
            },
            
            new object[]
            {
                "Warning in Control log",
                "2020-09-28 00:00:57.753 -0500 7084 main : WARN com.tableausoftware.tabadmin.agent.TabadminAgent - Exception while configuring process.\njava.lang.NullPointerException: null\n    at sun.nio.fs.WindowsPathParser.parse(WindowsPathParser.java:98) ~[?:1.8.0_252]",
                new DateTime(2020, 9, 28, 0, 0, 57, 753), 
                LogType.ControlLogsJava,
                new Dictionary<string, object>
                {
                    { "Class", "com.tableausoftware.tabadmin.agent.TabadminAgent"},
                    { "EventType", "Error - Control Logs"},
                    { "Message", "Exception while configuring process.\njava.lang.NullPointerException: null\n    at sun.nio.fs.WindowsPathParser.parse(WindowsPathParser.java:98) ~[?:1.8.0_252]" },
                    { "ProcessId", 7084 },
                    { "Thread", "main" },
                    { "Severity", "WARN" },
                }
            },
            
            new object[]
            {
                "Error in Control log, info severity",
                "2020-09-28 00:00:57.753 -0500 7084 main : INFO com.tableausoftware.tabadmin.agent.TabadminAgent - Exception while configuring process.\njava.lang.NullPointerException: null\n    at sun.nio.fs.WindowsPathParser.parse(WindowsPathParser.java:98) ~[?:1.8.0_252]",
                null, 
                LogType.ControlLogsJava,
                null
            },
            
            new object[]
            {
                "Error in Tabadmin Agent log",
                "2020-09-27 19:00:06.936 -0500  StatusRequestDispatcher-0-request : ERROR com.tableausoftware.hyperhealth.HyperHealthRedisPublisher - Timed out while publishing to redis: null",
                new DateTime(2020, 9, 27, 19, 0, 6, 936), 
                LogType.TabadminAgentJava,
                new Dictionary<string, object>
                {
                    { "Class", "com.tableausoftware.hyperhealth.HyperHealthRedisPublisher"},
                    { "EventType", "Error - Tabadmin Agent"},
                    { "Message", "Timed out while publishing to redis: null" },
                    { "Thread", "StatusRequestDispatcher-0-request" },
                    { "Severity", "ERROR" },
                }
            },
            
            new object[]
            {
                "Warning in Tabadmin Agent log",
                "2020-09-27 19:00:06.936 -0500  StatusRequestDispatcher-0-request : WARN com.tableausoftware.hyperhealth.HyperHealthRedisPublisher - Timed out while publishing to redis: null",
                new DateTime(2020, 9, 27, 19, 0, 6, 936), 
                LogType.TabadminAgentJava,
                new Dictionary<string, object>
                {
                    { "Class", "com.tableausoftware.hyperhealth.HyperHealthRedisPublisher"},
                    { "EventType", "Error - Tabadmin Agent"},
                    { "Message", "Timed out while publishing to redis: null" },
                    { "Thread", "StatusRequestDispatcher-0-request" },
                    { "Severity", "WARN" },
                }
            },
            
            new object[]
            {
                "Error in Tabadmin Agent log - info level",
                "2020-09-27 19:00:06.936 -0500  StatusRequestDispatcher-0-request : INFO com.tableausoftware.hyperhealth.HyperHealthRedisPublisher - Timed out while publishing to redis: null",
                null, 
                LogType.TabadminAgentJava,
                null
            },
        };
    }
}