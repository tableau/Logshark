using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using LogShark.Containers;
using LogShark.LogParser.Containers;
using LogShark.Plugins.ResourceManager.Model;
using LogShark.Plugins.Shared;
using LogShark.Tests.Plugins.Helpers;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json.Linq;
using Xunit;

namespace LogShark.Tests.Plugins.ResourceManagerPlugin
{
    public class ResourceManagerPluginTests : InvariantCultureTestsBase
    {
        private  static readonly LogFileInfo VizqlServerCppFileInfo = new LogFileInfo("vizqlserver_3-2_2017_12_19_03_24_29.txt", @"folder1/vizqlserver_3-2_2017_12_19_03_24_29.txt", "node1", DateTime.MinValue);
        private  static readonly LogFileInfo DataServerCppFileInfo = new LogFileInfo("dataserver_1-4_2017_12_19_03_24_29.txt", @"folder1/dataserver_1-4_2017_12_19_03_24_29.txt", "node1", DateTime.MinValue);
        private  static readonly LogFileInfo HyperFileInfo = new LogFileInfo("hyper-4_2017_12_19_03_24_29.txt", @"folder1/hyper_1-4_2017_12_19_03_24_29.txt", "node1", DateTime.MinValue);
        
        [Fact]
        public void BadOrNoOpInput()
        {
            var testWriterFactory = new TestWriterFactory();
            var processingNotificationsCollector = new ProcessingNotificationsCollector(10);
            using (var plugin = new LogShark.Plugins.ResourceManager.ResourceManagerPlugin())
            {
                plugin.Configure(testWriterFactory, null, processingNotificationsCollector, new NullLoggerFactory());
                
                var wrongContentFormat = new LogLine(new ReadLogLineResult(123, "ResourceManagerPlugin doesn't expect string"), VizqlServerCppFileInfo);
                var nullContent = new LogLine(new ReadLogLineResult(123, null), VizqlServerCppFileInfo);
                var nonMsgContent = new LogLine(new ReadLogLineResult(123, new NativeJsonLogsBaseEvent { EventType = "something else"}), VizqlServerCppFileInfo);
                var payloadIsNotAString = new LogLine(new ReadLogLineResult(123, new NativeJsonLogsBaseEvent { EventType = "msg", EventPayload = JToken.FromObject(new {Key = "value"})}), VizqlServerCppFileInfo);
                var vizqlAndSrmInternal = new LogLine(new ReadLogLineResult(123, new NativeJsonLogsBaseEvent { EventType = "srm-internal", EventPayload = "Resource Manager: CPU info: 0%"}), VizqlServerCppFileInfo);
                var hyperAndMessage = new LogLine(new ReadLogLineResult(123, new NativeJsonLogsBaseEvent { EventType = "msg", EventPayload = "Resource Manager: CPU info: 0%"}), HyperFileInfo);
                
                plugin.ProcessLogLine(wrongContentFormat, LogType.VizqlserverCpp);
                plugin.ProcessLogLine(nullContent, LogType.VizqlserverCpp);
                plugin.ProcessLogLine(nonMsgContent, LogType.VizqlserverCpp);
                plugin.ProcessLogLine(payloadIsNotAString, LogType.VizqlserverCpp);
                plugin.ProcessLogLine(vizqlAndSrmInternal, LogType.VizqlserverCpp);
                plugin.ProcessLogLine(hyperAndMessage, LogType.Hyper);
            }

            testWriterFactory.AssertAllWritersAreDisposedAndEmpty(5);
            processingNotificationsCollector.TotalErrorsReported.Should().Be(2);
        }

        [Fact]
        public void BasicTests()
        {
            var testWriterFactory = new TestWriterFactory();
            using (var plugin = new LogShark.Plugins.ResourceManager.ResourceManagerPlugin())
            {
                plugin.Configure(testWriterFactory, null, null, new NullLoggerFactory());

                foreach (var testCase in _testCases)
                {
                    var logLine = testCase.GetLogLine();
                    plugin.ProcessLogLine(logLine, testCase.LogType);
                }
            }
            
            var expectedOutput = _testCases.Select(testCase => testCase.ExpectedOutput).ToList();
            testWriterFactory.Writers.Count.Should().Be(5);
            var writerWithOutput = testWriterFactory.GetOneWriterAndVerifyOthersAreEmptyAndDisposed<ResourceManagerThreshold>("ResourceManagerThresholds", 5);
            ((List<object>)writerWithOutput.ReceivedObjects).Should().BeEquivalentTo(expectedOutput);
        }

        private readonly IList<PluginTestCase> _testCases = new List<PluginTestCase>
        {
            new PluginTestCase
            {
                LogContents = new NativeJsonLogsBaseEvent
                {
                    EventType = "msg",
                    EventPayload = JToken.FromObject("Resource Manager: Max CPU limited to 95% over 3600 seconds"),
                    ProcessId = 111,
                    Timestamp = new DateTime(2019, 4, 10, 10, 31, 00, 321),
                },
                LogType = LogType.VizqlserverCpp,
                LogFileInfo = VizqlServerCppFileInfo,
                LineNumber = 123,
                ExpectedOutput = new
                {
                    FileName = VizqlServerCppFileInfo.FileName,
                    FilePath = VizqlServerCppFileInfo.FilePath,
                    LineNumber = 123,
                    Timestamp = new DateTime(2019, 4, 10, 10, 31, 00, 321),
                    Worker = VizqlServerCppFileInfo.Worker,
                    
                    ProcessId = 111,
                    ProcessIndex = 2,
                    ProcessName = "vizqlserver",
                    
                    CpuLimit = 95,
                    PerProcessMemoryLimit = (long?) null,
                    TotalMemoryLimit = (long?) null,
                }
            },
            
            new PluginTestCase
            {
                LogContents = new NativeJsonLogsBaseEvent
                {
                    EventType = "msg",
                    EventPayload = JToken.FromObject("Resource Manager: All Processes Memory Limit: 130,566,496,051 bytes"),
                    ProcessId = 222,
                    Timestamp = new DateTime(2019, 4, 10, 11, 04, 00, 321),
                },
                LogType = LogType.DataserverCpp,
                LogFileInfo = DataServerCppFileInfo,
                LineNumber = 124,
                ExpectedOutput = new
                {
                    FileName = DataServerCppFileInfo.FileName,
                    FilePath = DataServerCppFileInfo.FilePath,
                    LineNumber = 124,
                    Timestamp = new DateTime(2019, 4, 10, 11, 04, 00, 321),
                    Worker = DataServerCppFileInfo.Worker,
                    
                    ProcessId = 222,
                    ProcessIndex = 4,
                    ProcessName = "dataserver",
                    
                    CpuLimit = (int?) null,
                    PerProcessMemoryLimit = (long?) null,
                    TotalMemoryLimit = 130566496051,
                }
            },
            
            new PluginTestCase
            {
                LogContents = new NativeJsonLogsBaseEvent
                {
                    EventType = "srm-internal",
                    EventPayload = JToken.FromObject(new { msg = "Resource Manager: All Processes Memory Limit: 130,566,496,051 bytes"}),
                    ProcessId = 222,
                    Timestamp = new DateTime(2019, 4, 10, 11, 04, 00, 321),
                },
                LogType = LogType.Hyper,
                LogFileInfo = HyperFileInfo,
                LineNumber = 125,
                ExpectedOutput = new
                {
                    FileName = HyperFileInfo.FileName,
                    FilePath = HyperFileInfo.FilePath,
                    LineNumber = 125,
                    Timestamp = new DateTime(2019, 4, 10, 11, 04, 00, 321),
                    Worker = HyperFileInfo.Worker,
                    
                    ProcessId = 222,
                    ProcessIndex = 4,
                    ProcessName = "hyper",
                    
                    CpuLimit = (int?) null,
                    PerProcessMemoryLimit = (long?) null,
                    TotalMemoryLimit = 130566496051,
                }
            },
        };
    }
}