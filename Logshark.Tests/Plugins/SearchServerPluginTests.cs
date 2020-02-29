using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using LogShark.Containers;
using LogShark.LogParser.Containers;
using LogShark.Plugins.SearchServer;
using LogShark.Tests.Plugins.Helpers;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LogShark.Tests.Plugins
{
    public class SearchServerPluginTests : InvariantCultureTestsBase
    {
        private static readonly LogFileInfo TestLogFileInfo = new LogFileInfo("searchserver_node1-0.txt", @"node1/searchserver_0.20182.18.1001.21158702228811516627172/searchserver_node1-0.txt", "worker5", new DateTime(2019, 05, 04, 03, 02, 01));
        
        [Fact]
        public void BadInput()
        {
            var processingNotificationsCollector = new ProcessingNotificationsCollector(10);
            var testWriterFactory = new TestWriterFactory();
            using (var plugin = new SearchServerPlugin())
            {
                plugin.Configure(testWriterFactory, null, processingNotificationsCollector, new NullLoggerFactory());
                
                var wrongContentFormat = new LogLine(new ReadLogLineResult(123, 1234), TestLogFileInfo);
                var nullContent = new LogLine(new ReadLogLineResult(123, null), TestLogFileInfo);
                var incorrectContent = new LogLine(new ReadLogLineResult(123, "I am not a SearchServer line!"), TestLogFileInfo);
                
                plugin.ProcessLogLine(wrongContentFormat, LogType.SearchServer);
                plugin.ProcessLogLine(nullContent, LogType.SearchServer);
                plugin.ProcessLogLine(incorrectContent, LogType.SearchServer);
            }

            testWriterFactory.AssertAllWritersAreDisposedAndEmpty(1);
            processingNotificationsCollector.TotalErrorsReported.Should().Be(3);
        }
        
        [Fact]
        public void SearchServerPluginTest()
        {
            var testWriterFactory = new TestWriterFactory();
            using (var plugin = new SearchServerPlugin())
            {
                plugin.Configure(testWriterFactory, null, null, new NullLoggerFactory());

                foreach (var testCase in _testCases)
                {
                    var logLine = testCase.GetLogLine();
                    plugin.ProcessLogLine(logLine, testCase.LogType);
                }
            }

            var expectedOutput = _testCases.Select(testCase => testCase.ExpectedOutput).ToList();
            var testWriter = testWriterFactory.Writers.Values.First() as TestWriter<SearchServerEvent>;

            testWriterFactory.Writers.Count.Should().Be(1);
            testWriter.WasDisposed.Should().Be(true);
            testWriter.ReceivedObjects.Should().BeEquivalentTo(expectedOutput);

        }

        private readonly List<PluginTestCase> _testCases = new List<PluginTestCase>
        {
            new PluginTestCase
            {
                LogType = LogType.SearchServer,
                LogContents = "2018-10-03 00:19:45.133 +0000 (,,,) catalina-exec-17 : INFO  org.apache.solr.core.SolrCore - [group] webapp=/solr path=/admin/ping params={wt=javabin&version=2} status=0 QTime=0",
                LogFileInfo = TestLogFileInfo,
                LineNumber = 550,
                ExpectedOutput = new
                {
                    Class = "org.apache.solr.core.SolrCore",
                    File = "searchserver_node1-0.txt",
                    FilePath = "node1/searchserver_0.20182.18.1001.21158702228811516627172/searchserver_node1-0.txt",
                    LineNumber = 550,
                    Message = "[group] webapp=/solr path=/admin/ping params={wt=javabin&version=2} status=0 QTime=0",
                    Severity = "INFO",
                    Timestamp = new DateTime(2018, 10, 03, 00, 19, 45, 133),
                    Worker = "worker5",
                }
            },
        };
    }
}
