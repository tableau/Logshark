using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using LogShark.Plugins.ResourceManager.Model;
using LogShark.Shared;
using LogShark.Shared.LogReading.Containers;
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

            testWriterFactory.AssertAllWritersAreDisposedAndEmpty(6);
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
            testWriterFactory.Writers.Count.Should().Be(6);
            var writerWithOutput = testWriterFactory.GetOneWriterAndVerifyOthersAreEmptyAndDisposed<ResourceManagerThreshold>("ResourceManagerThresholds", 6);
            ((List<object>)writerWithOutput.ReceivedObjects).Should().BeEquivalentTo(expectedOutput);
        }

        [Fact]
        public void HyperResourceMetricsTest()
        {
            var testWriterFactory = new TestWriterFactory();
            using (var plugin = new LogShark.Plugins.ResourceManager.ResourceManagerPlugin())
            {
                plugin.Configure(testWriterFactory, null, null, new NullLoggerFactory());

                // Test Hyper resource-metrics event
                var hyperResourceMetrics = new LogLine(new ReadLogLineResult(126, new NativeJsonLogsBaseEvent
                {
                    EventType = "resource-metrics",
                    EventPayload = JObject.Parse("{\"has-load\":true,\"memory\":{\"total_virtual_memory_mb\":139055,\"system_virtual_memory_mb\":20825,\"process_virtual_memory_mb\":160.75,\"total_physical_memory_mb\":130863,\"system_physical_memory_mb\":21637.3,\"process_physical_memory_mb\":181.449,\"process_file_mappings_mb\":0},\"mem-trackers\":{\"memory_tracked_current_usage_mb\":{\"global\":66.6963,\"global_network_writebuffer\":5,\"dbcache_resources_tracker\":0,\"global_network_readbuffer\":5.125,\"global_metrics\":0.176201,\"global_stringpool\":15.3125,\"global_tuple_data\":41.0088,\"global_locked\":0,\"global_transactions\":0.25,\"global_plan_cache\":17.0012,\"global_external_table_cache\":0,\"global_external_metadata\":0,\"global_disk_network_readbuffer\":0,\"global_disk_network_writebuffer\":0,\"global_disk_stringpool\":26,\"global_disk_transaction\":0,\"storage_layer_unflushed_memory\":0,\"storage_layer_temp_buffers\":0,\"io_cache\":0,\"global_disk_cache\":0},\"memory_tracked_peak_usage_mb\":{\"global\":139.782,\"global_network_writebuffer\":5,\"dbcache_resources_tracker\":0,\"global_network_readbuffer\":5.125,\"global_metrics\":0.176201,\"global_stringpool\":15.3125,\"global_tuple_data\":93.9354,\"global_locked\":0,\"global_transactions\":16.5625,\"global_plan_cache\":17.0012,\"global_external_table_cache\":0,\"global_external_metadata\":0,\"global_disk_network_readbuffer\":0,\"global_disk_network_writebuffer\":0,\"global_disk_stringpool\":26,\"global_disk_transaction\":0,\"storage_layer_unflushed_memory\":0,\"storage_layer_temp_buffers\":5.09839,\"io_cache\":0,\"global_disk_cache\":0}},\"volume-cache-trackers\":{\"volume_current_mb\":{},\"volume_peak_mb\":{}},\"load\":{\"overall_load\":0.0277778,\"scheduler_load\":0.0277778,\"workspace_load\":0,\"memory_load\":0.000642105,\"cpu_load\":0.0778158},\"cache-filesystem\":{\"cache_filesystem_available_mb\":{},\"cache_filesystem_size_mb\":{},\"cache_filesystem_cached_mb\":{},\"cache_filesystem_cached_peak_mb\":{},\"cache_filesystem_effectively_available_mb\":{}},\"scheduler-thread-count\":{\"scheduler_waiting_tasks_count\":0,\"scheduler_thread_count\":{\"active\":1,\"inactive\":53}}}"),
                    ProcessId = 333,
                    Timestamp = new DateTime(2019, 5, 6, 18, 0, 5, 816),
                }), HyperFileInfo);

                plugin.ProcessLogLine(hyperResourceMetrics, LogType.Hyper);
            }
            
            testWriterFactory.Writers.Count.Should().Be(6);
            var resourceMetricsWriter = testWriterFactory.GetOneWriterAndVerifyOthersAreEmptyAndDisposed<ResourceMetricsMemorySample>("ResourceMetricsMemorySamples", 6);
            resourceMetricsWriter.ReceivedObjects.Count.Should().Be(1);
            
            var expectedRecord = new
            {
                File = HyperFileInfo.FileName,
                Line = 126,
                Timestamp = new DateTime(2019, 5, 6, 18, 0, 5, 816),
                ProcessName = "hyper",
                Worker = HyperFileInfo.Worker,
                RequestId = (string)null,
                SessionId = (string)null,
                Site = (string)null,
                Username = (string)null,
                HasLoad = true,
                TotalVirtualMemoryGb = 139055.0 / 1024.0,
                SystemVirtualMemoryGb = 20825.0 / 1024.0,
                ProcessVirtualMemoryGb = 160.75 / 1024.0,
                TotalPhysicalMemoryGb = 130863.0 / 1024.0,
                SystemPhysicalMemoryGb = 21637.3 / 1024.0,
                ProcessPhysicalMemoryGb = 181.449 / 1024.0,
                ProcessFileMappingsGb = 0.0 / 1024.0,
                MemoryTrackedCurrentGlobalMb = 66.6963,
                MemoryTrackedCurrentGlobalNetworkWritebufferMb = 5.0,
                MemoryTrackedCurrentGlobalTupleDataMb = 41.0088,
                MemoryTrackedPeakGlobalMb = 139.782,
                MemoryTrackedPeakGlobalTupleDataMb = 93.9354,
                OverallLoad = 0.0277778,
                SchedulerLoad = 0.0277778,
                WorkspaceLoad = 0.0,
                MemoryLoad = 0.000642105,
                CpuLoad = 0.0778158,
                SchedulerWaitingTasksCount = 0,
                SchedulerThreadCountActive = 1,
                SchedulerThreadCountInactive = 53
            };
            resourceMetricsWriter.ReceivedObjects[0].Should().BeEquivalentTo(expectedRecord, options => options.ExcludingMissingMembers());
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
            }
        };
    }
}