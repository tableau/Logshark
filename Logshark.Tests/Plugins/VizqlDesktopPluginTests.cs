using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using LogShark.Containers;
using LogShark.LogParser.Containers;
using LogShark.Plugins.Shared;
using LogShark.Plugins.VizqlDesktop;
using LogShark.Plugins.VizqlDesktop.Model;
using LogShark.Tests.Plugins.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using Xunit;

namespace LogShark.Tests.Plugins
{
    public class VizqlDesktopPluginTests : InvariantCultureTestsBase
    {
        private  static readonly LogFileInfo TestLogFileInfo = new LogFileInfo("log.txt", "log.txt", "worker0", DateTime.MinValue);
        
        [Fact]
        public void BadOrNoOpInput()
        {
            var processingNotificationsCollector = new ProcessingNotificationsCollector(10);
            var testWriterFactory = new TestWriterFactory();
            using (var plugin = new VizqlDesktopPlugin())
            {
                plugin.Configure(testWriterFactory, null, processingNotificationsCollector, new NullLoggerFactory());
                
                var wrongContentFormat = new LogLine(new ReadLogLineResult(123, "VizqlDesktop doesn't expect string"), TestLogFileInfo);
                var nullContent = new LogLine(new ReadLogLineResult(123, null), TestLogFileInfo);
                var noOpContentType = new LogLine(new ReadLogLineResult(123, new NativeJsonLogsBaseEvent { EventType = "something else"}), TestLogFileInfo);

                plugin.ProcessLogLine(wrongContentFormat, LogType.BackgrounderJava);
                plugin.ProcessLogLine(nullContent, LogType.BackgrounderJava);
                plugin.ProcessLogLine(noOpContentType, LogType.BackgrounderJava);
            }

            testWriterFactory.AssertAllWritersAreDisposedAndEmpty(4);
            processingNotificationsCollector.TotalErrorsReported.Should().Be(2);
        }

        [Theory]
        [InlineData(false, null)]
        [InlineData(true, null)]
        [InlineData(true, 10)]
        public void TestWithDifferentMaxLimitConfigured(bool useConfig, int? maxLength)
        {
            var config = useConfig
                ? CreateConfig(maxLength)
                : null;
            var testWriterFactory = new TestWriterFactory();
            using (var plugin = new VizqlDesktopPlugin())
            {
                plugin.Configure(testWriterFactory, config, null, new NullLoggerFactory());

                foreach (var testCase in _testCases)
                {
                    var logLine = testCase.GetLogLine();
                    plugin.ProcessLogLine(logLine, LogType.VizqlDesktop);
                }
                
                plugin.CompleteProcessing();
            }
            
            testWriterFactory.AssertAllWritersDisposedState(true);

            var expectedSessions = GetExpectedOutputForName("session");
            var sessionsWriter = testWriterFactory.GetWriterByName<VizqlDesktopSession>("VizqlDesktopSessions");
            sessionsWriter.ReceivedObjects.Should().BeEquivalentTo(expectedSessions);

            var expectedErrors = GetExpectedOutputForName("error");
            var errorsWriter = testWriterFactory.GetWriterByName<VizqlDesktopErrorEvent>("VizqlDesktopErrorEvents");
            errorsWriter.ReceivedObjects.Should().BeEquivalentTo(expectedErrors);
            
            var expectedEndQueryEvents = GetExpectedOutputForName("endQuery");
            if (useConfig && maxLength != null)
            {
                expectedEndQueryEvents = TruncateQueryText(expectedEndQueryEvents, maxLength.Value);
            }
            var endQueryEventsWriter = testWriterFactory.GetWriterByName<VizqlEndQueryEvent>("VizqlDesktopEndQueryEvents");
            endQueryEventsWriter.ReceivedObjects.Should().BeEquivalentTo(expectedEndQueryEvents);
            
            var expectedPerformanceEvents = GetExpectedOutputForName("performance");
            var extraEvent = GetPerformanceEvent(8.985, "end-query", 126, @"{""cols"":1,""elapsed"":8.985,""is-command"":false,""protocol-class"":""postgres"",""protocol-id"":44,""query-category"":""Data"",""query-hash"":3535682211,""query-trunc"":""SELECT * FROM very_long_table_name_so_we_have_something_to_truncate;"",""rows"":3}");
            expectedPerformanceEvents.Insert(0, extraEvent);
            var performanceEventsWriter = testWriterFactory.GetWriterByName<VizqlPerformanceEvent>("VizqlDesktopPerformanceEvents");
            performanceEventsWriter.ReceivedObjects.Should().BeEquivalentTo(expectedPerformanceEvents);
        }
        
        private static IConfiguration CreateConfig(int? maxQueryLength)
        {
            return ConfigGenerator.GetConfigWithASingleValue(
                "MaxQueryLength",
                maxQueryLength.ToString());
        }

        private List<object> GetExpectedOutputForName(string name)
        {
            return _testCases
                .Select(testCase => testCase.ExpectedOutput as Tuple<string, object>)
                .Where(tuple => tuple != null && tuple.Item1 == name)
                .Select(tuple => tuple.Item2)
                .ToList();
        }

        private static List<object> TruncateQueryText(IEnumerable<object> list, int maxLength)
        {
            return list
                .Select(item =>
                {
                    var dynamicItem = (dynamic) item;
                    var query = (string) dynamicItem.Query;
                    var newQuery = query.Substring(0, maxLength);
                    return GetEndQueryEvent(newQuery);
                })
                .ToList();
        }

        private static object GetPerformanceEvent(double? elapsed, string type, int lineNumber, string value)
        {
            return new
            {
                FileName = TestLogFileInfo.FileName,
                FilePath = TestLogFileInfo.FilePath,
                LineNumber = lineNumber,
                Timestamp = new DateTime(2018, 10, 23, 10, 21, 20, 537),
                Worker = TestLogFileInfo.Worker,

                KeyType = type,
                ProcessId = 38608,
                ThreadId = "88f4",
                SessionId = "windows_machine_23520_190604_10273675",

                ElapsedSeconds = elapsed,
                Value = value
            };
        }

        private static object GetEndQueryEvent(string query)
        {
            return new
            {
                FileName = TestLogFileInfo.FileName,
                FilePath = TestLogFileInfo.FilePath,
                LineNumber = 126,
                Timestamp = new DateTime(2018, 10, 23, 10, 21, 20, 537),
                Worker = TestLogFileInfo.Worker,

                KeyType = "end-query",
                ProcessId = 38608,
                ThreadId = "88f4",
                SessionId = "windows_machine_23520_190604_10273675",

                Cols = 1,
                Elapsed = 8.985,
                ProtocolId = 44,
                Query = query,
                QueryHash = 3535682211,
                Rows = 3,
            };
        }

        private readonly IList<PluginTestCase> _testCases = new List<PluginTestCase>
        {
            new PluginTestCase // Even though it is a valid event, this line will be ignored because plugin did not see startup event yet and thus doesn't have session id
            {
                LogFileInfo = TestLogFileInfo,
                LogContents = JsonConvert.DeserializeObject<NativeJsonLogsBaseEvent>(@"{""ts"":""2019-06-04T10:17:53.268"",""pid"":14108,""tid"":""5a58"",""sev"":""info"",""req"":""-"",""sess"":""-"",""site"":""-"",""user"":""-"",""k"":""ec-drop"",""v"":{""cns"":""MDRV3Z"",""elapsed-ms"":""3"",""key-hash"":""319843205"",""key-size-b"":""114"",""outcome"":""done""}}"),
                LineNumber = 123,
            },
            
            new PluginTestCase // This error event should be captured even if we don't have session  id available
            {
                LogFileInfo = TestLogFileInfo,
                LogContents = JsonConvert.DeserializeObject<NativeJsonLogsBaseEvent>(@"{""ts"":""2019-06-04T10:36:18.616"",""pid"":7816,""tid"":""257c"",""sev"":""error"",""req"":""-"",""sess"":""-"",""site"":""-"",""user"":""-"",""k"":""connector-plugin-error"",""e"":{""log-code"":""0e9acd07"",""log-source"":""needs-classification""},""v"":""Class already registered: mongodb""}"),
                LineNumber = 124,
                ExpectedOutput = new Tuple<string, object>("error", new
                {
                    FileName = TestLogFileInfo.FileName,
                    FilePath = TestLogFileInfo.FilePath,
                    LineNumber = 124,
                    Timestamp = new DateTime(2019, 6, 4, 10, 36, 18, 616),
                    Worker = TestLogFileInfo.Worker,

                    KeyType = "connector-plugin-error",
                    ProcessId = 7816,
                    ThreadId = "257c",
                    SessionId = (string) null,
                    
                    Message = "\"Class already registered: mongodb\"",
                    Severity = "error"
                })},
            
            new PluginTestCase
            {
                LogFileInfo = TestLogFileInfo,
                LogContents = JsonConvert.DeserializeObject<NativeJsonLogsBaseEvent>(@"{""ts"":""2019-06-04T10:27:36.756"",""pid"":23520,""tid"":""5268"",""sev"":""info"",""req"":""-"",""sess"":""-"",""site"":""-"",""user"":""-"",""k"":""startup-info"",""v"":{""cwd"":""D:\\Blah"",""domain"":""test.com"",""hostname"":""windows_machine"",""os"":""Microsoft Windows 10 Enterprise (Build 15063)"",""process-id"":""23520 (0x5be0)"",""start-time"":""2019-06-04T17:27:36.756"",""tableau-version"":""20192.19.0506.2105,x64""}}"),
                LineNumber = 125,
                ExpectedOutput = new Tuple<string, object>("session", new
                {
                    CurrentWorkingDirectory = "D:\\Blah",
                    Domain = "test.com",
                    Hostname = "windows_machine",
                    Os = "Microsoft Windows 10 Enterprise (Build 15063)",
                    ProcessId = 23520,    
                    StartTime = new DateTime(2019, 6, 4, 10, 27, 36, 756),
                    TableauVersion = "20192.19.0506.2105,x64",
                    SessionId = "windows_machine_23520_190604_10273675",
                })},
            
            new PluginTestCase
            {
                LogFileInfo = TestLogFileInfo,
                LogContents = JsonConvert.DeserializeObject<NativeJsonLogsBaseEvent>(@"{""ts"":""2018-10-23T10:21:20.537"",""pid"":38608,""tid"":""88f4"",""sev"":""info"",""req"":""-"",""sess"":""-"",""site"":""-"",""user"":""-"",""k"":""end-query"",""v"":{""cols"":1,""elapsed"":8.985,""is-command"":false,""protocol-class"":""postgres"",""protocol-id"":44,""query-category"":""Data"",""query-hash"":3535682211,""query-trunc"":""SELECT * FROM very_long_table_name_so_we_have_something_to_truncate;"",""rows"":3},""ctx"":{""client-type"":""desktop"",""procid"":""38484"",""tid"":""21496"",""version"":""20181.18.0807.1415""}}"),
                LineNumber = 126,
                ExpectedOutput = new Tuple<string, object>("endQuery", GetEndQueryEvent("SELECT * FROM very_long_table_name_so_we_have_something_to_truncate;")
                )},
            
            new PluginTestCase
            {
                LogFileInfo = TestLogFileInfo,
                LogContents = JsonConvert.DeserializeObject<NativeJsonLogsBaseEvent>(@"{""ts"":""2018-10-23T10:21:20.537"",""pid"":38608,""tid"":""88f4"",""sev"":""info"",""req"":""-"",""sess"":""-"",""site"":""-"",""user"":""-"",""k"":""ec-drop"",""v"":{""cns"":""MDRV3Z"",""elapsed-ms"":""3"",""key-hash"":""319843205"",""key-size-b"":""114"",""outcome"":""done""}}"),
                LineNumber = 127,
                ExpectedOutput = new Tuple<string, object>("performance", GetPerformanceEvent(0.003, "ec-drop", 127, @"{""cns"":""MDRV3Z"",""elapsed-ms"":""3"",""key-hash"":""319843205"",""key-size-b"":""114"",""outcome"":""done""}"))},
            
            new PluginTestCase
            {
                LogFileInfo = TestLogFileInfo,
                LogContents = JsonConvert.DeserializeObject<NativeJsonLogsBaseEvent>(@"{""ts"":""2018-10-23T10:21:20.537"",""pid"":38608,""tid"":""88f4"",""sev"":""info"",""req"":""-"",""sess"":""-"",""site"":""-"",""user"":""-"",""k"":""compute-percentages"",""v"":{""elapsed"":""3""}}"),
                LineNumber = 128,
                ExpectedOutput = new Tuple<string, object>("performance", GetPerformanceEvent(3, "compute-percentages", 128, @"{""elapsed"":""3""}"))},
            
            new PluginTestCase
            {
                LogFileInfo = TestLogFileInfo,
                LogContents = JsonConvert.DeserializeObject<NativeJsonLogsBaseEvent>(@"{""ts"":""2018-10-23T10:21:20.537"",""pid"":38608,""tid"":""88f4"",""sev"":""info"",""req"":""-"",""sess"":""-"",""site"":""-"",""user"":""-"",""k"":""end-sql-temp-table-tuples-create"",""v"":{""elapsed"":""4""}}"),
                LineNumber = 129,
                ExpectedOutput = new Tuple<string, object>("performance", GetPerformanceEvent(4, "end-sql-temp-table-tuples-create", 129, @"{""elapsed"":""4""}"))},
            
            new PluginTestCase
            {
                LogFileInfo = TestLogFileInfo,
                LogContents = JsonConvert.DeserializeObject<NativeJsonLogsBaseEvent>(@"{""ts"":""2019-06-04T10:36:20.111"",""pid"":7816,""tid"":""257c"",""sev"":""error"",""req"":""-"",""sess"":""-"",""site"":""-"",""user"":""-"",""k"":""msg"",""e"":{""log-code"":""f6fa4626"",""log-source"":""needs-classification""},""v"":""Error message""}"),
                LineNumber = 130,
                ExpectedOutput = new Tuple<string, object>("error", new
                {
                    FileName = TestLogFileInfo.FileName,
                    FilePath = TestLogFileInfo.FilePath,
                    LineNumber = 130,
                    Timestamp = new DateTime(2019, 6, 4, 10, 36, 20, 111),
                    Worker = TestLogFileInfo.Worker,

                    KeyType = "msg",
                    ProcessId = 7816,
                    ThreadId = "257c",
                    SessionId = "windows_machine_23520_190604_10273675",
                    
                    Message = "\"Error message\"",
                    Severity = "error"
                })},
            
            new PluginTestCase
            {
                LogFileInfo = TestLogFileInfo,
                LogContents = JsonConvert.DeserializeObject<NativeJsonLogsBaseEvent>(@"{""ts"":""2018-10-23T10:21:20.537"",""pid"":38608,""tid"":""88f4"",""sev"":""info"",""req"":""-"",""sess"":""-"",""site"":""-"",""user"":""-"",""k"":""end-sql-temp-table-tuples-create"",""v"":{""elapsed"":""4""}}"),
                LineNumber = 131,
                ExpectedOutput = new Tuple<string, object>("performance", GetPerformanceEvent(4, "end-sql-temp-table-tuples-create", 131, @"{""elapsed"":""4""}"
                ))},
            
            new PluginTestCase
            {
                LogFileInfo = TestLogFileInfo,
                LogContents = JsonConvert.DeserializeObject<NativeJsonLogsBaseEvent>(@"{""ts"":""2019-06-04T10:36:20.111"",""pid"":7816,""tid"":""257c"",""sev"":""fatal"",""req"":""-"",""sess"":""-"",""site"":""-"",""user"":""-"",""k"":""msg"",""e"":{""log-code"":""f6fa4626"",""log-source"":""needs-classification""},""v"":{""error"":""Error message inside json""}}"),
                LineNumber = 132,
                ExpectedOutput = new Tuple<string, object>("error", new
                {
                    FileName = TestLogFileInfo.FileName,
                    FilePath = TestLogFileInfo.FilePath,
                    LineNumber = 132,
                    Timestamp = new DateTime(2019, 6, 4, 10, 36, 20, 111),
                    Worker = TestLogFileInfo.Worker,

                    KeyType = "msg",
                    ProcessId = 7816,
                    ThreadId = "257c",
                    SessionId = "windows_machine_23520_190604_10273675",
                    
                    Message = @"{""error"":""Error message inside json""}",
                    Severity = "fatal"
                })},
            
            new PluginTestCase
            {
                LogFileInfo = TestLogFileInfo,
                LogContents = JsonConvert.DeserializeObject<NativeJsonLogsBaseEvent>(@"{""ts"":""2018-10-23T10:21:20.537"",""pid"":38608,""tid"":""88f4"",""sev"":""info"",""req"":""-"",""sess"":""-"",""site"":""-"",""user"":""-"",""k"":""construct-protocol"",""v"":{""attributes"":{"":thread-session"":""10"",""authentication"":""auth-none"",""author-locale"":""en_US"",""class"":""hyper"",""dbname"":""postgres"",""sslmode"":"""",""user-language"":""en_US"",""username"":""tableau_internal_user""},""created"":""6/4/2019 10:36:20 AM"",""created-elapsed"":0.002,""disconnected"":false,""id"":10,""this"":""0x0000029c7b458100""}}"),
                LineNumber = 133,
                ExpectedOutput = new Tuple<string, object>("performance", GetPerformanceEvent(0.002, "construct-protocol", 133, @"{""attributes"":{"":thread-session"":""10"",""authentication"":""auth-none"",""author-locale"":""en_US"",""class"":""hyper"",""dbname"":""postgres"",""sslmode"":"""",""user-language"":""en_US"",""username"":""tableau_internal_user""},""created"":""6/4/2019 10:36:20 AM"",""created-elapsed"":0.002,""disconnected"":false,""id"":10,""this"":""0x0000029c7b458100""}"
                ))},
        };
    }
}