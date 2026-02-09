using System;
using FluentAssertions;
using LogShark.Plugins.ResourceManager;
using LogShark.Plugins.ResourceManager.Model;
using LogShark.Shared.LogReading.Containers;
using LogShark.Tests.Plugins.Helpers;
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
            
            _testWriterFactory.AssertAllWritersAreDisposedAndEmpty(6);
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
            
            var cpuSampleWriter = _testWriterFactory.GetOneWriterAndVerifyOthersAreEmptyAndDisposed<ResourceManagerCpuSample>("ResourceManagerCpuSamples", 6);

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
            
            var memorySampleWriter = _testWriterFactory.GetOneWriterAndVerifyOthersAreEmptyAndDisposed<ResourceManagerMemorySample>("ResourceManagerMemorySamples", 6);

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
            false, false, null, false, null, null, false, null)]
        [InlineData("Resource Manager: Exceeded sustained high CPU threshold above 75% for 1200 seconds.  This process has the highest usage of 94%",
            true, true, 94, false, null, null, false, null)]
        [InlineData("Resource Manager: Exceeded sustained high CPU threshold above 75% for 1200 seconds.  This process has the highest usage of 94% ... or 95%", // This is not a real log line
            true, true, 95, false, null, null, false, null)]
        [InlineData("Resource Manager: Exceeded sustained high CPU threshold above 75% for 1200 seconds.  This process has the highest usage of AAA%",
            false, false, null, false, null, null, false, null)]
        [InlineData("Resource Manager: Exceeded sustained high CPU threshold foo",
            false, false, null, false, null, null, false, null)]
        [InlineData("Resource Manager: Exceeded allowed memory usage per process. 47,413,526,528 bytes",
            true, false, null, true, 47413526528, null, false, null)]
        [InlineData("Resource Manager: Exceeded allowed memory usage per process. 47413526528 bytes",
            true, false, null, true, 47413526528, null, false, null)]
        [InlineData("Resource Manager: Exceeded allowed memory usage per process. foo bytes",
            false, false, null, false, null, null, false, null)]
        [InlineData("Resource Manager: Exceeded allowed memory usage across all processes. Memory info: 33,424,805,888 bytes (current process);56,026,918,912 bytes (Tableau total); 62,919,499,776 bytes (total of all processes); 17 (info count)",
            true, false, null, false, 33424805888, 56026918912, true, 62919499776)]
        [InlineData("Resource Manager: Exceeded allowed memory usage across all processes. Memory info: 33424805888 bytes (current process);56026918912 bytes (Tableau total); 62919499776 bytes (total of all processes); 17 (info count)",
            true, false, null, false, 33424805888, 56026918912, true, 62919499776)]
        [InlineData("Resource Manager: Exceeded allowed memory usage across all processes. Memory info: 33,424,805,888 bytes (current process);bar bytes (Tableau total); 65,419,499,776 bytes (total of all processes); 17 (info count)",
            true, false, null, false, 33424805888, null, true, 65419499776)]
        [InlineData("Resource Manager: Exceeded allowed memory usage across all processes. Memory info: foo bytes (current process);bar bytes (Tableau total); 62,919,499,776 bytes (total of all processes); 17 (info count)",
            true, false, null, false, null, null, true, 62919499776)]
        [InlineData("Resource Manager: Exceeded allowed memory usage across all processes. Memory info: foo bytes (current process);bar bytes (Tableau total); foo bytes (total of all processes); 17 (info count)",
            false, false, null, false, null, null, false, null)]
        public void ActionEvent(string payload, bool expectEvent, bool isCpuTermination, int? cpuValue, bool isProcessMemoryTermination, long? processMemoryValue, long? tableauTotalMemoryValue, bool isTotalMemoryTermination, long? totalMemoryValue)
        {
            var processingNotificationsCollector = new ProcessingNotificationsCollector(10);
            using (var eventProcessor = new ResourceManagerEventsProcessor(_testWriterFactory, processingNotificationsCollector))
            {
                eventProcessor.ProcessEvent(_testBaseEvent, payload, _testLogLine, TestProcessName);
            }
            
            var memorySampleWriter = _testWriterFactory.GetOneWriterAndVerifyOthersAreEmptyAndDisposed<ResourceManagerAction>("ResourceManagerActions", 6);

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
                        TableauTotalMemoryUtil = tableauTotalMemoryValue,
                        TotalMemoryUtilTermination = isTotalMemoryTermination,
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
            
            var memorySampleWriter = _testWriterFactory.GetOneWriterAndVerifyOthersAreEmptyAndDisposed<ResourceManagerThreshold>("ResourceManagerThresholds", 6);

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
            
            var cpuUsageWriter = _testWriterFactory.GetOneWriterAndVerifyOthersAreEmptyAndDisposed<ResourceManagerHighCpuUsage>("ResourceManagerHighCpuUsages", 6);

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

        [Theory]
        [InlineData("{\"has-load\":true,\"memory\":{\"total_physical_memory_mb\":130863.0,\"system_physical_memory_mb\":21637.3,\"process_physical_memory_mb\":181.449}}", 
            true, true, 130863.0, 21637.3, 181.449)]
        [InlineData("{\"has-load\":true,\"memory\":{\"total_virtual_memory_mb\":139055,\"system_virtual_memory_mb\":20825,\"process_virtual_memory_mb\":160.75,\"total_physical_memory_mb\":130863,\"system_physical_memory_mb\":21637.3,\"process_physical_memory_mb\":181.449,\"process_file_mappings_mb\":0},\"mem-trackers\":{\"memory_tracked_current_usage_mb\":{\"global\":66.6963,\"global_network_writebuffer\":5,\"global_tuple_data\":41.0088},\"memory_tracked_peak_usage_mb\":{\"global\":139.782,\"global_tuple_data\":93.9354}},\"load\":{\"overall_load\":0.0277778,\"scheduler_load\":0.0277778,\"workspace_load\":0,\"memory_load\":0.000642105,\"cpu_load\":0.0778158},\"scheduler-thread-count\":{\"scheduler_waiting_tasks_count\":0,\"scheduler_thread_count\":{\"active\":1,\"inactive\":53}}}", 
            true, true, 130863.0, 21637.3, 181.449)]
        [InlineData("{\"has-load\":true,\"memory\":{\"total_physical_memory_mb\":100.5,\"system_physical_memory_mb\":50.25}}", 
            true, true, 100.5, 50.25, null)]
        [InlineData("{\"has-load\":true,\"memory\":{\"process_physical_memory_mb\":75.123}}", 
            true, true, null, null, 75.123)]
        [InlineData("{\"has-load\":true,\"memory\":{}}", 
            true, true, null, null, null)]
        [InlineData("{\"has-load\":false,\"memory\":{\"total_physical_memory_mb\":130863.0}}", 
            true, false, 130863.0, null, null)]
        [InlineData("invalid json with has-load and memory", 
            false, null, null, null, null)] // This will trigger AddHyperMemorySampleEvent but fail JSON parsing
        [InlineData("{\"has-load\":true,\"memory\":{\"invalid\":\"structure\"}}", 
            true, true, null, null, null)]
        [InlineData("{\"has-load\":true}", 
            false, null, null, null, null)] // No "memory" string, so won't call AddHyperMemorySampleEvent
        [InlineData("no matching strings", 
            false, null, null, null, null)] // No "has-load" or "memory" strings
        public void ResourceMetricsMemorySampleEvents(string payload, bool expectEvent, bool? expectedHasLoad, 
            double? expectedTotalPhysicalMemory, double? expectedSystemPhysicalMemory, double? expectedProcessPhysicalMemory)
        {
            var processingNotificationsCollector = new ProcessingNotificationsCollector(10);
            using (var eventProcessor = new ResourceManagerEventsProcessor(_testWriterFactory, processingNotificationsCollector))
            {
                eventProcessor.ProcessEvent(_testBaseEvent, payload, _testLogLine, "hyper");
            }
            
            var resourceMetricsWriter = _testWriterFactory.GetOneWriterAndVerifyOthersAreEmptyAndDisposed<ResourceMetricsMemorySample>("ResourceMetricsMemorySamples", 6);

            resourceMetricsWriter.ReceivedObjects.Count.Should().Be(expectEvent ? 1 : 0);
            
            // Only expect errors when we actually call AddHyperMemorySample (i.e., when payload contains both "has-load" and "memory")
            var shouldCallAddHyperMemorySample = payload.Contains("has-load") && payload.Contains("memory");
            var shouldReportError = shouldCallAddHyperMemorySample && !expectEvent;
            processingNotificationsCollector.TotalErrorsReported.Should().Be(shouldReportError ? 1 : 0);
            
            if (expectEvent)
            {
                var expectedRecord = new
                {
                    File = TestLogFileInfo.FileName,
                    Line = _testLogLine.LineNumber,
                    Timestamp = _testBaseEvent.Timestamp,
                    ProcessName = "hyper",
                    Worker = TestLogFileInfo.Worker,
                    RequestId = (string)null,
                    SessionId = (string)null,
                    Site = (string)null,
                    Username = (string)null,
                    HasLoad = expectedHasLoad,
                    TotalPhysicalMemoryGb = expectedTotalPhysicalMemory / 1024.0,
                    SystemPhysicalMemoryGb = expectedSystemPhysicalMemory / 1024.0,
                    ProcessPhysicalMemoryGb = expectedProcessPhysicalMemory / 1024.0,
                };
                resourceMetricsWriter.ReceivedObjects[0].Should().BeEquivalentTo(expectedRecord, options => options.ExcludingMissingMembers());
            }
        }

        [Fact]
        public void ComprehensiveResourceMetricsTest()
        {
            var comprehensiveJson = "{\"has-load\":true,\"memory\":{\"total_virtual_memory_mb\":139055,\"system_virtual_memory_mb\":20825,\"process_virtual_memory_mb\":160.75,\"total_physical_memory_mb\":130863,\"system_physical_memory_mb\":21637.3,\"process_physical_memory_mb\":181.449,\"process_file_mappings_mb\":0},\"mem-trackers\":{\"memory_tracked_current_usage_mb\":{\"global\":66.6963,\"global_network_writebuffer\":5,\"global_tuple_data\":41.0088},\"memory_tracked_peak_usage_mb\":{\"global\":139.782,\"global_tuple_data\":93.9354}},\"load\":{\"overall_load\":0.0277778,\"scheduler_load\":0.0277778,\"workspace_load\":0,\"memory_load\":0.000642105,\"cpu_load\":0.0778158},\"scheduler-thread-count\":{\"scheduler_waiting_tasks_count\":0,\"scheduler_thread_count\":{\"active\":1,\"inactive\":53}}}";
            
            var processingNotificationsCollector = new ProcessingNotificationsCollector(10);
            using (var eventProcessor = new ResourceManagerEventsProcessor(_testWriterFactory, processingNotificationsCollector))
            {
                eventProcessor.ProcessEvent(_testBaseEvent, comprehensiveJson, _testLogLine, "hyper");
            }
            
            var resourceMetricsWriter = _testWriterFactory.GetOneWriterAndVerifyOthersAreEmptyAndDisposed<ResourceMetricsMemorySample>("ResourceMetricsMemorySamples", 6);
            resourceMetricsWriter.ReceivedObjects.Count.Should().Be(1);
            
            var result = (ResourceMetricsMemorySample)resourceMetricsWriter.ReceivedObjects[0];
            
            // Verify comprehensive fields are populated
            result.HasLoad.Should().Be(true);
            result.TotalVirtualMemoryGb.Should().Be(139055 / 1024.0);
            result.SystemVirtualMemoryGb.Should().Be(20825 / 1024.0);
            result.ProcessVirtualMemoryGb.Should().Be(160.75 / 1024.0);
            result.ProcessFileMappingsGb.Should().Be(0 / 1024.0);
            result.MemoryTrackedCurrentGlobalMb.Should().Be(66.6963);
            result.MemoryTrackedCurrentGlobalNetworkWritebufferMb.Should().Be(5);
            result.MemoryTrackedCurrentGlobalTupleDataMb.Should().Be(41.0088);
            result.MemoryTrackedPeakGlobalMb.Should().Be(139.782);
            result.MemoryTrackedPeakGlobalTupleDataMb.Should().Be(93.9354);
            result.OverallLoad.Should().Be(0.0277778);
            result.SchedulerLoad.Should().Be(0.0277778);
            result.WorkspaceLoad.Should().Be(0);
            result.MemoryLoad.Should().Be(0.000642105);
            result.CpuLoad.Should().Be(0.0778158);
            result.SchedulerWaitingTasksCount.Should().Be(0);
            result.SchedulerThreadCountActive.Should().Be(1);
            result.SchedulerThreadCountInactive.Should().Be(53);
        }
    }
}