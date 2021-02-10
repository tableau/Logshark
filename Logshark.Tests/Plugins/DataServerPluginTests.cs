using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using LogShark.Exceptions;
using LogShark.Plugins.DataServer;
using LogShark.Plugins.DataServer.Model;
using LogShark.Shared;
using LogShark.Shared.LogReading.Containers;
using LogShark.Tests.Extensions;
using LogShark.Tests.Plugins.Helpers;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json.Linq;
using Xunit;

namespace LogShark.Tests.Plugins
{
    public class DataServerPluginTests
    {
        private static readonly LogFileInfo CppTestLogFileInfo = new LogFileInfo("testCpp.log", @"folder1/testCpp.log", "node1", DateTime.MinValue);
        private static readonly LogFileInfo JavaTestLogFileInfo = new LogFileInfo("testJava.log", @"folder1/testJava.log", "node1", DateTime.MinValue);

        private readonly ProcessingNotificationsCollector _processingNotificationsCollector;

        public DataServerPluginTests()
        {
            _processingNotificationsCollector = new ProcessingNotificationsCollector(10);
        }

        [Fact]
        public void BadCppInput()
        {
            var testWriterFactory = new TestWriterFactory();
            using (var plugin = new DataServerPlugin())
            {
                plugin.Configure(testWriterFactory, null, _processingNotificationsCollector, new NullLoggerFactory());
                
                var wrongContentFormat = new LogLine(new ReadLogLineResult(123, "Plugin doesn't expect string"), CppTestLogFileInfo);
                var wrongContentFormat2 = new LogLine(new ReadLogLineResult(123, 123), CppTestLogFileInfo);
                var nullContent = new LogLine(new ReadLogLineResult(123, null), CppTestLogFileInfo);
                var noEventPayload = new LogLine(new ReadLogLineResult(123, new NativeJsonLogsBaseEvent { EventType = "something else"}), CppTestLogFileInfo);

                plugin.ProcessLogLine(wrongContentFormat, LogType.DataserverCpp);
                plugin.ProcessLogLine(wrongContentFormat2, LogType.DataserverCpp);
                plugin.ProcessLogLine(nullContent, LogType.DataserverCpp);
                plugin.ProcessLogLine(noEventPayload, LogType.DataserverCpp);

                plugin.CompleteProcessing();
            }

            testWriterFactory.AssertAllWritersAreDisposedAndEmpty(1);
            _processingNotificationsCollector.TotalErrorsReported.Should().Be(4);
        }

        [Fact]
        public void NonActionableCppInput()
        {
            var testWriterFactory = new TestWriterFactory();
            using (var plugin = new DataServerPlugin())
            {
                plugin.Configure(testWriterFactory, null, _processingNotificationsCollector, new NullLoggerFactory());
                
                var skipEvent1 = new LogLine(new ReadLogLineResult(123, new NativeJsonLogsBaseEvent { EventType = "msg", EventPayload = "ACTION: Lock Data Server session"}), CppTestLogFileInfo);
                var skipEvent2 = new LogLine(new ReadLogLineResult(123, new NativeJsonLogsBaseEvent { EventType = "msg", EventPayload = "ACTION: Lock Data Server session. Should use StartsWith statement"}), CppTestLogFileInfo);
                var skipEvent3 = new LogLine(new ReadLogLineResult(123, new NativeJsonLogsBaseEvent { EventType = "msg", EventPayload = "ACTION: Unlock Data Server session"}), CppTestLogFileInfo);
                var skipEvent4 = new LogLine(new ReadLogLineResult(123, new NativeJsonLogsBaseEvent { EventType = "msg", EventPayload = "ACTION: Unlock Data Server session. Should use StartsWith statement"}), CppTestLogFileInfo);

                plugin.ProcessLogLine(skipEvent1, LogType.DataserverCpp);
                plugin.ProcessLogLine(skipEvent2, LogType.DataserverCpp);
                plugin.ProcessLogLine(skipEvent3, LogType.DataserverCpp);
                plugin.ProcessLogLine(skipEvent4, LogType.DataserverCpp);

                plugin.CompleteProcessing();
            }

            testWriterFactory.AssertAllWritersAreDisposedAndEmpty(1);
            _processingNotificationsCollector.TotalErrorsReported.Should().Be(0);
        }
        
        [Fact]
        public void BadJavaInput()
        {
            var testWriterFactory = new TestWriterFactory();
            using (var plugin = new DataServerPlugin())
            {
                plugin.Configure(testWriterFactory, null, _processingNotificationsCollector, new NullLoggerFactory());
                
                var wrongContentFormat = new LogLine(new ReadLogLineResult(123, 1234), JavaTestLogFileInfo);
                var nullContent = new LogLine(new ReadLogLineResult(123, null), JavaTestLogFileInfo);
                var emptyContent = new LogLine(new ReadLogLineResult(123, ""), JavaTestLogFileInfo);
                var wrongLineContent = new LogLine(new ReadLogLineResult(123, "Not a log line"), JavaTestLogFileInfo);
                
                plugin.ProcessLogLine(wrongContentFormat, LogType.DataserverJava);
                plugin.ProcessLogLine(nullContent, LogType.DataserverJava);
                plugin.ProcessLogLine(emptyContent, LogType.DataserverJava);
                plugin.ProcessLogLine(wrongLineContent, LogType.DataserverJava);
                
                plugin.CompleteProcessing();
            }

            testWriterFactory.AssertAllWritersAreDisposedAndEmpty(1);
            _processingNotificationsCollector.TotalErrorsReported.Should().Be(4);
        }

        [Fact]
        public void UnsupportedLogType()
        {
            var testWriterFactory = new TestWriterFactory();
            using (var plugin = new DataServerPlugin())
            {
                plugin.Configure(testWriterFactory, null, _processingNotificationsCollector, new NullLoggerFactory());
                
                var someLogLine = new LogLine(new ReadLogLineResult(123, "Plugin should not even get to point where it inspects this line"), JavaTestLogFileInfo);
                Action wrongLogTypeAction = () => { plugin.ProcessLogLine(someLogLine, LogType.Apache);};
                wrongLogTypeAction.Should().Throw<LogSharkProgramLogicException>();
                
                plugin.CompleteProcessing();
            }

            testWriterFactory.AssertAllWritersAreDisposedAndEmpty(1);
            _processingNotificationsCollector.TotalErrorsReported.Should().Be(0);
        }

        [Fact]
        public void CppBasicTestCases()
        {
            var testWriterFactory = new TestWriterFactory();
            using (var plugin = new DataServerPlugin())
            {
                plugin.Configure(testWriterFactory, null, _processingNotificationsCollector, new NullLoggerFactory());

                foreach (var testCase in _cppBasicTestCases)
                {
                    var logLine = testCase.GetLogLine();
                    plugin.ProcessLogLine(logLine, LogType.DataserverCpp);
                }

                plugin.CompleteProcessing();
            }

            var expectedOutput = _cppBasicTestCases.Select(testCase => testCase.ExpectedOutput).ToList();
            var testWriter = testWriterFactory.GetOneWriterAndVerifyOthersAreEmptyAndDisposed<DataServerEvent>("DataServerEvents", 1);
            testWriter.ReceivedObjects.Should().BeEquivalentTo(expectedOutput);
            _processingNotificationsCollector.ReceivedAnything().Should().BeFalse();
        }
        
        [Fact]
        public void CppEventSpecificTestCases()
        {
            var testWriterFactory = new TestWriterFactory();
            using (var plugin = new DataServerPlugin())
            {
                plugin.Configure(testWriterFactory, null, _processingNotificationsCollector, new NullLoggerFactory());
                var testCases = _cppEventSpecificTestCases.Select(testCase => new PluginTestCase
                {
                    LogContents = new NativeJsonLogsBaseEvent
                    {
                        EventType = testCase.EventType,
                        EventPayload = JToken.Parse(testCase.JsonPayloadString)
                    },
                    LogFileInfo = CppTestLogFileInfo
                });

                foreach (var testCase in testCases)
                {
                    var logLine = testCase.GetLogLine();
                    plugin.ProcessLogLine(logLine, LogType.DataserverCpp);
                }

                plugin.CompleteProcessing();
            }

            var testWriter = testWriterFactory.GetOneWriterAndVerifyOthersAreEmptyAndDisposed<DataServerEvent>("DataServerEvents", 1);
            var expectedOutput = _cppEventSpecificTestCases
                .Select(testCase =>
                {
                    var dict = testCase.ExpectedOutput;
                    dict.Add("EventValue", testCase.JsonPayloadString);
                    return dict;
                })
                .ToList();
            testWriter.ReceivedObjects.Count.Should().Be(expectedOutput.Count);
            for (var i = 0; i < expectedOutput.Count; i++)
            {
                AssertMethods.AssertThatAllClassOwnPropsAreAtDefaultExpectFor(
                    testWriter.ReceivedObjects[i],
                    expectedOutput[i],
                    _cppEventSpecificTestCases[i].EventType);
            }
            _processingNotificationsCollector.ReceivedAnything().Should().BeFalse();
        }
        
        [Fact]
        public void JavaTestCases()
        {
            var testWriterFactory = new TestWriterFactory();
            using (var plugin = new DataServerPlugin())
            {
                plugin.Configure(testWriterFactory, null, _processingNotificationsCollector, new NullLoggerFactory());

                foreach (var testCase in _javaTestCases)
                {
                    var logLine = testCase.GetLogLine();
                    plugin.ProcessLogLine(logLine, LogType.DataserverJava);
                }

                plugin.CompleteProcessing();
            }

            var expectedOutput = _javaTestCases.Select(testCase => testCase.ExpectedOutput).ToList();
            var testWriter = testWriterFactory.GetOneWriterAndVerifyOthersAreEmptyAndDisposed<DataServerEvent>("DataServerEvents", 1);
            testWriter.ReceivedObjects.Should().BeEquivalentTo(expectedOutput);
            _processingNotificationsCollector.ReceivedAnything().Should().BeFalse();
        }

        private readonly IList<PluginTestCase> _cppBasicTestCases = new List<PluginTestCase>
        {
            new PluginTestCase
            {
                // generic message, no context
                LogContents = new NativeJsonLogsBaseEvent
                {
                    EventType = "hyper-api",
                    EventPayload = JToken.Parse(
                        "{\"connection-close-detach-skipped\":\"Code: hyper:libpq_error\nMessage: The Hyper server closed the connection unexpectedly.\nHint Message: The server process may have been shut down or terminated before or while processing the request.\nSeverity: ERROR\nContext id: 0xc50b814c\n\"}"),
                    ProcessId = 53072,
                    RequestId = "-",
                    SessionId = "default-",
                    Severity = "warn",
                    Site = "default",
                    ThreadId = "11b34",
                    Timestamp = new DateTime(2020, 1, 4, 15, 52, 5, 475),
                    Username = "user1"
                },
                LogFileInfo = CppTestLogFileInfo,
                LineNumber = 124,
                ExpectedOutput = new
                {
                    // BaseEvent
                    FileName = CppTestLogFileInfo.FileName,
                    FilePath = CppTestLogFileInfo.FilePath,
                    LineNumber = 124,
                    Timestamp = new DateTime(2020, 1, 4, 15, 52, 5, 475),
                    Worker = CppTestLogFileInfo.Worker,

                    // BaseCppEvent
                    EventKey = "hyper-api",
                    ProcessId = 53072,
                    RequestId = "-",
                    Severity = "warn",
                    ThreadId = "11b34",
                    SessionId = "default-",
                    Site = "default",
                    User = "user1",

                    // DataServerCppEvent - Context
                    ClientProcessId = (string) null,
                    ClientRequestId = (string) null,
                    ClientSessionId = (string) null,
                    ClientThreadId = (string) null,
                    ClientType = (string) null,
                    ClientUsername = (string) null,

                    EventValue = "{\"connection-close-detach-skipped\":\"Code: hyper:libpq_error\\nMessage: The Hyper server closed the connection unexpectedly.\\nHint Message: The server process may have been shut down or terminated before or while processing the request.\\nSeverity: ERROR\\nContext id: 0xc50b814c\\n\"}",
                }
            },
            
            new PluginTestCase { // generic message with context
                LogContents = new NativeJsonLogsBaseEvent
                {
                    ContextMetrics = new ContextMetrics
                    {
                        ClientProcessId = "123",
                        ClientRequestId = "456",
                        ClientSessionId = "789",
                        ClientThreadId = "012",
                        ClientType = "345",
                        ClientUsername = "678"
                    },
                    EventType = "hyper-api",
                    EventPayload = JToken.Parse("{\"connection-close-detach-skipped\":\"Code: hyper:libpq_error\nMessage: The Hyper server closed the connection unexpectedly.\nHint Message: The server process may have been shut down or terminated before or while processing the request.\nSeverity: ERROR\nContext id: 0xc50b814c\n\"}"),
                    ProcessId = 53072,
                    RequestId = "-",
                    SessionId = "default-",
                    Severity = "warn",
                    Site = "default",
                    ThreadId = "11b34",
                    Timestamp = new DateTime(2020, 1, 4, 15, 52, 5, 475),
                    Username = "user1"
                },
                LogFileInfo = CppTestLogFileInfo,
                LineNumber = 125,
                ExpectedOutput = new {
                    // BaseEvent
                    FileName = CppTestLogFileInfo.FileName,
                    FilePath = CppTestLogFileInfo.FilePath,
                    LineNumber = 125,
                    Timestamp = new DateTime(2020, 1, 4, 15, 52, 5, 475),
                    Worker = CppTestLogFileInfo.Worker,
                    
                    // BaseCppEvent
                    EventKey = "hyper-api",
                    ProcessId = 53072,
                    RequestId = "-",
                    Severity = "warn",
                    ThreadId = "11b34",
                    SessionId = "default-",
                    Site = "default",
                    User = "user1",
                    
                    // DataServerCppEvent - Context
                    ClientProcessId = "123",
                    ClientRequestId = "456",
                    ClientSessionId = "789",
                    ClientThreadId = "012",
                    ClientType = "345",
                    ClientUsername =  "678",
                    
                    EventValue = "{\"connection-close-detach-skipped\":\"Code: hyper:libpq_error\\nMessage: The Hyper server closed the connection unexpectedly.\\nHint Message: The server process may have been shut down or terminated before or while processing the request.\\nSeverity: ERROR\\nContext id: 0xc50b814c\\n\"}",
                }
            },
        };
        
        private readonly IList<CppEventSpecificTestCase> _cppEventSpecificTestCases = new List<CppEventSpecificTestCase>
        {
            new CppEventSpecificTestCase
            {
                EventType = "end-ds.connect-data-connection",
                JsonPayloadString = "{\"caption\":\"Data Source 1\",\"elapsed\":0.002,\"name\":\"Data Source 1\"}",
                ExpectedOutput = new Dictionary <string, object> {
                    { "ElapsedMs", 2 },
                }
            },
            
            new CppEventSpecificTestCase
            {
                EventType = "end-ds.load-metadata",
                JsonPayloadString = "{\"caption\":\"Data Source 1\",\"elapsed\":0.005,\"name\":\"Data Source 1\"}",
                ExpectedOutput = new Dictionary <string, object> {
                    { "ElapsedMs", 5 },
                }
            },
            
            new CppEventSpecificTestCase
            {
                EventType = "end-ds.connect",
                JsonPayloadString = "{\"caption\":\"Data Source 1\",\"elapsed\":0.007,\"name\":\"Data Source 1\"}",
                ExpectedOutput = new Dictionary <string, object> {
                    { "ElapsedMs", 7 },
                }
            },
            
            new CppEventSpecificTestCase
            {
                EventType = "end-ds.validate",
                JsonPayloadString = "{\"caption\":\"Data Source 1\",\"elapsed\":0.004,\"name\":\"Data Source 1\"}",
                ExpectedOutput = new Dictionary <string, object> {
                    { "ElapsedMs", 4 },
                }
            },
            
            new CppEventSpecificTestCase
            {
                EventType = "end-ds.validate-extract",
                JsonPayloadString = "{\"caption\":\"Data Source 1\",\"elapsed\":0.008,\"name\":\"Data Source 1\"}",
                ExpectedOutput = new Dictionary <string, object> {
                    { "ElapsedMs", 8 },
                }
            },
            
            new CppEventSpecificTestCase
            {
                EventType = "end-ds.parser-connect-extract",
                JsonPayloadString = "{\"caption\":\"Data Source 1\",\"elapsed\":0.006,\"name\":\"Data Source 1\"}",
                ExpectedOutput = new Dictionary <string, object> {
                    { "ElapsedMs", 6 },
                }
            },
            
            new CppEventSpecificTestCase
            {
                EventType = "end-ds.parser-connect",
                JsonPayloadString = "{\"caption\":\"Data Source 1\",\"elapsed\":0.009,\"name\":\"Data Source 1\"}",
                ExpectedOutput = new Dictionary <string, object> {
                    { "ElapsedMs", 9 },
                }
            },
            
            new CppEventSpecificTestCase
            {
                EventType = "extract-archive-file",
                JsonPayloadString = @"{""elapsed-ms"":""24"",""filename"":""D:\\Tableau\\Tableau Server\\temp.tds"",""total-written-b"":""1563931""}",
                ExpectedOutput = new Dictionary <string, object> {
                    { "ElapsedMs", 24 },
                }
            },
            
            new CppEventSpecificTestCase
            {
                EventType = "end-protocol.query",
                JsonPayloadString = @"{""cols"":9,""elapsed"":0.393,""is-command"":false,""protocol-class"":""sqlproxy"",""protocol-id"":92993,""query-category"":""Unknown"",""query-hash"":3494360539,""query-trunc"":""<?xml version='1.0' encoding='utf-8' ?>\r\n\r\n<sqlproxy>\r\n  <logical-query version='1.2.0'>\r\n    <selects>\r\n      <field>[Name (User)]</field>\r\n      <field>[sum:Calculation_1197957525217275905:ok]</field>\r\n~~~<<<query-trunc>>>~~~rmula='2' />\r\n      <aliases>\r\n        <alias key='1' value='Some Data' />\r\n      </aliases>\r\n    </column>\r\n  </datasource>\r\n</sqlproxy>\r\n"",""rows"":54}",
                ExpectedOutput = new Dictionary <string, object> {
                    { "Cols", 9 },
                    { "ElapsedMs", 393 },
                    { "ProtocolClass", "sqlproxy" },
                    { "ProtocolId", 92993 },
                    { "Query", "<?xml version='1.0' encoding='utf-8' ?>\r\n\r\n<sqlproxy>\r\n  <logical-query version='1.2.0'>\r\n    <selects>\r\n      <field>[Name (User)]</field>\r\n      <field>[sum:Calculation_1197957525217275905:ok]</field>\r\n~~~<<<query-trunc>>>~~~rmula='2' />\r\n      <aliases>\r\n        <alias key='1' value='Some Data' />\r\n      </aliases>\r\n    </column>\r\n  </datasource>\r\n</sqlproxy>\r\n" },
                    { "QueryCategory", "Unknown" },
                    { "QueryHash", 3494360539 },
                    { "Rows", 54 },
                }
            },
            
            new CppEventSpecificTestCase
            {
                EventType = "end-query",
                JsonPayloadString = @"{""cols"":19,""elapsed"":0.394,""is-command"":false,""protocol-class"":""sqlproxy"",""protocol-id"":92993,""query-category"":""Unknown"",""query-hash"":3494360539,""query-trunc"":""<?xml version='1.0' encoding='utf-8' ?>\r\n\r\n<sqlproxy>\r\n  <logical-query version='1.2.0'>\r\n    <selects>\r\n      <field>[Name (User)]</field>\r\n      <field>[sum:Calculation_1197957525217275905:ok]</field>\r\n~~~<<<query-trunc>>>~~~rmula='2' />\r\n      <aliases>\r\n        <alias key='1' value='Some Data' />\r\n      </aliases>\r\n    </column>\r\n  </datasource>\r\n</sqlproxy>\r\n"",""rows"":54}",
                ExpectedOutput = new Dictionary <string, object> {
                    { "Cols", 19 },
                    { "ElapsedMs", 394 },
                    { "ProtocolClass", "sqlproxy" },
                    { "ProtocolId", 92993 },
                    { "Query", "<?xml version='1.0' encoding='utf-8' ?>\r\n\r\n<sqlproxy>\r\n  <logical-query version='1.2.0'>\r\n    <selects>\r\n      <field>[Name (User)]</field>\r\n      <field>[sum:Calculation_1197957525217275905:ok]</field>\r\n~~~<<<query-trunc>>>~~~rmula='2' />\r\n      <aliases>\r\n        <alias key='1' value='Some Data' />\r\n      </aliases>\r\n    </column>\r\n  </datasource>\r\n</sqlproxy>\r\n" },
                    { "QueryCategory", "Unknown" },
                    { "QueryHash", 3494360539 },
                    { "Rows", 54 },
                }
            },
            
            new CppEventSpecificTestCase
            {
                EventType = "get-cached-query",
                JsonPayloadString = @"{""cache-load-outcome"":""miss"",""cache-read-ms"":1,""cache-write-ms"":1,""class"":""hyper"",""eqc-key-hash"":""3319893782"",""eqc-key-size-b"":18706,""eqc-load-elapsed-ms"":0,""eqc-load-outcome"":""miss-no-entry"",""kind"":""native"",""mem-load-fetch-ms"":0,""mem-load-lock-ms"":0,""mem-load-outcome"":""miss-no-entry"",""mem-load-total-ms"":0,""ms"":288}",
                ExpectedOutput = new Dictionary <string, object> {
                    { "CacheLoadOutcome", "miss"},
                    { "ElapsedMs", 288},
                    { "EqcLoadElapsedMs", 0},
                    { "EqcLoadOutcome", "miss-no-entry"},
                    { "KeyHash", "3319893782"},
                    { "MemLoadFetchMs", 0},
                    { "MemLoadLockMs", 0},
                    { "MemLoadOutcome", "miss-no-entry"},
                    { "MemLoadTotalMs", 0},
                    { "ProtocolClass", "hyper"},
                    { "QueryKind", "native"},
                }
            },
            
            new CppEventSpecificTestCase
            {
                EventType = "get-cached-query",
                JsonPayloadString = @"{""cache-outcome"":""hit"",""class"":""hyper"",""eqc-column-count"":162,""eqc-key-hash"":""290714814"",""eqc-key-size-b"":788,""eqc-load-elapsed-ms"":54,""eqc-outcome"":""hit"",""eqc-row-count"":0,""eqc-source"":""dataserver:backgrounder"",""eqc-value-size-b"":86832,""kind"":""metadata"",""mem-load-fetch-ms"":1,""mem-load-lock-ms"":2,""mem-outcome"":""miss-no-entry"",""mem-load-total-ms"":3,""mem-store-guid"":2278307,""mem-store-lock-ms"":4,""mem-store-put-ms"":5,""mem-store-total-ms"":9,""ms"":55,""logical-query-hash"":""testHash"",""mem-load-guid"":""fakeGuid""}",
                ExpectedOutput = new Dictionary <string, object> {
                    { "CacheLoadOutcome", "hit"},
                    { "Cols", 162},
                    { "ElapsedMs", 55},
                    { "EqcLoadElapsedMs", 54},
                    { "EqcLoadOutcome", "hit"},
                    { "EqcSource", "dataserver:backgrounder"},
                    { "KeyHash", "290714814"},
                    { "LogicalQueryHash", "testHash"},
                    { "MemLoadFetchMs", 1},
                    { "MemLoadGuid", "fakeGuid"},
                    { "MemLoadLockMs", 2},
                    { "MemLoadOutcome", "miss-no-entry"},
                    { "MemLoadTotalMs", 3},
                    { "MemStoreGuid", "2278307"},
                    { "MemStoreLockMs", 4},
                    { "MemStorePutMs", 5},
                    { "MemStoreTotalMs", 9},
                    { "ProtocolClass", "hyper"},
                    { "QueryKind", "metadata"},
                    { "Rows", 0},
                    { "ValueSizeBytes", 86832},
                }
            },
            
            new CppEventSpecificTestCase
            {
                EventType = "begin-protocol.query",
                JsonPayloadString = @"{""is-command"":false,""protocol-id"":92994,""query"":""show \""lc_collate\"""",""query-category"":""Metadata"",""query-hash"":2509126948}",
                ExpectedOutput = new Dictionary <string, object> {
                    { "ProtocolId", 92994 },
                    { "Query", "show \"lc_collate\"" },
                    { "QueryCategory", "Metadata" },
                    { "QueryHash", 2509126948 },
                }
            },
            
            new CppEventSpecificTestCase
            {
                EventType = "begin-query",
                JsonPayloadString = @"{""is-command"":false,""protocol-id"":92987,""query"":""SELECT \""Extract\"".\""Name1\"" AS \""Name\"" FROM \""Extract\"".\""Extract\"" \""Extract\""\nWHERE (\""Extract\"".\""Name\"" IN ('John Smith')\nGROUP BY 1\n/* { \""tableau-query-origins\"":  } */"",""query-category"":""Unknown"",""query-hash"":957994732}",
                ExpectedOutput = new Dictionary <string, object> {
                    { "ProtocolId", 92987 },
                    { "Query", "SELECT \"Extract\".\"Name1\" AS \"Name\" FROM \"Extract\".\"Extract\" \"Extract\"\nWHERE (\"Extract\".\"Name\" IN ('John Smith')\nGROUP BY 1\n/* { \"tableau-query-origins\":  } */" },
                    { "QueryCategory", "Unknown" },
                    { "QueryHash", 957994732 },
                }
            },
            
            new CppEventSpecificTestCase
            {
                EventType = "ec-load",
                JsonPayloadString = @"{""cns"":""LQTV2Z"",""elapsed-ms"":""3"",""key-hash"":""997127208"",""key-size-b"":""78"",""outcome"":""miss""}",
                ExpectedOutput = new Dictionary <string, object> {
                    { "CacheLoadOutcome", "miss" },
                    { "CacheNamespace", "LQTV2Z" },
                    { "ElapsedMs", 3 },
                    { "KeyHash", "997127208" },
                }
            },
            
            new CppEventSpecificTestCase
            {
                EventType = "ec-store",
                JsonPayloadString = @"{""cns"":""NQTV3Z"",""elapsed-ms"":""2"",""key-hash"":""2740633394"",""key-size-b"":""80"",""load-time-ms"":""250"",""lower-bound-ms"":""10"",""outcome"":""done"",""value-size-b"":""59""}",
                ExpectedOutput = new Dictionary <string, object> {
                    { "CacheNamespace", "NQTV3Z" },
                    { "CacheStoreOutcome", "done" },
                    { "ElapsedMs", 2 },
                    { "KeyHash", "2740633394" },
                    { "ValueSizeBytes", 59 },
                }
            },
            
            new CppEventSpecificTestCase
            {
                EventType = "eqc-store",
                JsonPayloadString = @"{""class"":""hyper"",""column-count"":""2"",""eqc-key-elapsed-ms"":""0"",""eqc-key-hash"":""130295572"",""eqc-key-size-b"":""4524"",""eqc-store-ms"":""0"",""eqc-store-outcome"":""ignore-expired"",""eqc-total-ms"":""4"",""kind"":""native"",""ms"":5,""query-latency-ms"":""227"",""row-count"":""3"",""value-size-b"":""1396""}",
                ExpectedOutput = new Dictionary <string, object> {
                    { "CacheStoreOutcome", "ignore-expired" },
                    { "Cols", 2 },
                    { "ElapsedMs", 4 },
                    { "ProtocolClass", "hyper" },
                    { "QueryKind", "native" },
                    { "QueryLatencyMs", 227 },
                    { "Rows", 3 },
                    { "ValueSizeBytes", 1396 },
                }
            },
            
            new CppEventSpecificTestCase
            {
                EventType = "eqc-store",
                JsonPayloadString = @"{""class"":""hyper"",""column-count"":""2"",""eqc-key-elapsed-ms"":""0"",""eqc-key-hash"":""130295572"",""eqc-key-size-b"":""4524"",""eqc-store-ms"":""0"",""cache-store-outcome"":""ignore-expired"",""elapsed-total-ms"":""4"",""query-kind"":""native"",""ms"":5,""query-latency-ms"":""227"",""row-count"":""3"",""value-size-b"":""1396""}",
                ExpectedOutput = new Dictionary <string, object> {
                    { "CacheStoreOutcome", "ignore-expired" },
                    { "Cols", 2 },
                    { "ElapsedMs", 4 },
                    { "ProtocolClass", "hyper" },
                    { "QueryKind", "native" },
                    { "QueryLatencyMs", 227 },
                    { "Rows", 3 },
                    { "ValueSizeBytes", 1396 },
                }
            },
            
            new CppEventSpecificTestCase
            {
                EventType = "read-metadata",
                JsonPayloadString = @"{""attributes"":{"":thread-session"":""92987"",""authentication"":""auth-username-password"",""author-locale"":""en_US"",""class"":""hyper"",""dbname"":""extract/3e/ba/{A444AAA4-A1A0-BB22-A015-CCCCEEEEFFFF}/extract.hyper"",""extract-engine"":""true"",""password"":""********"",""port"":""8040"",""server"":""host1"",""sslmode"":"""",""user-language"":""en_US"",""username"":""user1""},""elapsed"":0.261,""group-id"":91035,""id"":92987,""metadata-records"":2,""table"":""#Tableau_12345_1_Group""}",
                ExpectedOutput = new Dictionary <string, object> {
                    { "ElapsedMs", 261 },
                    { "ProtocolId", 92987 },
                    { "TableName", "#Tableau_12345_1_Group" },
                    { "DbName", "extract/3e/ba/{A444AAA4-A1A0-BB22-A015-CCCCEEEEFFFF}/extract.hyper" },
                    { "ProtocolClass", "hyper" },
                }
            },
            
            new CppEventSpecificTestCase
            {
                EventType = "data-server-temp-table",
                JsonPayloadString = @"{""action"":""CommitInsertData"",""action-tuple-count"":""2468"",""elapsed-ms"":""3"",""sessionId"":""DC45CF815ECC40FE9648773F5BAF62B4-2:2"",""tableName"":""#Tableau_11984_1_Group""}",
                ExpectedOutput = new Dictionary <string, object> {
                    { "ElapsedMs", 3 },
                    { "TempTableAction", "CommitInsertData" },
                }
            },
            
            new CppEventSpecificTestCase
            {
                EventType = "end-sql-regular-table-tuples-insert",
                JsonPayloadString = @"{""elapsed-insert"":0.151,""num-columns"":5,""num-tuples"":47273,""protocol-id"":92964,""source-query-hash"":656609377,""tablename"":""[public].[Tableau_92964_FQ_Temp_4]""}",
                ExpectedOutput = new Dictionary <string, object> {
                    { "Cols", 5 },
                    { "ElapsedMs", 151 },
                    { "ProtocolId", 92964 },
                    { "Rows", 47273 },
                }
            },
            
            new CppEventSpecificTestCase
            {
                EventType = "end-sql-regular-table-tuples-create",
                JsonPayloadString = @"{""elapsed-create"":0.012,""num-columns"":5,""protocol-id"":92964,""source-query-hash"":656609377,""tablename"":""[public].[Tableau_92964_3_FQ_Temp_4]""}",
                ExpectedOutput = new Dictionary <string, object> {
                    { "Cols", 5 },
                    { "ElapsedMs", 12 },
                    { "ProtocolId", 92964 },
                }
            },
            
            new CppEventSpecificTestCase
            {
                EventType = "end-sql-temp-table-tuples-create",
                JsonPayloadString = @"{""elapsed"":0.093,""elapsed-create"":0.02,""elapsed-insert"":0.072,""num-columns"":2,""num-tuples"":11,""protocol-id"":92948,""tablename"":""[#Tableau_92948_2_Group]""}",
                ExpectedOutput = new Dictionary <string, object> {
                    { "ElapsedMs", 93 },
                    { "ProtocolId", 92948 },
                }
            },
            
            new CppEventSpecificTestCase
            {
                EventType = "hyper-libpq-protocol",
                JsonPayloadString = @"{""bytes"":""28"",""elapsed"":0.019,""protocol-id"":""13298"",""rows"":""1"",""status"":""Inserted data chunk""}",
                ExpectedOutput = new Dictionary <string, object> {
                    { "ElapsedMs", 19 },
                    { "ProtocolId", 13298 },
                    { "Rows", 1 },
                }
            },
            
            new CppEventSpecificTestCase
            {
                EventType = "hyper-libpq-protocol", // Some other status, without elapsed
                JsonPayloadString = @"{""external-table"":""\""Extract\"""",""protocol-id"":""13274"",""status"":""Creating external temporary table."",""tde-path"":""'extract/11/70/6666/something.tde'"",""tde-table"":""'Extract.Extract'""}",
                ExpectedOutput = new Dictionary <string, object> {
                    { "ProtocolId", 13274 },
                }
            },
            
            new CppEventSpecificTestCase
            {
                EventType = "hyper-query-summary",
                JsonPayloadString = @"{""elapsed"":""0.006"",""elapsed-close-rowset"":""0.000"",""elapsed-execute-query"":""0.005"",""elapsed-idle-until-destroyed"":""0.000"",""elapsed-init-rowset"":""0.001"",""elapsed-read-rowset"":""0.000"",""protocol-id"":""92948"",""query-hash"":""2366174350""}",
                ExpectedOutput = new Dictionary <string, object> {
                    { "ElapsedMs", 6 },
                    { "ProtocolId", 92948 },
                    { "QueryHash", 2366174350 },
                }
            },
            
            new CppEventSpecificTestCase
            {
                EventType = "hyper-read-first-tuples",
                JsonPayloadString = @"{""cols"":""3"",""elapsed"":""0.044"",""rows"":""65,536""}",
                ExpectedOutput = new Dictionary <string, object> {
                    { "Cols", 3 },
                    { "ElapsedMs", 44 },
                    { "Rows", 65536 },
                }
            },
            
            new CppEventSpecificTestCase
            {
                EventType = "end-hyper-read-tuples",
                JsonPayloadString = @"{""builder-count"":""4"",""cols"":""3"",""elapsed"":""0.123"",""rows"":""184,294""}",
                ExpectedOutput = new Dictionary <string, object> {
                    { "Cols", 3 },
                    { "ElapsedMs", 123 },
                    { "Rows", 184294 },
                }
            },
            
            new CppEventSpecificTestCase
            {
                EventType = "hyper-api",
                JsonPayloadString = @"{""inserter-end"":""{\""table-name\"":\""#Tableau_92337_30_Tuples\"",\""elapsed-msec\"":25,\""chunk-count\"":1,\""byte-count\"":3317,\""mb-per-sec\"":0.13}""}",
                ExpectedOutput = new Dictionary <string, object> {
                    { "ChunkCount", 1 },
                    { "ElapsedMs", 25 },
                    { "MbPerSecond", 0.13 },
                    { "TableName", "#Tableau_92337_30_Tuples" },
                    { "ValueSizeBytes", 3317 },
                }
            },
            
            new CppEventSpecificTestCase
            {
                EventType = "hyper-api", // Unexpected value of inserter-end
                JsonPayloadString = @"{""inserter-end"":""blah""}",
                ExpectedOutput = new Dictionary <string, object>()
            },
            
            new CppEventSpecificTestCase {
                EventType = "hyper-api", // missing inserter-end
                JsonPayloadString = @"{""some_other_key"":""{\""table-name\"":\""#Tableau_92337_30_Tuples\"",\""elapsed-msec\"":25,\""chunk-count\"":1,\""byte-count\"":3317,\""mb-per-sec\"":0.13}""}",
                ExpectedOutput = new Dictionary <string, object>()
            },
            
            new CppEventSpecificTestCase
            {
                EventType = "data-inserter-summary",
                JsonPayloadString = @"{""elapsed"":""0.03"",""elapsed-finish"":""0.000"",""elapsed-get-builders"":""0.000"",""elapsed-read"":""0.000"",""protocol-id"":""92337"",""rows"":""135"",""size-bytes"":""3,317"",""table-name"":""#Tableau_92337_30_Tuples""}",
                ExpectedOutput = new Dictionary <string, object> {
                    { "ElapsedMs", 30 },
                    { "ProtocolId", 92337 },
                    { "Rows", 135 },
                    { "ValueSizeBytes", 3317 },
                }
            },
            
            new CppEventSpecificTestCase
            {
                EventType = "hyper-send-chunk",
                JsonPayloadString = @"{""elapsed"":""0.025"",""protocol-id"":""92337"",""rows"":""135"",""size-bytes"":""3,317""}",
                ExpectedOutput = new Dictionary <string, object> {
                    { "ElapsedMs", 25 },
                    { "ProtocolId", 92337 },
                    { "Rows", 135 },
                    { "ValueSizeBytes", 3317 },
                }
            },
            
            new CppEventSpecificTestCase
            {
                EventType = "hyper-process-chunks",
                JsonPayloadString = @"{""chunks"":""1"",""elapsed"":""0.025""}",
                ExpectedOutput = new Dictionary <string, object> {
                    { "ChunkCount", 1 },
                    { "ElapsedMs", 25 },
                }
            },
            
            new CppEventSpecificTestCase
            {
                EventType = "hyper-format-tuples",
                JsonPayloadString = @"{""builder-id"":""0"",""chunks-processed"":""2"",""elapsed"":""0.035""}",
                ExpectedOutput = new Dictionary <string, object> {
                    { "ChunkCount", 2 },
                    { "ElapsedMs", 35 },
                }
            },
            
            new CppEventSpecificTestCase
            {
                EventType = "construct-protocol-group", // Some attributes missing
                JsonPayloadString = @"{""attributes"":{""channel"":""file"",""class"":""sqlproxy"",""dbname"":""D:/Tableau/Tableau Server/data/tabsvc/temp/dataserver_2.20201.19.1220.1612/repoItem4523458214551579054.tmp"",""federated-keychain"":""********"",""keychain"":""********"",""password"":""********"",""server-userid"":""3202"",""server-viewerid"":""3728"",""username"":""""},""closed-protocols-count"":""0"",""connection-limit"":""1000000"",""group-id"":""90852"",""in-construction-count"":""0"",""protocols-count"":""0"",""this"":""0x00000070828fead0""}",
                ExpectedOutput = new Dictionary <string, object> {
                    { "ProtocolGroupConnectionLimit", 1000000 },
                    { "ProtocolGroupId", 90852 },
                    { "ProtocolGroupProtocolsCount", 0 },
                    { "DbName", "D:/Tableau/Tableau Server/data/tabsvc/temp/dataserver_2.20201.19.1220.1612/repoItem4523458214551579054.tmp" },
                    { "ProtocolClass", "sqlproxy" },
                }
            },
            
            new CppEventSpecificTestCase
            {
                EventType = "construct-protocol-group", // All attributes present
                JsonPayloadString = @"{""attributes"":{""authentication"":""auth-username-password"",""author-locale"":""en_US"",""class"":""hyper"",""dbname"":""extract/96/56/{1234}/test.hyper"",""extract-engine"":""true"",""password"":""********"",""port"":""8231"",""server"":""host123"",""sslmode"":"""",""user-language"":""en_US"",""username"":""user1""},""closed-protocols-count"":""0"",""connection-limit"":""1000000"",""group-id"":""90851"",""in-construction-count"":""0"",""protocols-count"":""3"",""this"":""0x0000006fa35ad350""}",
                ExpectedOutput = new Dictionary <string, object> {
                    { "ProtocolGroupConnectionLimit", 1000000 },
                    { "ProtocolGroupId", 90851 },
                    { "ProtocolGroupProtocolsCount", 3 },
                    { "DbName", "extract/96/56/{1234}/test.hyper" },
                    { "Port", "8231" },
                    { "ProtocolClass", "hyper" },
                    { "Server", "host123" },
                }
            },
            
            new CppEventSpecificTestCase
            {
                EventType = "destruct-protocol-group",
                JsonPayloadString = @"{""attributes"":{""authentication"":""auth-username-password"",""author-locale"":""en_US"",""class"":""hyper"",""dbname"":""extract/ab/c1/{123}/test2.hyper"",""extract-engine"":""true"",""password"":""********"",""port"":""8040"",""server"":""host321"",""sslmode"":"""",""user-language"":""en_US"",""username"":""user1""},""closed-protocols-count"":""0"",""connection-limit"":""1000000"",""group-id"":""90614"",""in-construction-count"":""0"",""protocols-count"":""0"",""this"":""0x000000701d12dfe0""}",
                ExpectedOutput = new Dictionary <string, object> {
                    { "ProtocolGroupConnectionLimit", 1000000 },
                    { "ProtocolGroupId", 90614 },
                    { "ProtocolGroupProtocolsCount", 0 },
                    { "DbName", "extract/ab/c1/{123}/test2.hyper" },
                    { "Port", "8040" },
                    { "ProtocolClass", "hyper" },
                    { "Server", "host321" },
                }
            },
            
            new CppEventSpecificTestCase
            {
                EventType = "construct-protocol",
                JsonPayloadString = @"{""attributes"":{"":thread-session"":""92794"",""authentication"":""auth-username-password"",""author-locale"":""en_US"",""class"":""hyper"",""dbname"":""extract/81/24/{456}/testy.hyper"",""extract-engine"":""true"",""password"":""********"",""port"":""8231"",""server"":""host456"",""sslmode"":"""",""user-language"":""en_US"",""username"":""user2""},""created"":""1/14/2020 3:20:07 PM"",""created-elapsed"":0.005,""disconnected"":false,""id"":92794,""this"":""0x0000006f013a7af0""}",
                ExpectedOutput = new Dictionary <string, object> {
                    { "ElapsedMs", 5 },
                    { "ProtocolId", 92794 },
                    { "DbName", "extract/81/24/{456}/testy.hyper" },
                    { "Port", "8231" },
                    { "ProtocolClass", "hyper" },
                    { "Server", "host456" },
                }
            },
            
            new CppEventSpecificTestCase
            {
                EventType = "destruct-protocol-elapsed",
                JsonPayloadString = @"{""attributes"":{"":thread-session"":""92535"",""authentication"":""auth-username-password"",""author-locale"":""en_US"",""class"":""hyper"",""dbname"":""extract/b2/c7/{789}/test3.hyper"",""extract-engine"":""true"",""password"":""********"",""port"":""8231"",""server"":""host345"",""sslmode"":"""",""user-language"":""en_US"",""username"":""user5""},""created"":""1/14/2020 2:47:39 PM"",""disconnected"":false,""elapsed"":0.007,""id"":92535,""group-id"":""432"",""this"":""0x0000006fcf6f4c50""}",
                ExpectedOutput = new Dictionary <string, object> {
                    { "ElapsedMs", 7 },
                    { "ProtocolGroupId", 432 },
                    { "ProtocolId", 92535 },
                    { "DbName", "extract/b2/c7/{789}/test3.hyper" },
                    { "Port", "8231" },
                    { "ProtocolClass", "hyper" },
                    { "Server", "host345" },
                }
            },
            
            new CppEventSpecificTestCase
            {
                EventType = "protocol.create-protocol",
                JsonPayloadString = @"{""authentication"":""auth-username-password"",""class"":""hyper"",""name"":""Summary"",""protocol-class"":""hyper"",""server"":""server10"",""userName"":""awesome_user""}",
                ExpectedOutput = new Dictionary <string, object> {
                    { "ProtocolClass", "hyper" },
                }
            },
            
            new CppEventSpecificTestCase
            {
                EventType = "protocol-added-to-group",
                JsonPayloadString = @"{""group"":{""attributes"":{""authentication"":""auth-username-password"",""author-locale"":""en_US"",""class"":""not_hyper"",""dbname"":""extract/17/f7/{555}/Data.hyper"",""extract-engine"":""true"",""password"":""********"",""port"":""8040"",""server"":""server11"",""sslmode"":"""",""user-language"":""en_US"",""username"":""admin""},""closed-protocols-count"":""0"",""connection-limit"":""1000000"",""group-id"":""90799"",""in-construction-count"":""0"",""protocols-count"":""1"",""this"":""0x00000070619ab5d0""},""protocol-id"":""92747""}",
                ExpectedOutput = new Dictionary <string, object> {
                    { "ProtocolId", 92747 },
                    { "ProtocolGroupConnectionLimit", 1000000 },
                    { "ProtocolGroupId", 90799 },
                    { "ProtocolGroupProtocolsCount", 1 },
                    { "DbName", "extract/17/f7/{555}/Data.hyper" },
                    { "Port", "8040" },
                    { "ProtocolClass", "not_hyper" },
                    { "Server", "server11" },
                }
            },
            
            new CppEventSpecificTestCase
            {
                EventType = "protocol-removed-from-group", // Federated connection - group attributes don't have clear DB info
                JsonPayloadString = @"{""group"":{""attributes"":{""class"":""federated"",""federated-connection"":""<connection :id='salesData1' :locale='1033' caption='' class='federated' defer-connect-prompt-validation='yes' name='salesData2'>\n  <named-connections>\n    <named-connection name='hyper_0'>\n      <connection :leaf-connection-name='hyper_0' authentication='auth-username-password' author-locale='en_US' caption='' class='hyper' dbname='extract/ca/a2/777/hyper_0.hyper' default-settings='yes' extract-engine='true' management-tablespace='extracts' name='salesData3' password='********' port='8231' schema='Extract' server='server55' sslmode='' tablename='Extract' user-language='en_US' username='user55' />\n    </named-connection>\n  </named-connections>\n</connection>\n""},""closed-protocols-count"":""0"",""connection-limit"":""1000001"",""group-id"":""89938"",""in-construction-count"":""0"",""protocols-count"":""0"",""this"":""0x0000006fc276a790""},""protocol-id"":""91873""}",
                ExpectedOutput = new Dictionary <string, object> {
                    { "ProtocolId", 91873 },
                    { "ProtocolGroupConnectionLimit", 1000001 },
                    { "ProtocolGroupId", 89938 },
                    { "ProtocolGroupProtocolsCount", 0 },
                    { "ProtocolClass", "federated" },
                }
            },

            new CppEventSpecificTestCase
            {
                EventType = "protocol-removed-from-group",
                JsonPayloadString = @"{""group"":{""attributes"":{""authentication"":""auth-username-password"",""author-locale"":""en_US"",""class"":""hyper"",""dbname"":""extract/ca/a2/444/hyper_0.hyper"",""extract-engine"":""true"",""password"":""********"",""port"":""8231"",""server"":""server44"",""sslmode"":"""",""user-language"":""en_US"",""username"":""user44""},""closed-protocols-count"":""0"",""connection-limit"":""1000002"",""group-id"":""89937"",""in-construction-count"":""0"",""protocols-count"":""5"",""this"":""0x0000006f98cba530""},""protocol-id"":""91872""}",
                ExpectedOutput = new Dictionary <string, object> {
                    { "ProtocolId", 91872 },
                    { "ProtocolGroupConnectionLimit", 1000002 },
                    { "ProtocolGroupId", 89937 },
                    { "ProtocolGroupProtocolsCount", 5 },
                    { "DbName", "extract/ca/a2/444/hyper_0.hyper" },
                    { "Port", "8231" },
                    { "ProtocolClass", "hyper" },
                    { "Server", "server44" },
                }
            },
        };
        
        private readonly IList<PluginTestCase> _javaTestCases = new List<PluginTestCase>
        {
             new PluginTestCase { // line with all data available
                LogContents = "2020-01-14 12:19:48.502 -0800 (Default,user1,vizqlSession1,Xh4iZLl4MQpetCEAjQjImwAAA8g) catalina-exec-23 : INFO  com.tableausoftware.hyper.discovery.HyperDiscoveryService - event=ServerConnectionsProvider.getDataengineConnections.begin",
                LogFileInfo = JavaTestLogFileInfo,
                LineNumber = 124,
                ExpectedOutput = new {
                    EventKey = "com.tableausoftware.hyper.discovery.HyperDiscoveryService",
                    FileName = JavaTestLogFileInfo.FileName,
                    FilePath = JavaTestLogFileInfo.FilePath,
                    LineNumber = 124,
                    EventValue = "event=ServerConnectionsProvider.getDataengineConnections.begin",
                    ProcessId = (int?) null,
                    RequestId = "Xh4iZLl4MQpetCEAjQjImwAAA8g",
                    SessionId = "vizqlSession1",
                    Severity = "INFO",
                    Site = "Default",
                    ThreadId = "catalina-exec-23",
                    Timestamp = DateTime.Parse("2020-01-14 12:19:48.502"),
                    User = "user1",
                    Worker = JavaTestLogFileInfo.Worker
                }
            },
             
             new PluginTestCase { // line with some data missing
                 LogContents = @"2020-01-13 16:00:43.604 -0800 (,,,) StatusServerThread_dataserver_0 : ERROR  com.tableausoftware.service.thrift.win32.NamedPipeServerTransport - Awaiting client connection on \\.\pipe\dataserver_0-status, handle native@0xdbe4.",
                 LogFileInfo = JavaTestLogFileInfo,
                 LineNumber = 125,
                 ExpectedOutput = new {
                     EventKey = "com.tableausoftware.service.thrift.win32.NamedPipeServerTransport",
                     FileName = JavaTestLogFileInfo.FileName,
                     FilePath = JavaTestLogFileInfo.FilePath,
                     LineNumber = 125,
                     EventValue = @"Awaiting client connection on \\.\pipe\dataserver_0-status, handle native@0xdbe4.",
                     ProcessId = (int?) null,
                     RequestId = (string) null,
                     SessionId = (string) null,
                     Severity = "ERROR",
                     Site = (string) null,
                     ThreadId = "StatusServerThread_dataserver_0",
                     Timestamp = DateTime.Parse("2020-01-13 16:00:43.604"),
                     User = (string) null,
                     Worker = JavaTestLogFileInfo.Worker
                 }
             },
        };

        public class CppEventSpecificTestCase
        {
            public string EventType { get; set; }
            public string JsonPayloadString { get; set; }
            public Dictionary <string, object> ExpectedOutput { get; set; }
        }
    }
}