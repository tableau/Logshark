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
                            ""commit-time"": 1.234567,
                            ""pre-execution"": {
                                ""parsing-time"": 0.000158191,
                                ""compilation-time"": 8.9397E-05,
                                ""elapsed"": 0.000262673,
                                ""wait-time-database-lock"": 2.345678,
                                ""processed-rows"": 3.456789,
                                ""processed-rows-byol"": 4.567890,
                                ""processed-rows-file-byol"": 5.678901,
                                ""result-spooling-number-built-chunks"": 6.789012,
                                ""threads"": {
                                    ""thread-time"": 7.890123,
                                    ""cpu-time"": 8.901234,
                                    ""wait-time"": 9.012345
                                },
                                ""peak-transaction-memory-mb"": 10.123456
                            },
                            ""execution"": {
                                ""elapsed"": 11.234567,
                                ""wait-time-objects-lock"": 12.345678,
                                ""wait-time-database-lock"": 13.456789,
                                ""processed-rows"": 14.567890,
                                ""processed-rows-byol"": 15.678901,
                                ""processed-rows-file-byol"": 16.789012,
                                ""result-spooling-number-built-chunks"": 17.890123,
                                ""threads"": {
                                    ""thread-time"": 18.901234,
                                    ""cpu-time"": 19.012345,
                                    ""wait-time"": 20.123456,
                                    ""wait-time-write-buffer-backpressure"": 19.012345
                                },
                                ""peak-transaction-memory-mb"": 20.123456,
                                ""adaptive-compilation"": {
                                    ""compilation-time"": 21.234567,
                                    ""optimized"": {
                                        ""expected"": 22.345678,
                                        ""actual"": 23.456789
                                    }
                                }
                            },
                            ""execution-time"": 2.228E-05,
                            ""time-to-schedule"": 0.00010993,
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
                    FileName = "hyper_2018_07_12_02_56_45.log",
                    FilePath = "hyper/hyper_2018_07_12_02_56_45.log",
                    Key = "query-end",
                    Line = 41,
                    PeakResultBufferMemoryMb = 0.062561d,
                    PeakResultBufferDiskMb = null,
                    PlanCacheHitCount = 0,
                    PlanCacheStatus = "not run yet",
                    ProcessId = 12252,
                    QueryExecutionTime = 2.228E-05,
                    QueryTrunc = "SET SESSIONID=\"F71D8A10BE67431F85AC66F6E6932497-0:0\"",
                    PreExecParsingTime = 0.000158191d,
                    PreExecCompilationTime = 8.9397E-05,
                    PreExecElapsed = 0.000262673d,
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
                    User = "tableau_internal_user",
                    Worker = "worker0",
                    CommitTime = 1.234567,
                    QueryHash = null,
                    Statement = null,
                    PreExecWaitTimeDBLock = 2.345678,
                    PreExecProcessedRows = 3.456789,
                    PreExecProcessedRowsBYOL = 4.567890,
                    PreExecProcessedRowsFileBYOL = 5.678901,
                    PreExecResultSpoolingNumBuiltChunks = 6.789012,
                    PreExecThreadTime = 7.890123,
                    PreExecThreadsCPUTime = 8.901234,
                    PreExecThreadsWaitTime = 9.012345,
                    PreExecPeakTransactionMemMb = 10.123456,
                    ExecElapsed = 11.234567,
                    ExecWaitTimeObjLock = 12.345678,
                    ExecyWaitTimeDBLock = 13.456789,
                    ExecProcessedRows = 14.567890,
                    ExecProcessedRowsBYOL = 15.678901,
                    ExecProcessedRowsFileBYOL = 16.789012,
                    ExecResultSpoolingNumBuiltChunks = 17.890123,
                    ExecThreadTime = 18.901234,
                    ExecThreadsCpuTime = 19.012345,
                    ExecThreadsWaitTime = 20.123456,
                    ExecThreadsWaitTimeBuffBackPressure = 19.012345,
                    ExecPeakTransactionMemMb = 20.123456,
                    ExecAdaptiveCompilationTime = 21.234567,
                    ExecAdaptiveCompilationOptExpected = 22.345678,
                    ExecAdaptiveCompilationOptActual = 23.456789,
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
                            ""commit-time"": 24.567890,
                            ""pre-execution"": {
                                ""parsing-time"": 0.000158191,
                                ""compilation-time"": 8.9397E-05,
                                ""elapsed"": 0.000262673,
                                ""wait-time-database-lock"": 25.678901,
                                ""processed-rows"": 26.789012,
                                ""processed-rows-byol"": 27.890123,
                                ""processed-rows-file-byol"": 28.901234,
                                ""result-spooling-number-built-chunks"": 29.012345,
                                ""threads"": {
                                    ""thread-time"": 30.123456,
                                    ""cpu-time"": 31.234567,
                                    ""wait-time"": 32.345678
                                },
                                ""peak-transaction-memory-mb"": 33.456789
                            },
                            ""execution"": {
                                ""elapsed"": 34.567890,
                                ""wait-time-objects-lock"": 35.678901,
                                ""wait-time-database-lock"": 36.789012,
                                ""processed-rows"": 37.890123,
                                ""processed-rows-byol"": 38.901234,
                                ""processed-rows-file-byol"": 39.012345,
                                ""result-spooling-number-built-chunks"": 40.123456,
                                ""threads"": {
                                    ""thread-time"": 41.234567,
                                    ""cpu-time"": 42.345678,
                                    ""wait-time"": 43.456789,
                                    ""wait-time-write-buffer-backpressure"": 42.345678
                                },
                                ""peak-transaction-memory-mb"": 43.456789,
                                ""adaptive-compilation"": {
                                    ""compilation-time"": 44.567890,
                                    ""optimized"": {
                                        ""expected"": 45.678901,
                                        ""actual"": 46.789012
                                    }
                                }
                            },
                            ""execution-time"": 2.228E-05,
                            ""time-to-schedule"": 0.00010993,
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
                    FileName = "hyper_2018_07_12_02_56_45.log",
                    FilePath = "hyper/hyper_2018_07_12_02_56_45.log",
                    Key = "query-end",
                    Line = 41,
                    PeakResultBufferMemoryMb = 0.062561d,
                    PeakResultBufferDiskMb = null,
                    PlanCacheHitCount = 0,
                    PlanCacheStatus = "not run yet",
                    ProcessId = 12252,
                    QueryExecutionTime = 2.228E-05,
                    QueryTrunc = "SET SESSIONID=\"F71D8A10BE67431F85AC66F6E6932497-0:0\"",
                    PreExecParsingTime = 0.000158191d,
                    PreExecCompilationTime = 8.9397E-05,
                    PreExecElapsed = 0.000262673d,
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
                    User = "tableau_internal_user",
                    Worker = "worker0",
                    CommitTime = 24.567890,
                    QueryHash = null,
                    Statement = null,
                    PreExecWaitTimeDBLock = 25.678901,
                    PreExecProcessedRows = 26.789012,
                    PreExecProcessedRowsBYOL = 27.890123,
                    PreExecProcessedRowsFileBYOL = 28.901234,
                    PreExecResultSpoolingNumBuiltChunks = 29.012345,
                    PreExecThreadTime = 30.123456,
                    PreExecThreadsCPUTime = 31.234567,
                    PreExecThreadsWaitTime = 32.345678,
                    PreExecPeakTransactionMemMb = 33.456789,
                    ExecElapsed = 34.567890,
                    ExecWaitTimeObjLock = 35.678901,
                    ExecyWaitTimeDBLock = 36.789012,
                    ExecProcessedRows = 37.890123,
                    ExecProcessedRowsBYOL = 38.901234,
                    ExecProcessedRowsFileBYOL = 39.012345,
                    ExecResultSpoolingNumBuiltChunks = 40.123456,
                    ExecThreadTime = 41.234567,
                    ExecThreadsCpuTime = 42.345678,
                    ExecThreadsWaitTime = 43.456789,
                    ExecThreadsWaitTimeBuffBackPressure = 42.345678,
                    ExecPeakTransactionMemMb = 43.456789,
                    ExecAdaptiveCompilationTime = 44.567890,
                    ExecAdaptiveCompilationOptExpected = 45.678901,
                    ExecAdaptiveCompilationOptActual = 46.789012,
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
                        ""commit-time"":47.890123,
                        ""pre-execution"": {
                            ""parsing-time"": 0.0,
                            ""compilation-time"": 0.0,
                            ""elapsed"": 0.0,
                            ""wait-time-database-lock"": 48.901234,
                            ""processed-rows"": 49.012345,
                            ""processed-rows-byol"": 50.123456,
                            ""processed-rows-file-byol"": 51.234567,
                            ""result-spooling-number-built-chunks"": 52.345678,
                            ""threads"": {
                                ""thread-time"": 53.456789,
                                ""cpu-time"": 54.567890,
                                ""wait-time"": 55.678901
                            },
                            ""peak-transaction-memory-mb"": 56.789012
                        },
                        ""execution"": {
                            ""elapsed"": 57.890123,
                            ""wait-time-objects-lock"": 58.901234,
                            ""wait-time-database-lock"": 59.012345,
                            ""processed-rows"": 60.123456,
                            ""processed-rows-byol"": 61.234567,
                            ""processed-rows-file-byol"": 62.345678,
                            ""result-spooling-number-built-chunks"": 63.456789,
                            ""threads"": {
                                ""thread-time"": 64.567890,
                                ""cpu-time"": 65.678901,
                                ""wait-time"": 66.789012,
                                ""wait-time-write-buffer-backpressure"": 65.678901
                            },
                            ""peak-transaction-memory-mb"": 66.789012,
                            ""adaptive-compilation"": {
                                ""compilation-time"": 67.890123,
                                ""optimized"": {
                                    ""expected"": 68.901234,
                                    ""actual"": 69.012345
                                }
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
                    FileName = "hyper_2018_07_12_02_56_45.log",
                    FilePath = "hyper/hyper_2018_07_12_02_56_45.log",
                    Key = "query-end-cancelled",
                    Line = 45173,
                    PeakResultBufferMemoryMb = 0,
                    PeakResultBufferDiskMb = null,
                    PlanCacheHitCount = 0,
                    PlanCacheStatus = "cache miss",
                    ProcessId = 12252,
                    QueryExecutionTime = 947.971,
                    QueryTrunc = "INSERT BULK INTO <truncated>",
                    QuerySettingsActive = false,
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
                    User = "tableau_internal_user",
                    Worker = "worker0",
                    CommitTime = 47.890123,
                    QueryHash = null,
                    Statement = null,
                    PreExecParsingTime = 0.0,
                    PreExecCompilationTime = 0.0,
                    PreExecElapsed = 0.0,
                    PreExecWaitTimeDBLock = 48.901234,
                    PreExecProcessedRows = 49.012345,
                    PreExecProcessedRowsBYOL = 50.123456,
                    PreExecProcessedRowsFileBYOL = 51.234567,
                    PreExecResultSpoolingNumBuiltChunks = 52.345678,
                    PreExecThreadTime = 53.456789,
                    PreExecThreadsCPUTime = 54.567890,
                    PreExecThreadsWaitTime = 55.678901,
                    PreExecPeakTransactionMemMb = 56.789012,
                    ExecElapsed = 57.890123,
                    ExecWaitTimeObjLock = 58.901234,
                    ExecyWaitTimeDBLock = 59.012345,
                    ExecProcessedRows = 60.123456,
                    ExecProcessedRowsBYOL = 61.234567,
                    ExecProcessedRowsFileBYOL = 62.345678,
                    ExecResultSpoolingNumBuiltChunks = 63.456789,
                    ExecThreadTime = 64.567890,
                    ExecThreadsCpuTime = 65.678901,
                    ExecThreadsWaitTime = 66.789012,
                    ExecThreadsWaitTimeBuffBackPressure = 65.678901,
                    ExecPeakTransactionMemMb = 66.789012,
                    ExecAdaptiveCompilationTime = 67.890123,
                    ExecAdaptiveCompilationOptExpected = 68.901234,
                    ExecAdaptiveCompilationOptActual = 69.012345,
                    StorageAccessTime = null,
                    StorageAccessCount = null,
                    StorageAccessBytes = null,
                    StorageWorkerBlockedTime = null,
                    StorageWorkerBlockedCount = null,
                    StorageCacheHitCount = null,
                    StorageCacheHitBytes = null,
                    StorageCacheBytesSaved = null,
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
                    CommitTime = null,
                    QueryHash = null,
                    Statement = null,
                    PreExecParsingTime = 0.0,
                    PreExecCompilationTime = 0.0,
                    PreExecElapsed = 0.0,
                    PreExecWaitTimeDBLock = 0.0,
                    PreExecProcessedRows = 0.0,
                    PreExecProcessedRowsBYOL = 0.0,
                    PreExecProcessedRowsFileBYOL = 0.0,
                    PreExecResultSpoolingNumBuiltChunks = 0.0,
                    PreExecThreadTime = 0.0,
                    PreExecThreadsCPUTime = 0.0,
                    PreExecThreadsWaitTime = 0.0,
                    PreExecPeakTransactionMemMb = 0.0,
                    ExecElapsed = 0.0,
                    ExecWaitTimeObjLock = 0.0,
                    ExecyWaitTimeDBLock = 0.0,
                    ExecProcessedRows = 0.0,
                    ExecProcessedRowsBYOL = 0.0,
                    ExecProcessedRowsFileBYOL = 0.0,
                    ExecResultSpoolingNumBuiltChunks = 0.0,
                    ExecThreadTime = 0.0,
                    ExecThreadsCpuTime = 0.0,
                    ExecThreadsWaitTime = 0.0,
                    ExecThreadsWaitTimeBuffBackPressure = 0.0,
                    ExecPeakTransactionMemMb = 0.0,
                    ExecAdaptiveCompilationTime = 0.0,
                    ExecAdaptiveCompilationOptExpected = 0.0,
                    ExecAdaptiveCompilationOptActual = 0.0,
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
                    CommitTime = null,
                    QueryHash = null,
                    Statement = null,
                    PreExecParsingTime = 0.0,
                    PreExecCompilationTime = 0.0,
                    PreExecElapsed = 0.0,
                    PreExecWaitTimeDBLock = 0.0,
                    PreExecProcessedRows = 0.0,
                    PreExecProcessedRowsBYOL = 0.0,
                    PreExecProcessedRowsFileBYOL = 0.0,
                    PreExecResultSpoolingNumBuiltChunks = 0.0,
                    PreExecThreadTime = 0.0,
                    PreExecThreadsCPUTime = 0.0,
                    PreExecThreadsWaitTime = 0.0,
                    PreExecPeakTransactionMemMb = 0.0,
                    ExecElapsed = 0.0,
                    ExecWaitTimeObjLock = 0.0,
                    ExecyWaitTimeDBLock = 0.0,
                    ExecProcessedRows = 0.0,
                    ExecProcessedRowsBYOL = 0.0,
                    ExecProcessedRowsFileBYOL = 0.0,
                    ExecResultSpoolingNumBuiltChunks = 0.0,
                    ExecThreadTime = 0.0,
                    ExecThreadsCpuTime = 0.0,
                    ExecThreadsWaitTime = 0.0,
                    ExecThreadsWaitTimeBuffBackPressure = 0.0,
                    ExecPeakTransactionMemMb = 0.0,
                    ExecAdaptiveCompilationTime = 0.0,
                    ExecAdaptiveCompilationOptExpected = 0.0,
                    ExecAdaptiveCompilationOptActual = 0.0,
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
                        ""commit-time"":70.123456,
                        ""pre-execution"": {
                            ""parsing-time"": 0.0,
                            ""compilation-time"": 0.0,
                            ""elapsed"": 0.0,
                            ""wait-time-database-lock"": 71.234567,
                            ""processed-rows"": 72.345678,
                            ""processed-rows-byol"": 73.456789,
                            ""processed-rows-file-byol"": 74.567890,
                            ""result-spooling-number-built-chunks"": 75.678901,
                            ""threads"": {
                                ""thread-time"": 76.789012,
                                ""cpu-time"": 77.890123,
                                ""wait-time"": 78.901234
                            },
                            ""peak-transaction-memory-mb"": 79.012345
                        },
                        ""execution"": {
                            ""elapsed"": 80.123456,
                            ""wait-time-objects-lock"": 81.234567,
                            ""wait-time-database-lock"": 82.345678,
                            ""processed-rows"": 83.456789,
                            ""processed-rows-byol"": 84.567890,
                            ""processed-rows-file-byol"": 85.678901,
                            ""result-spooling-number-built-chunks"": 86.789012,
                            ""threads"": {
                                ""thread-time"": 87.890123,
                                ""cpu-time"": 88.901234,
                                ""wait-time"": 89.012345,
                                ""wait-time-write-buffer-backpressure"": 88.901234
                            },
                            ""peak-transaction-memory-mb"": 89.012345,
                            ""adaptive-compilation"": {
                                ""compilation-time"": 90.123456,
                                ""optimized"": {
                                    ""expected"": 91.234567,
                                    ""actual"": 92.345678
                                }
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
                    FileName = "hyper_2018_07_12_02_56_45.log",
                    FilePath = "hyper/hyper_2018_07_12_02_56_45.log",
                    Key = "query-end-canceled",
                    Line = 45173,
                    PeakResultBufferMemoryMb = 0,
                    PeakResultBufferDiskMb = null,
                    PlanCacheHitCount = 0,
                    PlanCacheStatus = "cache miss",
                    ProcessId = 12252,
                    QueryExecutionTime = 947.971,
                    QueryTrunc = "INSERT BULK INTO <truncated>",
                    QuerySettingsActive = false,
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
                    User = "tableau_internal_user",
                    Worker = "worker0",
                    CommitTime = 70.123456,
                    QueryHash = null,
                    Statement = null,
                    PreExecParsingTime = 0.0,
                    PreExecCompilationTime = 0.0,
                    PreExecElapsed = 0.0,
                    PreExecWaitTimeDBLock = 71.234567,
                    PreExecProcessedRows = 72.345678,
                    PreExecProcessedRowsBYOL = 73.456789,
                    PreExecProcessedRowsFileBYOL = 74.567890,
                    PreExecResultSpoolingNumBuiltChunks = 75.678901,
                    PreExecThreadTime = 76.789012,
                    PreExecThreadsCPUTime = 77.890123,
                    PreExecThreadsWaitTime = 78.901234,
                    PreExecPeakTransactionMemMb = 79.012345,
                    ExecElapsed = 80.123456,
                    ExecWaitTimeObjLock = 81.234567,
                    ExecyWaitTimeDBLock = 82.345678,
                    ExecProcessedRows = 83.456789,
                    ExecProcessedRowsBYOL = 84.567890,
                    ExecProcessedRowsFileBYOL = 85.678901,
                    ExecResultSpoolingNumBuiltChunks = 86.789012,
                    ExecThreadTime = 87.890123,
                    ExecThreadsCpuTime = 88.901234,
                    ExecThreadsWaitTime = 89.012345,
                    ExecThreadsWaitTimeBuffBackPressure = 88.901234,
                    ExecPeakTransactionMemMb = 89.012345,
                    ExecAdaptiveCompilationTime = 90.123456,
                    ExecAdaptiveCompilationOptExpected = 91.234567,
                    ExecAdaptiveCompilationOptActual = 92.345678,
                    StorageAccessTime = null,
                    StorageAccessCount = null,
                    StorageAccessBytes = null,
                    StorageWorkerBlockedTime = null,
                    StorageWorkerBlockedCount = null,
                    StorageCacheHitCount = null,
                    StorageCacheHitBytes = null,
                    StorageCacheBytesSaved = null,
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
                        ""options"":{}
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
                    Options =@"{}" ,
                    CommitTime = null,
                    QueryHash = null,
                    Statement = null,
                    PreExecParsingTime = 0.0,
                    PreExecCompilationTime = 0.0,
                    PreExecElapsed = 0.0,
                    PreExecWaitTimeDBLock = 0.0,
                    PreExecProcessedRows = 0.0,
                    PreExecProcessedRowsBYOL = 0.0,
                    PreExecProcessedRowsFileBYOL = 0.0,
                    PreExecResultSpoolingNumBuiltChunks = 0.0,
                    PreExecThreadTime = 0.0,
                    PreExecThreadsCPUTime = 0.0,
                    PreExecThreadsWaitTime = 0.0,
                    PreExecPeakTransactionMemMb = 0.0,
                    ExecElapsed = 0.0,
                    ExecWaitTimeObjLock = 0.0,
                    ExecyWaitTimeDBLock = 0.0,
                    ExecProcessedRows = 0.0,
                    ExecProcessedRowsBYOL = 0.0,
                    ExecProcessedRowsFileBYOL = 0.0,
                    ExecResultSpoolingNumBuiltChunks = 0.0,
                    ExecThreadTime = 0.0,
                    ExecThreadsCpuTime = 0.0,
                    ExecThreadsWaitTime = 0.0,
                    ExecThreadsWaitTimeBuffBackPressure = 0.0,
                    ExecPeakTransactionMemMb = 0.0,
                    ExecAdaptiveCompilationTime = 0.0,
                    ExecAdaptiveCompilationOptExpected = 0.0,
                    ExecAdaptiveCompilationOptActual = 0.0,
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
                    CommitTime = null,
                    QueryHash = null,
                    Statement = null,
                    PreExecParsingTime = 0.0,
                    PreExecCompilationTime = 0.0,
                    PreExecElapsed = 0.0,
                    PreExecWaitTimeDBLock = 0.0,
                    PreExecProcessedRows = 0.0,
                    PreExecProcessedRowsBYOL = 0.0,
                    PreExecProcessedRowsFileBYOL = 0.0,
                    PreExecResultSpoolingNumBuiltChunks = 0.0,
                    PreExecThreadTime = 0.0,
                    PreExecThreadsCPUTime = 0.0,
                    PreExecThreadsWaitTime = 0.0,
                    PreExecPeakTransactionMemMb = 0.0,
                    ExecElapsed = 0.0,
                    ExecWaitTimeObjLock = 0.0,
                    ExecyWaitTimeDBLock = 0.0,
                    ExecProcessedRows = 0.0,
                    ExecProcessedRowsBYOL = 0.0,
                    ExecProcessedRowsFileBYOL = 0.0,
                    ExecResultSpoolingNumBuiltChunks = 0.0,
                    ExecThreadTime = 0.0,
                    ExecThreadsCpuTime = 0.0,
                    ExecThreadsWaitTime = 0.0,
                    ExecThreadsWaitTimeBuffBackPressure = 0.0,
                    ExecPeakTransactionMemMb = 0.0,
                    ExecAdaptiveCompilationTime = 0.0,
                    ExecAdaptiveCompilationOptExpected = 0.0,
                    ExecAdaptiveCompilationOptActual = 0.0,
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
                    CommitTime = null,
                    QueryHash = null,
                    Statement = null,
                    PreExecParsingTime = 0.0,
                    PreExecCompilationTime = 0.0,
                    PreExecElapsed = 0.0,
                    PreExecWaitTimeDBLock = 0.0,
                    PreExecProcessedRows = 0.0,
                    PreExecProcessedRowsBYOL = 0.0,
                    PreExecProcessedRowsFileBYOL = 0.0,
                    PreExecResultSpoolingNumBuiltChunks = 0.0,
                    PreExecThreadTime = 0.0,
                    PreExecThreadsCPUTime = 0.0,
                    PreExecThreadsWaitTime = 0.0,
                    PreExecPeakTransactionMemMb = 0.0,
                    ExecElapsed = 0.0,
                    ExecWaitTimeObjLock = 0.0,
                    ExecyWaitTimeDBLock = 0.0,
                    ExecProcessedRows = 0.0,
                    ExecProcessedRowsBYOL = 0.0,
                    ExecProcessedRowsFileBYOL = 0.0,
                    ExecResultSpoolingNumBuiltChunks = 0.0,
                    ExecThreadTime = 0.0,
                    ExecThreadsCpuTime = 0.0,
                    ExecThreadsWaitTime = 0.0,
                    ExecThreadsWaitTimeBuffBackPressure = 0.0,
                    ExecPeakTransactionMemMb = 0.0,
                    ExecAdaptiveCompilationTime = 0.0,
                    ExecAdaptiveCompilationOptExpected = 0.0,
                    ExecAdaptiveCompilationOptActual = 0.0,
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

                    Reason = "client terminated",
                    CommitTime = null,
                    QueryHash = null,
                    Statement = null,
                    PreExecParsingTime = 0.0,
                    PreExecCompilationTime = 0.0,
                    PreExecElapsed = 0.0,
                    PreExecWaitTimeDBLock = 0.0,
                    PreExecProcessedRows = 0.0,
                    PreExecProcessedRowsBYOL = 0.0,
                    PreExecProcessedRowsFileBYOL = 0.0,
                    PreExecResultSpoolingNumBuiltChunks = 0.0,
                    PreExecThreadTime = 0.0,
                    PreExecThreadsCPUTime = 0.0,
                    PreExecThreadsWaitTime = 0.0,
                    PreExecPeakTransactionMemMb = 0.0,
                    ExecElapsed = 0.0,
                    ExecWaitTimeObjLock = 0.0,
                    ExecyWaitTimeDBLock = 0.0,
                    ExecProcessedRows = 0.0,
                    ExecProcessedRowsBYOL = 0.0,
                    ExecProcessedRowsFileBYOL = 0.0,
                    ExecResultSpoolingNumBuiltChunks = 0.0,
                    ExecThreadTime = 0.0,
                    ExecThreadsCpuTime = 0.0,
                    ExecThreadsWaitTime = 0.0,
                    ExecThreadsWaitTimeBuffBackPressure = 0.0,
                    ExecPeakTransactionMemMb = 0.0,
                    ExecAdaptiveCompilationTime = 0.0,
                    ExecAdaptiveCompilationOptExpected = 0.0,
                    ExecAdaptiveCompilationOptActual = 0.0,
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
                        ""elapsed"":1.8e-06,
                        ""elapsed-registry-insert"":99.012345,
                        ""reopen"":false,
                        ""load-success"":true,
                        ""database-uuid"":""test-uuid-12345""
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
                    ElapseRegistryInsert = 99.012345,
                    Reopen = false,
                    LoadSuccess = true,
                    DatabaseUuid = "test-uuid-12345",
                    PreExecParsingTime = 0.0,
                    PreExecCompilationTime = 0.0,
                    PreExecElapsed = 0.0,
                    PreExecWaitTimeDBLock = 0.0,
                    PreExecProcessedRows = 0.0,
                    PreExecProcessedRowsBYOL = 0.0,
                    PreExecProcessedRowsFileBYOL = 0.0,
                    PreExecResultSpoolingNumBuiltChunks = 0.0,
                    PreExecThreadTime = 0.0,
                    PreExecThreadsCPUTime = 0.0,
                    PreExecThreadsWaitTime = 0.0,
                    PreExecPeakTransactionMemMb = 0.0,
                    ExecElapsed = 0.0,
                    ExecWaitTimeObjLock = 0.0,
                    ExecyWaitTimeDBLock = 0.0,
                    ExecProcessedRows = 0.0,
                    ExecProcessedRowsBYOL = 0.0,
                    ExecProcessedRowsFileBYOL = 0.0,
                    ExecResultSpoolingNumBuiltChunks = 0.0,
                    ExecThreadTime = 0.0,
                    ExecThreadsCpuTime = 0.0,
                    ExecThreadsWaitTime = 0.0,
                    ExecThreadsWaitTimeBuffBackPressure = 0.0,
                    ExecPeakTransactionMemMb = 0.0,
                    ExecAdaptiveCompilationTime = 0.0,
                    ExecAdaptiveCompilationOptExpected = 0.0,
                    ExecAdaptiveCompilationOptActual = 0.0,
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
                        ""elapsed"":0.00250188,
                        ""new-ref-count"":12345
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
                    Elapsed = 0.00250188,
                    NewRefCount = 12345,
                    PreExecParsingTime = 0.0,
                    PreExecCompilationTime = 0.0,
                    PreExecElapsed = 0.0,
                    PreExecWaitTimeDBLock = 0.0,
                    PreExecProcessedRows = 0.0,
                    PreExecProcessedRowsBYOL = 0.0,
                    PreExecProcessedRowsFileBYOL = 0.0,
                    PreExecResultSpoolingNumBuiltChunks = 0.0,
                    PreExecThreadTime = 0.0,
                    PreExecThreadsCPUTime = 0.0,
                    PreExecThreadsWaitTime = 0.0,
                    PreExecPeakTransactionMemMb = 0.0,
                    ExecElapsed = 0.0,
                    ExecWaitTimeObjLock = 0.0,
                    ExecyWaitTimeDBLock = 0.0,
                    ExecProcessedRows = 0.0,
                    ExecProcessedRowsBYOL = 0.0,
                    ExecProcessedRowsFileBYOL = 0.0,
                    ExecResultSpoolingNumBuiltChunks = 0.0,
                    ExecThreadTime = 0.0,
                    ExecThreadsCpuTime = 0.0,
                    ExecThreadsWaitTime = 0.0,
                    ExecThreadsWaitTimeBuffBackPressure = 0.0,
                    ExecPeakTransactionMemMb = 0.0,
                    ExecAdaptiveCompilationTime = 0.0,
                    ExecAdaptiveCompilationOptExpected = 0.0,
                    ExecAdaptiveCompilationOptActual = 0.0,
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
                    CommitTime = null,
                    QueryHash = null,
                    Statement = null,
                    PreExecParsingTime = 0.0,
                    PreExecCompilationTime = 0.0,
                    PreExecElapsed = 0.0,
                    PreExecWaitTimeDBLock = 0.0,
                    PreExecProcessedRows = 0.0,
                    PreExecProcessedRowsBYOL = 0.0,
                    PreExecProcessedRowsFileBYOL = 0.0,
                    PreExecResultSpoolingNumBuiltChunks = 0.0,
                    PreExecThreadTime = 0.0,
                    PreExecThreadsCPUTime = 0.0,
                    PreExecThreadsWaitTime = 0.0,
                    PreExecPeakTransactionMemMb = 0.0,
                    ExecElapsed = 0.0,
                    ExecWaitTimeObjLock = 0.0,
                    ExecyWaitTimeDBLock = 0.0,
                    ExecProcessedRows = 0.0,
                    ExecProcessedRowsBYOL = 0.0,
                    ExecProcessedRowsFileBYOL = 0.0,
                    ExecResultSpoolingNumBuiltChunks = 0.0,
                    ExecThreadTime = 0.0,
                    ExecThreadsCpuTime = 0.0,
                    ExecThreadsWaitTime = 0.0,
                    ExecThreadsWaitTimeBuffBackPressure = 0.0,
                    ExecPeakTransactionMemMb = 0.0,
                    ExecAdaptiveCompilationTime = 0.0,
                    ExecAdaptiveCompilationOptExpected = 0.0,
                    ExecAdaptiveCompilationOptActual = 0.0,
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

                    RemoteAddress = "::1",
                    CommitTime = null,
                    QueryHash = null,
                    Statement = null,
                    PreExecParsingTime = 0.0,
                    PreExecCompilationTime = 0.0,
                    PreExecElapsed = 0.0,
                    PreExecWaitTimeDBLock = 0.0,
                    PreExecProcessedRows = 0.0,
                    PreExecProcessedRowsBYOL = 0.0,
                    PreExecProcessedRowsFileBYOL = 0.0,
                    PreExecResultSpoolingNumBuiltChunks = 0.0,
                    PreExecThreadTime = 0.0,
                    PreExecThreadsCPUTime = 0.0,
                    PreExecThreadsWaitTime = 0.0,
                    PreExecPeakTransactionMemMb = 0.0,
                    ExecElapsed = 0.0,
                    ExecWaitTimeObjLock = 0.0,
                    ExecyWaitTimeDBLock = 0.0,
                    ExecProcessedRows = 0.0,
                    ExecProcessedRowsBYOL = 0.0,
                    ExecProcessedRowsFileBYOL = 0.0,
                    ExecResultSpoolingNumBuiltChunks = 0.0,
                    ExecThreadTime = 0.0,
                    ExecThreadsCpuTime = 0.0,
                    ExecThreadsWaitTime = 0.0,
                    ExecThreadsWaitTimeBuffBackPressure = 0.0,
                    ExecPeakTransactionMemMb = 0.0,
                    ExecAdaptiveCompilationTime = 0.0,
                    ExecAdaptiveCompilationOptExpected = 0.0,
                    ExecAdaptiveCompilationOptActual = 0.0,
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
                    PreExecParsingTime = 0.0,
                    PreExecCompilationTime = 0.0,
                    PreExecElapsed = 0.0,
                    PreExecWaitTimeDBLock = 0.0,
                    PreExecProcessedRows = 0.0,
                    PreExecProcessedRowsBYOL = 0.0,
                    PreExecProcessedRowsFileBYOL = 0.0,
                    PreExecResultSpoolingNumBuiltChunks = 0.0,
                    PreExecThreadTime = 0.0,
                    PreExecThreadsCPUTime = 0.0,
                    PreExecThreadsWaitTime = 0.0,
                    PreExecPeakTransactionMemMb = 0.0,
                    ExecElapsed = 0.0,
                    ExecWaitTimeObjLock = 0.0,
                    ExecyWaitTimeDBLock = 0.0,
                    ExecProcessedRows = 0.0,
                    ExecProcessedRowsBYOL = 0.0,
                    ExecProcessedRowsFileBYOL = 0.0,
                    ExecResultSpoolingNumBuiltChunks = 0.0,
                    ExecThreadTime = 0.0,
                    ExecThreadsCpuTime = 0.0,
                    ExecThreadsWaitTime = 0.0,
                    ExecThreadsWaitTimeBuffBackPressure = 0.0,
                    ExecPeakTransactionMemMb = 0.0,
                    ExecAdaptiveCompilationTime = 0.0,
                    ExecAdaptiveCompilationOptExpected = 0.0,
                    ExecAdaptiveCompilationOptActual = 0.0,
                },
                LineNumber = 45173,
                LogFileInfo = TestLogFileInfo,
                LogType = LogType.Hyper
            },
        };
    }
}
