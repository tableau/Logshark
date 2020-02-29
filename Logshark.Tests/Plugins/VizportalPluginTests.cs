using FluentAssertions;
using LogShark.Containers;
using LogShark.Plugins.Vizportal;
using LogShark.Tests.Plugins.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LogShark.LogParser.Containers;
using Xunit;

namespace LogShark.Tests.Plugins
{
    public class VizportalPluginTests : InvariantCultureTestsBase
    {
        private static readonly LogFileInfo TestLogFileInfo = new LogFileInfo("vizportal-0.log", @"vizportal/vizportal-0.log", "node1", DateTime.MinValue);
        
        [Fact]
        public void BadInput()
        {
            var processingNotificationsCollector = new ProcessingNotificationsCollector(10);
            var testWriterFactory = new TestWriterFactory();
            using (var plugin = new VizportalPlugin())
            {
                plugin.Configure(testWriterFactory, null, processingNotificationsCollector, new NullLoggerFactory());
                
                var wrongContentFormat = new LogLine(new ReadLogLineResult(123, 1234), TestLogFileInfo);
                var nullContent = new LogLine(new ReadLogLineResult(123, null), TestLogFileInfo);
                var wrongContent = new LogLine(new ReadLogLineResult(123, "I am not a log line!"), TestLogFileInfo);
                
                plugin.ProcessLogLine(wrongContentFormat, LogType.VizportalJava);
                plugin.ProcessLogLine(nullContent, LogType.VizportalJava);
                plugin.ProcessLogLine(wrongContent, LogType.VizportalJava);
            }

            testWriterFactory.AssertAllWritersAreDisposedAndEmpty(1);
            processingNotificationsCollector.TotalErrorsReported.Should().Be(3);
        }

        [Fact]
        public void RunTestCases()
        { 
            var testWriterFactory = new TestWriterFactory();
            using (var plugin = new VizportalPlugin())
            {
                plugin.Configure(testWriterFactory, null, null, new NullLoggerFactory());

                foreach (var testCase in _testCases)
                {
                    var logLine = testCase.GetLogLine();
                    plugin.ProcessLogLine(logLine, LogType.VizportalJava);
                }
            }

            var expectedOutput = _testCases.Select(testCase => testCase.ExpectedOutput).ToList();
            var testWriter = testWriterFactory.Writers.Values.First() as TestWriter<VizportalEvent>;

            testWriterFactory.Writers.Count.Should().Be(1);
            testWriter.WasDisposed.Should().Be(true);
            testWriter.ReceivedObjects.Should().BeEquivalentTo(expectedOutput);
        }

        private readonly List<PluginTestCase> _testCases = new List<PluginTestCase>
        {
            new PluginTestCase // random line from test logs
            {
                LogContents = @"2018-07-12 17:13:42.835 -0700 (-,-,-,W0futr2lVgac7tY08X1vpwAAA90,0:-12ed38f9:16490d16613:-7fe1) catalina-exec-1 vizportal: INFO  com.tableausoftware.app.vizportal.LoggingInterceptor - Request received: /v1/getSessionInfo",
                LogFileInfo = TestLogFileInfo,
                LineNumber = 1,
                ExpectedOutput = new VizportalEvent
                {
                    Class = "com.tableausoftware.app.vizportal.LoggingInterceptor",
                    File = "vizportal-0.log",
                    FilePath = @"vizportal/vizportal-0.log",
                    LineNumber = 1,
                    Message = @"Request received: /v1/getSessionInfo",
                    RequestId = "W0futr2lVgac7tY08X1vpwAAA90",
                    SessionId = "-",
                    Severity = "INFO",
                    Site = "-",
                    Timestamp = DateTime.Parse("2018-07-12 17:13:42.835"),
                    User = "-",
                    Worker = "node1"
                }
            }
        };
    }
}
