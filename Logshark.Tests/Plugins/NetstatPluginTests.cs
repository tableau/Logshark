using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using LogShark.Plugins.Netstat;
using LogShark.Shared;
using LogShark.Shared.LogReading.Containers;
using LogShark.Tests.Plugins.Helpers;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LogShark.Tests.Plugins
{
    public class NetstatPluginTests : InvariantCultureTestsBase
    {
        [Fact]
        public void BadInput()
        {
            var processingNotificationsCollector = new ProcessingNotificationsCollector(10);
            var testWriterFactory = new TestWriterFactory();
            using (var plugin = new NetstatPlugin())
            {
                plugin.Configure(testWriterFactory, null, processingNotificationsCollector, new NullLoggerFactory());
                var logFileInfo = new LogFileInfo("netstat-anp.txt", @"folder1/netstat-anp.txt", "worker1", new DateTime(2019, 04, 12, 13, 33, 31));
                
                var wrongContentFormat = new LogLine(new ReadLogLineResult(123, 1234), logFileInfo);
                var nullContent = new LogLine(new ReadLogLineResult(123, null), logFileInfo);
                
                plugin.ProcessLogLine(wrongContentFormat, LogType.NetstatLinux);
                plugin.ProcessLogLine(nullContent, LogType.NetstatLinux);
                plugin.ProcessLogLine(nullContent, LogType.NetstatWindows);
            }

            testWriterFactory.AssertAllWritersAreDisposedAndEmpty(1);
            processingNotificationsCollector.TotalErrorsReported.Should().Be(3);
        }
        
        [Fact]
        public void NetstatPluginTest()
        {
            var testWriterFactory = new TestWriterFactory();
            using (var plugin = new NetstatPlugin())
            {
                plugin.Configure(testWriterFactory, null, null, new NullLoggerFactory());

                foreach (var testCase in _testCases)
                {
                    var logLine = testCase.GetLogLine();
                    plugin.ProcessLogLine(logLine, testCase.LogType);
                }
            }

            var expectedOutput = _testCases.Select(testCase => testCase.ExpectedOutput).ToList();
            var testWriter = testWriterFactory.Writers.Values.First() as TestWriter<NetstatActiveConnection>;

            testWriterFactory.Writers.Count.Should().Be(1);
            testWriter.WasDisposed.Should().Be(true);
            testWriter.ReceivedObjects.Should().BeEquivalentTo(expectedOutput);
        }

        private readonly List<PluginTestCase> _testCases = new List<PluginTestCase>
        {
            new PluginTestCase
            {
                LogType = LogType.NetstatLinux,
                LogContents = "tcp6       0      0 :::8487                 :::*                    LISTEN      25702/appzookeeper  ",
                LogFileInfo = new LogFileInfo("netstat-anp.txt", @"folder1/netstat-anp.txt", "worker1", new DateTime(2019, 04, 12, 13, 33, 31)),
                LineNumber = 1,
                ExpectedOutput = new
                {
                    Line = 1,
                    ProcessName = "appzookeeper",
                    ProcessId = 25702,
                    ComponentName = (string)null,
                    Protocol = "tcp6",
                    LocalAddress = "::",
                    LocalPort = "8487",
                    ForeignAddress = "::",
                    ForeignPort = "*",
                    TcpState = "LISTEN",
                    RecvQ = 0,
                    SendQ = 0,
                    IsKnownTableauServerProcess = true,
                    Worker = "worker1",
                    FileLastModified = new DateTime(2019, 04, 12, 13, 33, 31),
                }
            },
            new PluginTestCase
            {
               LogType = LogType.NetstatWindows,
               LogContents = new Stack<(string line, int lineNumber)>(new [] {
                   ("  TCP    [::]:49408             [::]:0                 LISTENING", 2502),
                   ("  PolicyAgent", 2503),
                   (" [svchost.exe]", 2504)}),
                LogFileInfo = new LogFileInfo("netstat-info.txt",  @"folder1/netstat-info.txt", "worker1", new DateTime(2019, 04, 12, 14, 34, 41)),
                LineNumber = 2502,
                ExpectedOutput = new
                {
                    Line = 2502,
                    ProcessName = "svchost.exe",
                    ProcessId = (int?)null,
                    ComponentName = "PolicyAgent",
                    Protocol = "TCP",
                    LocalAddress = "[::]",
                    LocalPort = "49408",
                    ForeignAddress = "[::]",
                    ForeignPort = "0",
                    TcpState = "LISTENING",
                    RecvQ = (int?)null,
                    SendQ = (int?)null,
                    IsKnownTableauServerProcess = false,
                    Worker = "worker1",
                    FileLastModified = new DateTime(2019, 04, 12, 14, 34, 41),
                }
            },
        };
    }
}
