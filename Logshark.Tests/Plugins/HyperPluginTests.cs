using FluentAssertions;
using LogShark.Plugins.Hyper;
using LogShark.Plugins.Hyper.Model;
using LogShark.Tests.Plugins.Helpers;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using LogShark.Shared;
using LogShark.Shared.LogReading.Containers;
using Xunit;

namespace LogShark.Tests.Plugins
{
    public class HyperPluginTests : InvariantCultureTestsBase
    {
        private static readonly LogFileInfo TestLogFileInfo = new LogFileInfo("hyper_2018_07_12_02_56_45.log", @"hyper/hyper_2018_07_12_02_56_45.log", "worker0", DateTime.MinValue);

        [Fact]
        public void BadOrNoOpInput()
        {
            var processingNotificationsCollector = new ProcessingNotificationsCollector(10);
            var testWriterFactory = new TestWriterFactory();
            using (var plugin = new HyperPlugin())
            {
                plugin.Configure(testWriterFactory, null, processingNotificationsCollector, new NullLoggerFactory());

                var wrongContentFormat = new LogLine(new ReadLogLineResult(123, "Hyper doesn't expect string"), TestLogFileInfo);
                var nullContent = new LogLine(new ReadLogLineResult(123, null), TestLogFileInfo);

                plugin.ProcessLogLine(wrongContentFormat, LogType.VizqlserverCpp);
                plugin.ProcessLogLine(nullContent, LogType.VizqlserverCpp);
            }

            testWriterFactory.AssertAllWritersAreDisposedAndEmpty(2);
            processingNotificationsCollector.TotalErrorsReported.Should().Be(2);
        }

        [Fact]
        public void RunTestCases_HyperError()
        {
            var hyperErrorTestCases = _errorTestCases;

            var testWriterFactory = new TestWriterFactory();
            using (var plugin = new HyperPlugin())
            {
                plugin.Configure(testWriterFactory, null, null, new NullLoggerFactory());

                foreach (var testCase in hyperErrorTestCases)
                {
                    var logLine = testCase.GetLogLine();
                    plugin.ProcessLogLine(logLine, LogType.Hyper);
                }

                plugin.CompleteProcessing();
            }

            var expectedOutput = hyperErrorTestCases.Select(testCase => testCase.ExpectedOutput).ToList();
            var hyperErrorWriter = testWriterFactory.Writers.Values.OfType<TestWriter<HyperError>>().First();

            testWriterFactory.Writers.Count.Should().Be(2);
            hyperErrorWriter.WasDisposed.Should().Be(true);
            hyperErrorWriter.ReceivedObjects.Should().BeEquivalentTo(expectedOutput);
        }

        [Fact]
        public void RunTestCases_HyperQuery()
        {
            var hyperQueryTestCases = _testCases;

            var testWriterFactory = new TestWriterFactory();
            using (var plugin = new HyperPlugin())
            {
                plugin.Configure(testWriterFactory, null, null, new NullLoggerFactory());

                foreach (var testCase in hyperQueryTestCases)
                {
                    var logLine = testCase.GetLogLine();
                    plugin.ProcessLogLine(logLine, LogType.Hyper);
                }

                plugin.CompleteProcessing();
            }

            var expectedOutput = hyperQueryTestCases.Select(testCase => testCase.ExpectedOutput).ToList();
            var hyperQueryWriter = testWriterFactory.Writers.Values.OfType<TestWriter<HyperEvent>>().First();

            testWriterFactory.Writers.Count.Should().Be(2);
            hyperQueryWriter.WasDisposed.Should().Be(true);
            hyperQueryWriter.ReceivedObjects.Should().BeEquivalentTo(expectedOutput);
        }


        private readonly IList<PluginTestCase> _errorTestCases = new List<PluginTestCase>
        {
            new PluginTestCase() {
                LogContents = new NativeJsonLogsBaseEvent()
                {
                    ArtData = null,
                    EventPayload = JToken.Parse(@"{""error"": ""An existing connection was forcibly closed by the remote host"", ""source"": ""handleMain""}"),
                    EventType = "connection-communication-failure",
                    ProcessId = 12252,
                    RequestId = "1",
                    SessionId = "207",
                    Severity = "error",
                    Site = "-",
                    ThreadId = "4ef8",
                    Timestamp = DateTime.Parse("2018-07-12T03:09:27"),
                    Username = "tableau_internal_user",
                },
                ExpectedOutput = new HyperError()
                {
                    FileName = "hyper_2018_07_12_02_56_45.log",
                    FilePath = "hyper/hyper_2018_07_12_02_56_45.log",
                    Key = "connection-communication-failure",
                    Line = 45173,
                    ProcessId = 12252,
                    RequestId = "1",
                    SessionId = "207",
                    Severity = "error",
                    Site = "-",
                    ThreadId = "4ef8",
                    Timestamp = DateTime.Parse("2018-07-12T03:09:27"),
                    User = "tableau_internal_user",
                    Value = $"error: An existing connection was forcibly closed by the remote host{Environment.NewLine}source: handleMain",
                    Worker = "worker0",
                },
                LineNumber = 45173,
                LogFileInfo = TestLogFileInfo,
                LogType = LogType.Hyper
            },
            new PluginTestCase() {
                LogContents = new NativeJsonLogsBaseEvent()
                {
                    ArtData = null,
                    EventPayload = JToken.Parse(@"{""error"": ""An existing connection was forcibly closed by the remote host"", ""source"": ""handleMain""}"),
                    EventType = "query-end-system-error",
                    ProcessId = 12252,
                    RequestId = "1",
                    SessionId = "207",
                    Severity = "error",
                    Site = "default",
                    ThreadId = "4ef8",
                    Timestamp = DateTime.Parse("2018-07-12T03:09:27"),
                    Username = "default",
                },
                ExpectedOutput = new HyperError()
                {
                    FileName = "hyper_2018_07_12_02_56_45.log",
                    FilePath = "hyper/hyper_2018_07_12_02_56_45.log",
                    Key = "query-end-system-error",
                    Line = 45173,
                    ProcessId = 12252,
                    RequestId = "1",
                    SessionId = "207",
                    Severity = "error",
                    Site = "default",
                    ThreadId = "4ef8",
                    Timestamp = DateTime.Parse("2018-07-12T03:09:27"),
                    User = "default",
                    Value = $"error: An existing connection was forcibly closed by the remote host{Environment.NewLine}source: handleMain",
                    Worker = "worker0",
                },
                LineNumber = 45173,
                LogFileInfo = TestLogFileInfo,
                LogType = LogType.Hyper
            },
        };

        private readonly IList<PluginTestCase> _testCases = new List<PluginTestCase>
        {
            new PluginTestCase() { // query-end ctx
                LogContents = new NativeJsonLogsBaseEvent()
                {
                    ArtData = null,
                    ContextMetrics = new ContextMetrics()
                    {
                        ClientSessionId = "F71D8A10BE67431F85AC66F6E6932497-0:0",
                        ClientRequestId = "42"
                    },
                    EventPayload = JToken.Parse(@"
                        {""statement-id"": 1,
                            ""transaction-visible-id"": 0,
                            ""transaction-id"": 0,
                            ""client-session-id"": ""DONTUSETHISVALUE"",
                            ""elapsed"": 0.000262673,
                            ""parsing-time"": 0.000158191,
                            ""compilation-time"": 8.9397E-05,
                            ""execution-time"": 2.228E-05,
                            ""time-to-schedule"": 0.00010993,
                            ""lock-acquisition-time"": 6.496E-06,
                            ""peak-transaction-memory-mb"": 0,
                            ""peak-result-buffer-memory-mb"": 0.062561,
                            ""result-size-mb"": 0.000495911,
                            ""spooling"": false,
                            ""plan-cache-status"": ""not run yet"",
                            ""plan-cache-hit-count"": 0,
                            ""cols"": 0,
                            ""rows"": 0,
                            ""query-trunc"": ""SET SESSIONID=\""F71D8A10BE67431F85AC66F6E6932497-0:0\""""
                        }"),
                    EventType = "query-end",
                    ProcessId = 12252,
                    RequestId = "2",
                    SessionId = "2",
                    Severity = "info",
                    Site = "-",
                    ThreadId = "4544",
                    Timestamp = DateTime.Parse("2018-07-11T11:02:22"),
                    Username = "tableau_internal_user",
                },
                ExpectedOutput = new HyperEvent()
                {
                    ClientRequestId = "42",
                    ClientSessionId = "F71D8A10BE67431F85AC66F6E6932497-0:0",
                    Columns = 0,
                    Elapsed = 0.000262673d,
                    ExclusiveExecution = null,
                    FileName = "hyper_2018_07_12_02_56_45.log",
                    FilePath = "hyper/hyper_2018_07_12_02_56_45.log",
                    Key = "query-end",
                    Line = 41,
                    LockAcquisitionTime = 6.496E-06,
                    PeakResultBufferMemoryMb = 0.062561d,
                    PeakTransactionMemoryMb = 0,
                    PlanCacheHitCount = 0,
                    PlanCacheStatus = "not run yet",
                    ProcessId = 12252,
                    QueryCompilationTime = 8.9397E-05,
                    QueryExecutionTime = 2.228E-05,
                    QueryParsingTime = 0.000158191d,
                    QueryTrunc = "SET SESSIONID=\"F71D8A10BE67431F85AC66F6E6932497-0:0\"",
                    RequestId = "2",
                    ResultSizeMb = 0.000495911d,
                    Rows = 0,
                    SessionId = "2",
                    Severity = "info",
                    Site = "-",
                    Spooling = false,
                    StatementId = "1",
                    ThreadId = "4544",
                    Timestamp = DateTime.Parse("2018-07-11T11:02:22"),
                    TimeToSchedule = 0.00010993d,
                    TransactionId = "0",
                    TransactionVisibleId = "0",
                    User = "tableau_internal_user",
                    Worker = "worker0",
                },
                LineNumber = 41,
                LogFileInfo = TestLogFileInfo,
                LogType = LogType.Hyper
            },

            new PluginTestCase() { // query-end
                LogContents = new NativeJsonLogsBaseEvent()
                {
                    ArtData = null,
                    EventPayload = JToken.Parse(@"
                        {""statement-id"": 1,
                            ""transaction-visible-id"": 0,
                            ""transaction-id"": 0,
                            ""client-session-id"": ""F71D8A10BE67431F85AC66F6E6932497-0:0"",
                            ""elapsed"": 0.000262673,
                            ""parsing-time"": 0.000158191,
                            ""compilation-time"": 8.9397E-05,
                            ""execution-time"": 2.228E-05,
                            ""time-to-schedule"": 0.00010993,
                            ""lock-acquisition-time"": 6.496E-06,
                            ""peak-transaction-memory-mb"": 0,
                            ""peak-result-buffer-memory-mb"": 0.062561,
                            ""result-size-mb"": 0.000495911,
                            ""spooling"": false,
                            ""plan-cache-status"": ""not run yet"",
                            ""plan-cache-hit-count"": 0,
                            ""cols"": 0,
                            ""rows"": 0,
                            ""query-trunc"": ""SET SESSIONID=\""F71D8A10BE67431F85AC66F6E6932497-0:0\""""
                        }"),
                    EventType = "query-end",
                    ProcessId = 12252,
                    RequestId = "2",
                    SessionId = "2",
                    Severity = "info",
                    Site = "-",
                    ThreadId = "4544",
                    Timestamp = DateTime.Parse("2018-07-11T11:02:22"),
                    Username = "tableau_internal_user",
                },
                ExpectedOutput = new HyperEvent()
                {
                    ClientSessionId = "F71D8A10BE67431F85AC66F6E6932497-0:0",
                    Columns = 0,
                    Elapsed = 0.000262673d,
                    ExclusiveExecution = null,
                    FileName = "hyper_2018_07_12_02_56_45.log",
                    FilePath = "hyper/hyper_2018_07_12_02_56_45.log",
                    Key = "query-end",
                    Line = 41,
                    LockAcquisitionTime = 6.496E-06,
                    PeakResultBufferMemoryMb = 0.062561d,
                    PeakTransactionMemoryMb = 0,
                    PlanCacheHitCount = 0,
                    PlanCacheStatus = "not run yet",
                    ProcessId = 12252,
                    QueryCompilationTime = 8.9397E-05,
                    QueryExecutionTime = 2.228E-05,
                    QueryParsingTime = 0.000158191d,
                    QueryTrunc = "SET SESSIONID=\"F71D8A10BE67431F85AC66F6E6932497-0:0\"",
                    RequestId = "2",
                    ResultSizeMb = 0.000495911d,
                    Rows = 0,
                    SessionId = "2",
                    Severity = "info",
                    Site = "-",
                    Spooling = false,
                    StatementId = "1",
                    ThreadId = "4544",
                    Timestamp = DateTime.Parse("2018-07-11T11:02:22"),
                    TimeToSchedule = 0.00010993d,
                    TransactionId = "0",
                    TransactionVisibleId = "0",
                    User = "tableau_internal_user",
                    Worker = "worker0",
                },
                LineNumber = 41,
                LogFileInfo = TestLogFileInfo,
                LogType = LogType.Hyper
            },

            new PluginTestCase() { // query-end-canceLLed
                LogContents = new NativeJsonLogsBaseEvent()
                {
                    ArtData = null,
                    EventPayload = JToken.Parse(@"
                    {   
                        ""elapsed"":948.088,
                        ""parsing-time"":5.8925e-05,
                        ""compilation-time"":0.11608,
                        ""execution-time"":947.971,
                        ""cancelation-delay"":0.528081,
                        ""exec-threads"":{
                            ""total-time"":956.289,""cpu-time"":20.1109,""wait-time"":935.689,
                            ""storage"":{
                                ""access-time"":9.716e-06,""access-count"":9,""access-bytes"":72,""write-time"":0.291634,""write-count"":936,""write-bytes"":66498228
                            }
                        },
                        ""time-to-schedule"":3.4804e-05,
                        ""lock-acquisition-time"":3.25e-07,
                        ""peak-transaction-memory-mb"":0.25,
                        ""peak-result-buffer-memory-mb"":0,
                        ""peak-result-buffer-disk-mb"":0,
                        ""result-size-mb"":0.002388,
                        ""spooling"":false,
                        ""query-settings-active"":false,
                        ""plan-cache-status"": ""cache miss"",
                        ""plan-cache-hit-count"":0,
                        ""cols"":0,
                        ""rows"":0,
                        ""query-trunc"":
                        ""INSERT BULK INTO <truncated>""
                    }"),
                    EventType = "query-end-cancelled",
                    ProcessId = 12252,
                    RequestId = "2",
                    SessionId = "2",
                    Severity = "info",
                    Site = "-",
                    ThreadId = "4544",
                    Timestamp = DateTime.Parse("2018-07-11T11:02:22"),
                    Username = "tableau_internal_user",
                },
                ExpectedOutput = new HyperEvent()
                {
                    Columns = 0,
                    Elapsed = 948.088,
                    ExclusiveExecution = null,
                    FileName = "hyper_2018_07_12_02_56_45.log",
                    FilePath = "hyper/hyper_2018_07_12_02_56_45.log",
                    Key = "query-end-cancelled",
                    Line = 45173,
                    LockAcquisitionTime = 3.25e-07,
                    PeakResultBufferMemoryMb = 0,
                    PeakTransactionMemoryMb = 0.25,
                    PlanCacheHitCount = 0,
                    PlanCacheStatus = "cache miss",
                    ProcessId = 12252,
                    QueryCompilationTime = 0.11608,
                    QueryExecutionTime = 947.971,
                    QueryParsingTime = 5.8925e-05,
                    QueryTrunc = "INSERT BULK INTO <truncated>",
                    RequestId = "2",
                    ResultSizeMb = 0.002388,
                    Rows = 0,
                    SessionId = "2",
                    Severity = "info",
                    Site = "-",
                    Spooling = false,
                    StatementId = null,
                    ThreadId = "4544",
                    Timestamp = DateTime.Parse("2018-07-11T11:02:22"),
                    TimeToSchedule = 3.4804e-05,
                    TransactionId = null,
                    TransactionVisibleId = null,
                    User = "tableau_internal_user",
                    Worker = "worker0",
                    ExecThreadsCpuTime = 20.1109,
                    ExecThreadsWaitTime = 935.689,
                    ExecThreadsTotalTime = 956.289,
                    CopyDataTime = null,
                    CopyDataSize = null,
                    StorageAccessTime = 9.716e-06,
                    StorageAccessCount = 9,
                    StorageAccessBytes = 72,
                    StorageWriteTime = 0.291634,
                    StorageWriteCount = 936,
                    StorageWriteBytes = 66498228,
                },
                LineNumber = 45173,
                LogFileInfo = TestLogFileInfo,
                LogType = LogType.Hyper
            },
            new PluginTestCase() { // asio-continuation-slow
                LogContents = new NativeJsonLogsBaseEvent()
                {
                    ArtData = null,
                    EventPayload = JToken.Parse(@"
                    {   
                       ""source"":""handleMain"",""elapsed"":2.11228,""lock-acquisition-time"":3.284e-06
                    }"),
                    EventType = "asio-continuation-slow",
                    ProcessId = 12252,
                    RequestId = "2",
                    SessionId = "2",
                    Severity = "info",
                    Site = "-",
                    ThreadId = "4544",
                    Timestamp = DateTime.Parse("2018-07-11T11:02:22"),
                    Username = "tableau_internal_user",
                },
                ExpectedOutput = new HyperEvent()
                {
                    Key = "asio-continuation-slow",
                    Elapsed = 2.11228,
                    LockAcquisitionTime = 3.284e-06,
                    Source = "handleMain",
                    Line = 45173,
                    ProcessId = 12252,
                    RequestId = "2",
                    SessionId = "2",
                    Severity = "info",
                    Site = "-",
                    ThreadId = "4544",
                    Timestamp = DateTime.Parse("2018-07-11T11:02:22"),
                    User = "tableau_internal_user",
                    FileName = "hyper_2018_07_12_02_56_45.log",
                    FilePath = "hyper/hyper_2018_07_12_02_56_45.log",
                    Worker = "worker0",
                },
                LineNumber = 45173,
                LogFileInfo = TestLogFileInfo,
                LogType = LogType.Hyper
            },
            new PluginTestCase() { // log-rate-limit-reached
                LogContents = new NativeJsonLogsBaseEvent()
                {
                    ArtData = null,
                    EventPayload = JToken.Parse(@"
                    {   
                       ""key"":""number-network-threads-low"",""current-count"":10,""remaining-interval-seconds"":9.99987
                    }"),
                    EventType = "log-rate-limit-reached",
                    ProcessId = 12252,
                    RequestId = "2",
                    SessionId = "2",
                    Severity = "info",
                    Site = "-",
                    ThreadId = "4544",
                    Timestamp = DateTime.Parse("2018-07-11T11:02:22"),
                    Username = "tableau_internal_user",
                },
                ExpectedOutput = new HyperEvent()
                {
                    Key = "log-rate-limit-reached",
                    SubKey = "number-network-threads-low",
                    CurrentCount = 10,
                    RemainingIntervalSeconds = 9.99987,
                    Line = 45173,
                    ProcessId = 12252,
                    RequestId = "2",
                    SessionId = "2",
                    Severity = "info",
                    Site = "-",
                    ThreadId = "4544",
                    Timestamp = DateTime.Parse("2018-07-11T11:02:22"),
                    User = "tableau_internal_user",
                    FileName = "hyper_2018_07_12_02_56_45.log",
                    FilePath = "hyper/hyper_2018_07_12_02_56_45.log",
                    Worker = "worker0",
                },
                LineNumber = 45173,
                LogFileInfo = TestLogFileInfo,
                LogType = LogType.Hyper
            },

            new PluginTestCase() { // query-end-canceLed
                LogContents = new NativeJsonLogsBaseEvent()
                {
                    ArtData = null,
                    EventPayload = JToken.Parse(@"
                    {   
                        ""elapsed"":948.088,
                        ""parsing-time"":5.8925e-05,
                        ""compilation-time"":0.11608,
                        ""execution-time"":947.971,
                        ""cancelation-delay"":0.528081,
                        ""exec-threads"":{
                            ""total-time"":956.289,""cpu-time"":20.1109,""wait-time"":935.689,
                            ""storage"":{
                                ""access-time"":9.716e-06,""access-count"":9,""access-bytes"":72,""write-time"":0.291634,""write-count"":936,""write-bytes"":66498228
                            }
                        },
                        ""time-to-schedule"":3.4804e-05,
                        ""lock-acquisition-time"":3.25e-07,
                        ""peak-transaction-memory-mb"":0.25,
                        ""peak-result-buffer-memory-mb"":0,
                        ""peak-result-buffer-disk-mb"":0,
                        ""result-size-mb"":0.002388,
                        ""spooling"":false,
                        ""query-settings-active"":false,
                        ""plan-cache-status"": ""cache miss"",
                        ""plan-cache-hit-count"":0,
                        ""cols"":0,
                        ""rows"":0,
                        ""query-trunc"":
                        ""INSERT BULK INTO <truncated>""
                    }"),
                    EventType = "query-end-canceled",
                    ProcessId = 12252,
                    RequestId = "2",
                    SessionId = "2",
                    Severity = "info",
                    Site = "-",
                    ThreadId = "4544",
                    Timestamp = DateTime.Parse("2018-07-11T11:02:22"),
                    Username = "tableau_internal_user",
                },
                ExpectedOutput = new HyperEvent()
                {
                    Columns = 0,
                    Elapsed = 948.088,
                    ExclusiveExecution = null,
                    FileName = "hyper_2018_07_12_02_56_45.log",
                    FilePath = "hyper/hyper_2018_07_12_02_56_45.log",
                    Key = "query-end-canceled",
                    Line = 45173,
                    LockAcquisitionTime = 3.25e-07,
                    PeakResultBufferMemoryMb = 0,
                    PeakTransactionMemoryMb = 0.25,
                    PlanCacheHitCount = 0,
                    PlanCacheStatus = "cache miss",
                    ProcessId = 12252,
                    QueryCompilationTime = 0.11608,
                    QueryExecutionTime = 947.971,
                    QueryParsingTime = 5.8925e-05,
                    QueryTrunc = "INSERT BULK INTO <truncated>",
                    RequestId = "2",
                    ResultSizeMb = 0.002388,
                    Rows = 0,
                    SessionId = "2",
                    Severity = "info",
                    Site = "-",
                    Spooling = false,
                    StatementId = null,
                    ThreadId = "4544",
                    Timestamp = DateTime.Parse("2018-07-11T11:02:22"),
                    TimeToSchedule = 3.4804e-05,
                    TransactionId = null,
                    TransactionVisibleId = null,
                    User = "tableau_internal_user",
                    Worker = "worker0",
                    ExecThreadsCpuTime = 20.1109,
                    ExecThreadsWaitTime = 935.689,
                    ExecThreadsTotalTime = 956.289,
                    CopyDataTime = null,
                    CopyDataSize = null,
                    StorageAccessTime = 9.716e-06,
                    StorageAccessCount = 9,
                    StorageAccessBytes = 72,
                    StorageWriteTime = 0.291634,
                    StorageWriteCount = 936,
                    StorageWriteBytes = 66498228,
                },
                LineNumber = 45173,
                LogFileInfo = TestLogFileInfo,
                LogType = LogType.Hyper
            },

            new PluginTestCase() { // connection-startup-begin
                LogContents = new NativeJsonLogsBaseEvent()
                {
                    ArtData = null,
                    EventPayload = JToken.Parse(@"
                    {
                        ""db-user"":""tableau_internal_user"",
                        ""options"":{""database"":""postgres"", ""<truncated>"":""""}
                    }"),
                    EventType = "connection-startup-begin",
                    ProcessId = 12252,
                    RequestId = "2",
                    SessionId = "2",
                    Severity = "info",
                    Site = "-",
                    ThreadId = "4544",
                    Timestamp = DateTime.Parse("2018-07-11T11:02:22"),
                    Username = "tableau_internal_user",
                },
                ExpectedOutput = new HyperEvent()
                {
                    FileName = "hyper_2018_07_12_02_56_45.log",
                    FilePath = "hyper/hyper_2018_07_12_02_56_45.log",
                    Line = 45173,

                    Key = "connection-startup-begin",
                    ProcessId = 12252,
                    RequestId = "2",
                    SessionId = "2",
                    Severity = "info",
                    Site = "-",
                    ThreadId = "4544",
                    Timestamp = DateTime.Parse("2018-07-11T11:02:22"),
                    User = "tableau_internal_user",
                    Worker = "worker0",

                    DbUser = "tableau_internal_user",
                    Options = @"{
  ""database"": ""postgres"",
  ""<truncated>"": """"
}",
                },
                LineNumber = 45173,
                LogFileInfo = TestLogFileInfo,
                LogType = LogType.Hyper
            },

            new PluginTestCase() { // connection-startup-end
                LogContents = new NativeJsonLogsBaseEvent()
                {
                    ArtData = null,
                    EventPayload = JToken.Parse(@"{
                        ""elapsed-interpret-options"":8.96e-05,
                        ""elapsed-check-user"":2.35e-05,
                        ""have-cred"":false,
                        ""password-requested"":true, 
                        ""elapsed-check-authentication"":6.15e-05,
                        ""elapsed"":0.0001768
                    }"),
                    EventType = "connection-startup-end",
                    ProcessId = 12252,
                    RequestId = "2",
                    SessionId = "2",
                    Severity = "info",
                    Site = "-",
                    ThreadId = "4544",
                    Timestamp = DateTime.Parse("2018-07-11T11:02:22"),
                    Username = "tableau_internal_user",
                },
                ExpectedOutput = new HyperEvent()
                {
                    FileName = "hyper_2018_07_12_02_56_45.log",
                    FilePath = "hyper/hyper_2018_07_12_02_56_45.log",
                    Line = 45173,

                    Key = "connection-startup-end",
                    ProcessId = 12252,
                    RequestId = "2",
                    SessionId = "2",
                    Severity = "info",
                    Site = "-",
                    ThreadId = "4544",
                    Timestamp = DateTime.Parse("2018-07-11T11:02:22"),
                    User = "tableau_internal_user",
                    Worker = "worker0",

                    Elapsed = 0.0001768,
                    ElapsedInterpretOptions = 8.96e-05,
                    ElapsedCheckUser = 2.35e-05,
                    ElapsedCheckAuthentication = 6.15e-05,
                    HaveCred = false,
                    CredName = null,
                },
                LineNumber = 45173,
                LogFileInfo = TestLogFileInfo,
                LogType = LogType.Hyper
            },

            new PluginTestCase() { // cancel-request-received
                LogContents = new NativeJsonLogsBaseEvent()
                {
                    ArtData = null,
                    EventPayload = JToken.Parse(@"{""id"":7688,""secret"":203817136}"),
                    EventType = "cancel-request-received",
                    ProcessId = 12252,
                    RequestId = "2",
                    SessionId = "2",
                    Severity = "info",
                    Site = "-",
                    ThreadId = "4544",
                    Timestamp = DateTime.Parse("2018-07-11T11:02:22"),
                    Username = "tableau_internal_user",
                },
                ExpectedOutput = new HyperEvent()
                {
                    FileName = "hyper_2018_07_12_02_56_45.log",
                    FilePath = "hyper/hyper_2018_07_12_02_56_45.log",
                    Line = 45173,

                    Key = "cancel-request-received",
                    ProcessId = 12252,
                    RequestId = "2",
                    SessionId = "2",
                    Severity = "info",
                    Site = "-",
                    ThreadId = "4544",
                    Timestamp = DateTime.Parse("2018-07-11T11:02:22"),
                    User = "tableau_internal_user",
                    Worker = "worker0",

                    Id = "7688",
                    Secret = 203817136,
                },
                LineNumber = 45173,
                LogFileInfo = TestLogFileInfo,
                LogType = LogType.Hyper
            },

            new PluginTestCase() { // connection-close-request
                LogContents = new NativeJsonLogsBaseEvent()
                {
                    ArtData = null,
                    EventPayload = JToken.Parse(@"{""reason"":""client terminated""}"),
                    EventType = "connection-close-request",
                    ProcessId = 12252,
                    RequestId = "2",
                    SessionId = "2",
                    Severity = "info",
                    Site = "-",
                    ThreadId = "4544",
                    Timestamp = DateTime.Parse("2018-07-11T11:02:22"),
                    Username = "tableau_internal_user",
                },
                ExpectedOutput = new HyperEvent()
                {
                    FileName = "hyper_2018_07_12_02_56_45.log",
                    FilePath = "hyper/hyper_2018_07_12_02_56_45.log",
                    Line = 45173,

                    Key = "connection-close-request",
                    ProcessId = 12252,
                    RequestId = "2",
                    SessionId = "2",
                    Severity = "info",
                    Site = "-",
                    ThreadId = "4544",
                    Timestamp = DateTime.Parse("2018-07-11T11:02:22"),
                    User = "tableau_internal_user",
                    Worker = "worker0",

                    Reason = "client terminated"
                },
                LineNumber = 45173,
                LogFileInfo = TestLogFileInfo,
                LogType = LogType.Hyper
            },

            new PluginTestCase() { // dbregistry-load
                LogContents = new NativeJsonLogsBaseEvent()
                {
                    ArtData = null,
                    EventPayload = JToken.Parse(@"{
                        ""path"":""\\\\?\\C:\\ProgramData\\Tableau\\Tableau Server\\data\\tabsvc\\hyper\\0\\db"",
                        ""new-ref-count"":2,
                        ""already-loaded"":true,
                        ""elapsed"":1.8e-06
                    }"),
                    EventType = "dbregistry-load",
                    ProcessId = 12252,
                    RequestId = "2",
                    SessionId = "2",
                    Severity = "info",
                    Site = "-",
                    ThreadId = "4544",
                    Timestamp = DateTime.Parse("2018-07-11T11:02:22"),
                    Username = "tableau_internal_user",
                },
                ExpectedOutput = new HyperEvent()
                {
                    FileName = "hyper_2018_07_12_02_56_45.log",
                    FilePath = "hyper/hyper_2018_07_12_02_56_45.log",
                    Line = 45173,

                    Key = "dbregistry-load",
                    ProcessId = 12252,
                    RequestId = "2",
                    SessionId = "2",
                    Severity = "info",
                    Site = "-",
                    ThreadId = "4544",
                    Timestamp = DateTime.Parse("2018-07-11T11:02:22"),
                    User = "tableau_internal_user",
                    Worker = "worker0",

                    PathGiven = null,
                    CanonicalPath = null,
                    NewRefCount = 2,
                    AlreadyLoaded = true,
                    Elapsed = 1.8e-06,
                    Error = null,
                },
                LineNumber = 45173,
                LogFileInfo = TestLogFileInfo,
                LogType = LogType.Hyper
            },

            new PluginTestCase() { // dbregistry-release
                LogContents = new NativeJsonLogsBaseEvent()
                {
                    ArtData = null,
                    EventPayload = JToken.Parse(@"{
                        ""path"":""\\\\?\\C:\\ProgramData\\Tableau\\Tableau Server\\data\\tabsvc\\hyper\\0\\db"",
                        ""saved"":true,
                        ""elapsed-save"":0.00247648,
                        ""closed"":true,
                        ""elapsed-registry-close"":1.4745e-05,
                        ""elapsed"":0.00250188
                    }"),
                    EventType = "dbregistry-release",
                    ProcessId = 12252,
                    RequestId = "2",
                    SessionId = "2",
                    Severity = "info",
                    Site = "-",
                    ThreadId = "4544",
                    Timestamp = DateTime.Parse("2018-07-11T11:02:22"),
                    Username = "tableau_internal_user",
                },
                ExpectedOutput = new HyperEvent()
                {
                    FileName = "hyper_2018_07_12_02_56_45.log",
                    FilePath = "hyper/hyper_2018_07_12_02_56_45.log",
                    Line = 45173,

                    Key = "dbregistry-release",
                    ProcessId = 12252,
                    RequestId = "2",
                    SessionId = "2",
                    Severity = "info",
                    Site = "-",
                    ThreadId = "4544",
                    Timestamp = DateTime.Parse("2018-07-11T11:02:22"),
                    User = "tableau_internal_user",
                    Worker = "worker0",

                    PathGiven = null,
                    CanonicalPath = null,
                    Saved = true,
                    FailedOnLoad = null,
                    Error = null,
                    WasUnloaded = null,
                    WasDropped = null,
                    Closed = true,
                    ElapsedSave = 0.00247648,
                    ElapsedRegistryClose = 1.4745e-05,
                    Elapsed = 0.00250188
                },
                LineNumber = 45173,
                LogFileInfo = TestLogFileInfo,
                LogType = LogType.Hyper
            },

            new PluginTestCase() { // query-result-sent
                LogContents = new NativeJsonLogsBaseEvent()
                {
                    ArtData = null,
                    EventPayload = JToken.Parse(@"{""success"":true,""time-since-query-end"":1.2288e-05,""transferred-volume-mb"":0.000495911}"),
                    EventType = "query-result-sent",
                    ProcessId = 12252,
                    RequestId = "2",
                    SessionId = "2",
                    Severity = "info",
                    Site = "-",
                    ThreadId = "4544",
                    Timestamp = DateTime.Parse("2018-07-11T11:02:22"),
                    Username = "tableau_internal_user",
                },
                ExpectedOutput = new HyperEvent()
                {
                    FileName = "hyper_2018_07_12_02_56_45.log",
                    FilePath = "hyper/hyper_2018_07_12_02_56_45.log",
                    Line = 45173,

                    Key = "query-result-sent",
                    ProcessId = 12252,
                    RequestId = "2",
                    SessionId = "2",
                    Severity = "info",
                    Site = "-",
                    ThreadId = "4544",
                    Timestamp = DateTime.Parse("2018-07-11T11:02:22"),
                    User = "tableau_internal_user",
                    Worker = "worker0",

                    Success = true,
                    TimeSinceQueryEnd = 1.2288e-05,
                    TransferredVolumeMb = 0.000495911,
                },
                LineNumber = 45173,
                LogFileInfo = TestLogFileInfo,
                LogType = LogType.Hyper
            },

            new PluginTestCase() { // tcp-ip-client-allowed
                LogContents = new NativeJsonLogsBaseEvent()
                {
                    ArtData = null,
                    EventPayload = JToken.Parse(@"{""local-address"":""::"",""remote-address"":""::1""}"),
                    EventType = "tcp-ip-client-allowed",
                    ProcessId = 12252,
                    RequestId = "2",
                    SessionId = "2",
                    Severity = "info",
                    Site = "-",
                    ThreadId = "4544",
                    Timestamp = DateTime.Parse("2018-07-11T11:02:22"),
                    Username = "tableau_internal_user",
                },
                ExpectedOutput = new HyperEvent()
                {
                    FileName = "hyper_2018_07_12_02_56_45.log",
                    FilePath = "hyper/hyper_2018_07_12_02_56_45.log",
                    Line = 45173,

                    Key = "tcp-ip-client-allowed",
                    ProcessId = 12252,
                    RequestId = "2",
                    SessionId = "2",
                    Severity = "info",
                    Site = "-",
                    ThreadId = "4544",
                    Timestamp = DateTime.Parse("2018-07-11T11:02:22"),
                    User = "tableau_internal_user",
                    Worker = "worker0",

                    RemoteAddress = "::1"
                },
                LineNumber = 45173,
                LogFileInfo = TestLogFileInfo,
                LogType = LogType.Hyper
            },

             new PluginTestCase() { // resource-stats
                LogContents = new NativeJsonLogsBaseEvent()
                {
                    ArtData = null,
                    EventPayload = JToken.Parse(@"{
                        ""memory"":{
                            ""virtual"":{
                                ""total-mb"":36249,
                                ""system-mb"":19688,
                                ""process-mb"":41
                            },
                            ""physical"":{
                                ""total-mb"":32409,
                                ""system-mb"":18037,
                                ""process-mb"":54
                            }
                        },
                        ""mem-tracker"":{
                            ""global"":{
                                ""current-mb"":1,
                                ""peak-mb"":2
                            },
                            ""global_network_readbuffer"":{
                                ""current-mb"":3,
                                ""peak-mb"":4
                            },
                            ""global_network_writebuffer"":{
                                ""current-mb"":5,
                                ""peak-mb"":6
                            },
                            ""global_stringpool"":{
                                ""current-mb"":7,
                                ""peak-mb"":8
                            },
                            ""global_transactions"":{
                                ""current-mb"":9,
                                ""peak-mb"":10
                            },
                            ""global_locked"":{
                                ""current-mb"":11,
                                ""peak-mb"":12
                            },
                            ""global_tuple_data"":{
                                ""current-mb"":13,
                                ""peak-mb"":14
                            },
                            ""global_plan_cache"":{
                                ""current-mb"":15,
                                ""peak-mb"":16
                            },
                            ""global_external_table_cache"":{
                                ""current-mb"":17,
                                ""peak-mb"":18
                            },
                            ""global_disk_network_readbuffer"":{
                                ""current-mb"":19,
                                ""peak-mb"":20
                            },
                            ""global_disk_network_writebuffer"":{
                                ""current-mb"":21,
                                ""peak-mb"":22
                            },
                            ""global_disk_stringpool"":{
                                ""current-mb"":23,
                                ""peak-mb"":24
                            },
                            ""global_disk_transaction"":{
                                ""current-mb"":25,
                                ""peak-mb"":26
                            }
                        }
                    }"),
                    EventType = "resource-stats",
                    ProcessId = 12252,
                    RequestId = "2",
                    SessionId = "2",
                    Severity = "info",
                    Site = "-",
                    ThreadId = "4544",
                    Timestamp = DateTime.Parse("2018-07-11T11:02:22"),
                    Username = "tableau_internal_user",
                },
                ExpectedOutput = new HyperEvent()
                {
                    FileName = "hyper_2018_07_12_02_56_45.log",
                    FilePath = "hyper/hyper_2018_07_12_02_56_45.log",
                    Line = 45173,

                    Key = "resource-stats",
                    ProcessId = 12252,
                    RequestId = "2",
                    SessionId = "2",
                    Severity = "info",
                    Site = "-",
                    ThreadId = "4544",
                    Timestamp = DateTime.Parse("2018-07-11T11:02:22"),
                    User = "tableau_internal_user",
                    Worker = "worker0",

                    VirtualTotalMb = 36249,
                    VirtualSystemMb = 19688,
                    VirtualProcessMb = 41,
                    PhysicalTotalMb = 32409,
                    PhysicalSystemMb = 18037,
                    PhysicalProcessMb = 54,
                    GlobalCurrentMb = 1,
                    GlobalPeakMb = 2,
                    GlobalNetworkReadbufferCurrentMb = 3,
                    GlobalNetworkReadbufferPeakMb = 4,
                    GlobalNetworkWriteBufferCurrentMb = 5,
                    GlobalNetworkWriteBufferPeakMb = 6,
                    GlobalStringpoolCurrentMb = 7,
                    GlobalStringpoolPeakMb = 8,
                    GlobalTransactionsCurrentMb = 9,
                    GlobalTransactionsPeakMb = 10,
                    GlobalLockedCurrentMb = 11,
                    GlobalLockedPeakMb = 12,
                    GlobalTupleDataCurrentMb = 13,
                    GlobalTupleDataPeakMb = 14,
                    GlobalPlanCacheCurrentMb = 15,
                    GlobalPlanCachePeakMb = 16,
                    GlobalExternalTableCacheCurrentMb = 17,
                    GlobalExternalTableCachePeakMb = 18,
                    GlobalDiskNetworkReadbufferCurrentMb = 19,
                    GlobalDiskNetworkReadbufferPeakMb = 20,
                    GlobalDiskNetworkWritebufferCurrentMb = 21,
                    GlobalDiskNetworkWritebufferPeakMb = 22,
                    GlobalDiskStringpoolCurrentMb = 23,
                    GlobalDiskStringpoolPeakMb = 24,
                    GlobalDiskTransactionCurrentMb = 25,
                    GlobalDiskTransactionPeakMb = 26,
                },
                LineNumber = 45173,
                LogFileInfo = TestLogFileInfo,
                LogType = LogType.Hyper
            },
        };
    }
}
