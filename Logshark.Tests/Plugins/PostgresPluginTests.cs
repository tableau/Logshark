using FluentAssertions;
using LogShark.Plugins.Postgres;
using LogShark.Tests.Plugins.Helpers;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using LogShark.Shared;
using LogShark.Shared.LogReading;
using LogShark.Shared.LogReading.Containers;
using Xunit;

namespace LogShark.Tests.Plugins
{
    public class PostgresPluginTests : InvariantCultureTestsBase
    {
        private static readonly LogFileInfo TestLogFileInfo = new LogFileInfo("postgresql.csv", @"pgsql/postgresql.csv", "node1", DateTime.MinValue);

        [Fact]
        public void BadInput()
        {
            var processingNotificationsCollector = new ProcessingNotificationsCollector(10);
            var testWriterFactory = new TestWriterFactory();
            using (var plugin = new PostgresPlugin())
            {
                plugin.Configure(testWriterFactory, null, processingNotificationsCollector, new NullLoggerFactory());
                
                var wrongContentFormat = new LogLine(new ReadLogLineResult(123, 1234), TestLogFileInfo);
                var nullContent = new LogLine(new ReadLogLineResult(123, null), TestLogFileInfo);
                var stringContent = new LogLine(new ReadLogLineResult(123, "I don't expect string!"), TestLogFileInfo);
                
                plugin.ProcessLogLine(wrongContentFormat, LogType.PostgresCsv);
                plugin.ProcessLogLine(nullContent, LogType.PostgresCsv);
                plugin.ProcessLogLine(stringContent, LogType.PostgresCsv);
            }

            testWriterFactory.AssertAllWritersAreDisposedAndEmpty(1);
            processingNotificationsCollector.TotalErrorsReported.Should().Be(3);
        }
        
        [Fact]
        public void RunTestCases()
        { 
            var testWriterFactory = new TestWriterFactory();
            using (var plugin = new PostgresPlugin())
            {
                plugin.Configure(testWriterFactory, null, null, new NullLoggerFactory());

                foreach (var testCase in _testCases)
                {
                    var logLine = testCase.GetLogLine();
                    plugin.ProcessLogLine(logLine, LogType.PostgresCsv);
                }
            }

            var expectedOutput = _testCases.Select(testCase => testCase.ExpectedOutput).ToList();
            var testWriter = testWriterFactory.Writers.Values.First() as TestWriter<PostgresEvent>;

            testWriterFactory.Writers.Count.Should().Be(1);
            testWriter.WasDisposed.Should().Be(true);
            testWriter.ReceivedObjects.Should().BeEquivalentTo(expectedOutput);
        }

        private readonly List<PluginTestCase> _testCases = new List<PluginTestCase>
        {
            new PluginTestCase // random line from test logs
            {
                LogContents = new PostgresCsvMapping()
                {
                    Timestamp = DateTime.Parse("2018-07-13 04:50:35.406 GMT"),
                    Username = string.Empty,
                    DatabaseName = string.Empty,
                    Pid = 5576,
                    Client = string.Empty,
                    SessionId = "5b47e189.15c8",
                    PerSessionLineNumber = "3",
                    CommandTag = string.Empty,
                    SessionStartTime = "2018-07-12 23:17:29 GMT",
                    VirtualTransactionId = string.Empty,
                    RegularTransactionId = "0",
                    Sev = "LOG",
                    SqlstateCode = "00000",
                    Message = "received fast shutdown request",
                    MessageDetail = string.Empty,
                    Hint = string.Empty,
                    InternalQueryLedToError = string.Empty,
                    InternalQueryErrorPosition = string.Empty,
                    ErrorContext = string.Empty,
                    UserQueryLedToError = string.Empty,
                    UserQueryErrorPosition = string.Empty,
                    ErrorLocationInPostgresSource = string.Empty,
                    ApplicationName = string.Empty,
                },
                LogFileInfo = TestLogFileInfo,
                LineNumber = 1,
                ExpectedOutput = new PostgresEvent
                {
                    Duration = null,
                    File = "postgresql.csv",
                    FilePath = "pgsql/postgresql.csv",
                    LineNumber = 1,
                    Message = "received fast shutdown request",
                    ProcessId = 5576,
                    Severity = "LOG",
                    Timestamp = DateTime.Parse("2018-07-13 04:50:35.406 GMT").ToUniversalTime(),
                    Worker = "node1",
                }
            },
            new PluginTestCase() // random line with duration
            {
                LogContents = new PostgresCsvMapping()
                {
                    Timestamp = DateTime.Parse("2018-07-13 12:45:18.545 GMT"),
                    Username = "rails",
                    DatabaseName = "workgroup",
                    Pid = 3912,
                    Client = "127.0.0.1:56274",
                    SessionId = "5b488dd3.f48",
                    PerSessionLineNumber = "1",
                    CommandTag = "COMMIT",
                    SessionStartTime = "2018-07-13 11:32:35 GMT",
                    VirtualTransactionId = "16/0",
                    RegularTransactionId = "0",
                    Sev = "LOG",
                    SqlstateCode = "00000",
                    Message = "duration: 1236.292 ms  execute S_2: COMMIT",
                    MessageDetail = string.Empty,
                    Hint = string.Empty,
                    InternalQueryLedToError = string.Empty,
                    InternalQueryErrorPosition = string.Empty,
                    ErrorContext = string.Empty,
                    UserQueryLedToError = string.Empty,
                    UserQueryErrorPosition = string.Empty,
                    ErrorLocationInPostgresSource = string.Empty,
                    ApplicationName = string.Empty,
                },
                LogFileInfo = TestLogFileInfo,
                LineNumber = 1,
                ExpectedOutput = new PostgresEvent
                {
                    Duration = 1236,
                    File = "postgresql.csv",
                    FilePath = "pgsql/postgresql.csv",
                    LineNumber = 1,
                    Message = "duration: 1236.292 ms  execute S_2: COMMIT",
                    ProcessId = 3912,
                    Severity = "LOG",
                    Timestamp = DateTime.Parse("2018-07-13 12:45:18.545 GMT").ToUniversalTime(),
                    Worker = "node1",
                }
            }
        };
    }
}
