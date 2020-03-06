using System;
using System.Collections.Generic;
using FluentAssertions;
using LogShark.Containers;
using LogShark.Plugins.ResourceManager;
using LogShark.Plugins.ResourceManager.Model;
using LogShark.Plugins.Shared;
using LogShark.Tests.Plugins.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LogShark.Tests.Plugins.ResourceManagerPlugin
{
    public class ResourceManagerEventsParserTests : InvariantCultureTestsBase
    {
        private static readonly LogFileInfo TestLogFileInfo = new LogFileInfo("test.log", @"folder1/test.log", "node1", DateTime.MinValue);
        private const string TestProcessName = "testProcess";
        
        private readonly NativeJsonLogsBaseEvent _testBaseEvent = new NativeJsonLogsBaseEvent // These are the only two fields needed by ResourceManagerEvent
        {
            Timestamp = DateTime.Now,
            ProcessId = 1234
        };
        private readonly LogLine _testLogLine = PluginTestCase.GetLogLineForExpectedOutput(245, TestLogFileInfo);
        private readonly TestWriterFactory _testWriterFactory = new TestWriterFactory();

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("{\"Resource Manager\" : \"works\"}")]
        [InlineData("Resource Manager: blah")]
        public void NonEvents(string payload)
        {
            using (var eventProcessor = new ResourceManagerEventsProcessor(_testWriterFactory, null))
            {
                eventProcessor.ProcessEvent(_testBaseEvent, payload, _testLogLine, TestProcessName);
            }
            
            _testWriterFactory.AssertAllWritersAreDisposedAndEmpty(5);
        }

        [Theory]
        [InlineData("Resource Manager: CPU info: 0%", true, 0, null)]
        [InlineData("Resource Manager: CPU info: 3%", true, 3, null)]
        [InlineData("Resource Manager: CPU info: ERROR", false, 0, null)]
        [InlineData("Resource Manager: CPU info: A%", false, 0, null)]
        [InlineData("Resource Manager: CPU info: 1%; 2% (Tableau total); 10 (info count)", true, 1, 2)]
        [InlineData("Resource Manager: CPU info: A%; 2% (Tableau total);", true, 2, null)] // This is not quite expected, but second regex for CPU number applies to this string just fine
        [InlineData("Resource Manager: CPU info: 1%; B% (Tableau total); 10 (info count)", true, 1, null)]
        public void CpuSampleEvents(string payload, bool expectEvent, int expectedCpuUtil, int? expectedTotalUtil)
        {
            var processingNotificationsCollector = new ProcessingNotificationsCollector(10); 
            using (var eventProcessor = new ResourceManagerEventsProcessor(_testWriterFactory, processingNotificationsCollector))
            {
                eventProcessor.ProcessEvent(_testBaseEvent, payload, _testLogLine, TestProcessName);
            }
            
            var cpuSampleWriter = _testWriterFactory.GetOneWriterAndVerifyOthersAreEmptyAndDisposed<ResourceManagerCpuSample>("ResourceManagerCpuSamples", 5);

            cpuSampleWriter.ReceivedObjects.Count.Should().Be(expectEvent ? 1 : 0);
            processingNotificationsCollector.TotalErrorsReported.Should().Be(expectEvent ? 0 : 1);
            if (expectEvent)
            {
                var expectedRecord = new
                {
                    FileName = TestLogFileInfo.FileName,
                    FilePath = TestLogFileInfo.FilePath,
                    LineNumber = _testLogLine.LineNumber,
                    Timestamp = _testBaseEvent.Timestamp,
                    Worker = TestLogFileInfo.Worker,
                    
                    ProcessId = _testBaseEvent.ProcessId,
                    ProcessIndex = (int?)null,
                    ProcessName = TestProcessName,

                    ProcessCpuUtil = expectedCpuUtil,
                    TotalCpuUtil = expectedTotalUtil,
                };
                cpuSampleWriter.ReceivedObjects[0].Should().BeEquivalentTo(expectedRecord);
            }
        }

        [Theory]
        [InlineData("Resource Manager: Memory info: 1,041,182,720 bytes (current process);8,391,983,104 bytes (Tableau total); 12,589,604,864 bytes (total of all processes); 10 (info count)",
            true, 1041182720, 12589604864)]
        [InlineData("Resource Manager: Memory info: 1,041,182,720 bytes (current process);8,391,983,104 bytes (Tableau total); Foo bytes (total of all processes); 10 (info count)",
            false, 0, 0)] // Second number expected to have space after preceding semicolon
        [InlineData("Resource Manager: Memory info: Foo bytes (current process);8,391,983,104 bytes (Tableau total); 12,589,604,864 bytes (total of all processes); 10 (info count)",
            false, 0, 0)] // First number expected to have colon ans space before it
        [InlineData("Resource Manager: Memory info: Foo", false, 0, 0)]
        public void MemorySampleEvent(string payload, bool expectEvent, long expectedMemoryUtil, long expectedTotalUtil)
        {
            var processingNotificationsCollector = new ProcessingNotificationsCollector(10);
            using (var eventProcessor = new ResourceManagerEventsProcessor(_testWriterFactory, processingNotificationsCollector))
            {
                eventProcessor.ProcessEvent(_testBaseEvent, payload, _testLogLine, TestProcessName);
            }
            
            var memorySampleWriter = _testWriterFactory.GetOneWriterAndVerifyOthersAreEmptyAndDisposed<ResourceManagerMemorySample>("ResourceManagerMemorySamples", 5);

            memorySampleWriter.ReceivedObjects.Count.Should().Be(expectEvent ? 1 : 0);
            processingNotificationsCollector.TotalErrorsReported.Should().Be(expectEvent ? 0 : 1);
            if (expectEvent)
            {
                var expectedRecord = new
                {
                    FileName = TestLogFileInfo.FileName,
                    FilePath = TestLogFileInfo.FilePath,
                    LineNumber = _testLogLine.LineNumber,
                    Timestamp = _testBaseEvent.Timestamp,
                    Worker = TestLogFileInfo.Worker,
                    
                    ProcessId = _testBaseEvent.ProcessId,
                    ProcessIndex = (int?)null,
                    ProcessName = TestProcessName,

                    ProcessMemoryUtil = expectedMemoryUtil,
                    TotalMemoryUtil = expectedTotalUtil,
                };
                memorySampleWriter.ReceivedObjects[0].Should().BeEquivalentTo(expectedRecord);
            }
        }

        [Theory]
        [InlineData("Resource Manager: Exceeded foo",
            false, false, null, false, null, false, null)]
        [InlineData("Resource Manager: Exceeded sustained high CPU threshold above 75% for 1200 seconds.  This process has the highest usage of 94%",
            true, true, 94, false, null, false, null)]
        [InlineData("Resource Manager: Exceeded sustained high CPU threshold above 75% for 1200 seconds.  This process has the highest usage of 94% ... or 95%", // This is not a real log line
            true, true, 95, false, null, false, null)]
        [InlineData("Resource Manager: Exceeded sustained high CPU threshold above 75% for 1200 seconds.  This process has the highest usage of AAA%",
            false, false, null, false, null, false, null)]
        [InlineData("Resource Manager: Exceeded sustained high CPU threshold foo",
            false, false, null, false, null, false, null)]
        [InlineData("Resource Manager: Exceeded allowed memory usage per process. 47,413,526,528 bytes",
            true, false, null, true, 47413526528, false, null)]
        [InlineData("Resource Manager: Exceeded allowed memory usage per process. 47413526528 bytes",
            true, false, null, true, 47413526528, false, null)]
        [InlineData("Resource Manager: Exceeded allowed memory usage per process. foo bytes",
            false, false, null, false, null, false, null)]
        [InlineData("Resource Manager: Exceeded allowed memory usage across all processes. Memory info: 33,424,805,888 bytes (current process);56,026,918,912 bytes (Tableau total); 62,919,499,776 bytes (total of all processes); 17 (info count)",
            true, false, null, false, null, true, 33424805888)]
        [InlineData("Resource Manager: Exceeded allowed memory usage across all processes. Memory info: 33424805888 bytes (current process);56,026,918,912 bytes (Tableau total); 62,919,499,776 bytes (total of all processes); 17 (info count)",
            true, false, null, false, null, true, 33424805888)]
        [InlineData("Resource Manager: Exceeded allowed memory usage across all processes. Memory info: foo bytes (current process);56,026,918,912 bytes (Tableau total); 62,919,499,776 bytes (total of all processes); 17 (info count)",
            false, false, null, false, null, false, null)]
        public void ActionEvent(string payload, bool expectEvent, bool isCpuTermination, int? cpuValue, bool isProcessMemoryTermination, long? processMemoryValue, bool isTotalMemoryTermincation, long? totalMemoryValue)
        {
            var processingNotificationsCollector = new ProcessingNotificationsCollector(10);
            using (var eventProcessor = new ResourceManagerEventsProcessor(_testWriterFactory, processingNotificationsCollector))
            {
                eventProcessor.ProcessEvent(_testBaseEvent, payload, _testLogLine, TestProcessName);
            }
            
            var memorySampleWriter = _testWriterFactory.GetOneWriterAndVerifyOthersAreEmptyAndDisposed<ResourceManagerAction>("ResourceManagerActions", 5);

            memorySampleWriter.ReceivedObjects.Count.Should().Be(expectEvent ? 1 : 0);
            processingNotificationsCollector.TotalErrorsReported.Should().Be(expectEvent ? 0 : 1);
            if (expectEvent)
            {
                var expectedRecord = new
                    {
                        FileName = TestLogFileInfo.FileName,
                        FilePath = TestLogFileInfo.FilePath,
                        LineNumber = _testLogLine.LineNumber,
                        Timestamp = _testBaseEvent.Timestamp,
                        Worker = TestLogFileInfo.Worker,
                        
                        ProcessId = _testBaseEvent.ProcessId,
                        ProcessIndex = (int?)null,
                        ProcessName = TestProcessName,
                        
                        CpuUtilTermination = isCpuTermination,
                        CpuUtil = cpuValue,
                        ProcessMemoryUtilTermination = isProcessMemoryTermination,
                        ProcessMemoryUtil = processMemoryValue,
                        TotalMemoryUtilTermination = isTotalMemoryTermincation,
                        TotalMemoryUtil = totalMemoryValue,
                    };
                memorySampleWriter.ReceivedObjects[0].Should().BeEquivalentTo(expectedRecord);
            }
        }

        [Theory]
        [InlineData("Resource Manager: Max CPU limited to 95% over 3600 seconds", true, 95, null, null)]
        [InlineData("Resource Manager: Max CPU limited to AA% over 3600 seconds", false, null, null, null)]
        [InlineData("Resource Manager: Max CPU limited to foo", false, null, null, null)]
        [InlineData("Resource Manager: Per Process Memory Limit: 96,206,891,827 bytes", true, null, 96206891827, null)]
        [InlineData("Resource Manager: Per Process Memory Limit: 96206891827 bytes", true, null, 96206891827, null)]
        [InlineData("Resource Manager: Per Process Memory Limit: AA bytes", false, null, null, null)]
        [InlineData("Resource Manager: Per Process Memory Limit: foo", false, null, null, null)]
        [InlineData("Resource Manager: All Processes Memory Limit: 130,566,496,051 bytes", true, null, null, 130566496051)]
        [InlineData("Resource Manager: All Processes Memory Limit: 130566496051 bytes", true, null, null, 130566496051)]
        [InlineData("Resource Manager: All Processes Memory Limit: AA bytes", false, null, null, null)]
        [InlineData("Resource Manager: All Processes Memory Limit: foo", false, null, null, null)]
        public void ThresholdEvent(string payload, bool expectEvent, int? cpuLimit, long? perProcessMemoryLimit, long? totalMemoryLimit)
        {
            var processingNotificationsCollector = new ProcessingNotificationsCollector(10);
            using (var eventProcessor = new ResourceManagerEventsProcessor(_testWriterFactory, processingNotificationsCollector))
            {
                eventProcessor.ProcessEvent(_testBaseEvent, payload, _testLogLine, TestProcessName);
            }
            
            var memorySampleWriter = _testWriterFactory.GetOneWriterAndVerifyOthersAreEmptyAndDisposed<ResourceManagerThreshold>("ResourceManagerThresholds", 5);

            memorySampleWriter.ReceivedObjects.Count.Should().Be(expectEvent ? 1 : 0);
            processingNotificationsCollector.TotalErrorsReported.Should().Be(expectEvent ? 0 : 1);
            if (expectEvent)
            {
                var expectedRecord = new
                {
                    FileName = TestLogFileInfo.FileName,
                    FilePath = TestLogFileInfo.FilePath,
                    LineNumber = _testLogLine.LineNumber,
                    Timestamp = _testBaseEvent.Timestamp,
                    Worker = TestLogFileInfo.Worker,
                    
                    ProcessId = _testBaseEvent.ProcessId,
                    ProcessIndex = (int?)null,
                    ProcessName = TestProcessName,
                    
                    CpuLimit = cpuLimit,
                    PerProcessMemoryLimit = perProcessMemoryLimit,
                    TotalMemoryLimit = totalMemoryLimit,
                };
                memorySampleWriter.ReceivedObjects[0].Should().BeEquivalentTo(expectedRecord);
            }
        }

        [Theory]
        [InlineData("Resource Manager: Detected high CPU usage. 0%", true, 0)]
        [InlineData("Resource Manager: Detected high CPU usage. 90%", true, 90)]
        [InlineData("Resource Manager: Detected high CPU usage: 100%", true, 100)]
        [InlineData("Resource Manager: Detected high CPU usage. ERROR", false, 0)]
        [InlineData("Resource Manager: Detected high CPU usage. A%", false, 0)]
        public void HighCpuUsageEvents(string payload, bool expectEvent, int expectedCpuUsage)
        {
            var processingNotificationsCollector = new ProcessingNotificationsCollector(10);
            using (var eventProcessor = new ResourceManagerEventsProcessor(_testWriterFactory, processingNotificationsCollector))
            {
                eventProcessor.ProcessEvent(_testBaseEvent, payload, _testLogLine, TestProcessName);
            }

            var cpuUsageWriter = _testWriterFactory.GetOneWriterAndVerifyOthersAreEmptyAndDisposed<ResourceManagerHighCpuUsage>("ResourceManagerHighCpuUsages", 5);

            cpuUsageWriter.ReceivedObjects.Count.Should().Be(expectEvent ? 1 : 0);
            processingNotificationsCollector.TotalErrorsReported.Should().Be(expectEvent ? 0 : 1);
            if (expectEvent)
            {
                var expectedRecord = new
                {
                    FileName = TestLogFileInfo.FileName,
                    FilePath = TestLogFileInfo.FilePath,
                    LineNumber = _testLogLine.LineNumber,
                    Timestamp = _testBaseEvent.Timestamp,
                    Worker = TestLogFileInfo.Worker,

                    ProcessId = _testBaseEvent.ProcessId,
                    ProcessIndex = (int?)null,
                    ProcessName = TestProcessName,

                    CpuUsagePercent = expectedCpuUsage,
                };
                cpuUsageWriter.ReceivedObjects[0].Should().BeEquivalentTo(expectedRecord);
            }
        }
    }
}