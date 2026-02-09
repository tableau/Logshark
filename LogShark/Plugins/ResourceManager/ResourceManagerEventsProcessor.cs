using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using LogShark.Containers;
using LogShark.Plugins.ResourceManager.Model;
using LogShark.Shared;
using LogShark.Shared.LogReading.Containers;
using LogShark.Writers;
using LogShark.Writers.Containers;
using Newtonsoft.Json.Linq;

namespace LogShark.Plugins.ResourceManager
{
    public class ResourceManagerEventsProcessor : IDisposable
    {
        private static readonly DataSetInfo ActionsDataSetInfo = new DataSetInfo("ResourceManager", "ResourceManagerActions");
        private static readonly DataSetInfo CpuSamplesDataSetInfo = new DataSetInfo("ResourceManager", "ResourceManagerCpuSamples");
        private static readonly DataSetInfo MemorySamplesDataSetInfo = new DataSetInfo("ResourceManager", "ResourceManagerMemorySamples");
        private static readonly DataSetInfo ResourceMetricsMemorySamplesDataSetInfo = new DataSetInfo("ResourceManager", "ResourceMetricsMemorySamples");
        private static readonly DataSetInfo ThresholdsDataSetInfo = new DataSetInfo("ResourceManager", "ResourceManagerThresholds");
        private static readonly DataSetInfo HighCpuUsageDataSetInfo = new DataSetInfo("ResourceManager", "ResourceManagerHighCpuUsages");

        private readonly IWriter<ResourceManagerAction> _actionsWriter;
        private readonly IWriter<ResourceManagerCpuSample> _cpuSamplesWriter;
        private readonly IWriter<ResourceManagerMemorySample> _memorySamplesWriter;
        private readonly IWriter<ResourceMetricsMemorySample> _resourceMetricsMemorySamplesWriter;
        private readonly IWriter<ResourceManagerThreshold> _thresholdsWriters;
        private readonly IWriter<ResourceManagerHighCpuUsage> _cpuUsageWriter;

        private readonly IProcessingNotificationsCollector _processingNotificationsCollector;

        public ResourceManagerEventsProcessor(IWriterFactory writerFactory, IProcessingNotificationsCollector processingNotificationsCollector)
        {
            _processingNotificationsCollector = processingNotificationsCollector;
            
            _actionsWriter = writerFactory.GetWriter<ResourceManagerAction>(ActionsDataSetInfo);
            _cpuSamplesWriter = writerFactory.GetWriter<ResourceManagerCpuSample>(CpuSamplesDataSetInfo);
            _memorySamplesWriter = writerFactory.GetWriter<ResourceManagerMemorySample>(MemorySamplesDataSetInfo);
            _resourceMetricsMemorySamplesWriter = writerFactory.GetWriter<ResourceMetricsMemorySample>(ResourceMetricsMemorySamplesDataSetInfo);
            _thresholdsWriters = writerFactory.GetWriter<ResourceManagerThreshold>(ThresholdsDataSetInfo);
            _cpuUsageWriter = writerFactory.GetWriter<ResourceManagerHighCpuUsage>(HighCpuUsageDataSetInfo);
        }

        public void ProcessEvent(NativeJsonLogsBaseEvent baseEvent, string message, LogLine logLine, string processName)
        { 
            if(processName== "hyper")
            {
                if (message.Contains("has-load") && message.Contains("memory"))
                {
                    AddHyperMemorySampleEvent(baseEvent,message,logLine,processName);
                    return;
                }
               
            }
            if (!message?.StartsWith("Resource Manager") ?? true)
            {
                return;
            }
            
            if (message.StartsWith("Resource Manager: CPU info:"))
            {
                AddCpuSampleEvent(baseEvent, message, logLine, processName);
                return;
            }

            if (message.StartsWith("Resource Manager: Memory info:"))
            {
                AddMemorySampleEvent(baseEvent, message, logLine, processName);
                return;
            }

            if (message.StartsWith("Resource Manager: Exceeded"))
            {
                AddActionEvent(baseEvent, message, logLine, processName);
                return;
            }

            if (message.StartsWith("Resource Manager: Max CPU limited to"))
            {
                AddCpuLimitEvent(baseEvent, message, logLine, processName);
                return;
            }

            if (message.StartsWith("Resource Manager: Per Process Memory Limit:"))
            {
                AddPerProcessMemoryLimitEvent(baseEvent, message, logLine, processName);
                return;
            }

            if (message.StartsWith("Resource Manager: All Processes Memory Limit:"))
            {
                AddTotalMemoryLimitEvent(baseEvent, message, logLine, processName);
                return;
            }

            if (message.StartsWith("Resource Manager: Detected high CPU usage"))
            {
                AddHighCpuUsageEvent(baseEvent, message, logLine, processName);
                return;
            }
        }

        public IEnumerable<WriterLineCounts> CompleteProcessing()
        {
            return new List<WriterLineCounts>
            {
                _actionsWriter.Close(),
                _cpuSamplesWriter.Close(),
                _memorySamplesWriter.Close(),
                _resourceMetricsMemorySamplesWriter.Close(),
                _thresholdsWriters.Close(),
                _cpuUsageWriter.Close()
            };
        }

        public void Dispose()
        {
            _actionsWriter.Dispose();
            _cpuSamplesWriter.Dispose();
            _memorySamplesWriter.Dispose();
            _resourceMetricsMemorySamplesWriter.Dispose();
            _thresholdsWriters.Dispose();
            _cpuUsageWriter.Dispose();
        }
        
        private static readonly Regex CurrentAndTotalCpuUtilRegex = new Regex(@".*\s(?<current_process_util>\d+?)%.*\s(?<total_processes_util>\d+?)%.*", RegexOptions.Compiled | RegexOptions.ExplicitCapture);
        private static readonly Regex CurrentCpuUtilRegex = new Regex(@".*\s(?<current_process_util>\d+?)%.*", RegexOptions.Compiled | RegexOptions.ExplicitCapture);
        private void AddCpuSampleEvent(NativeJsonLogsBaseEvent baseEvent, string message, LogLine logLine, string processName)
        {
            ResourceManagerCpuSample record = null;

            var currentAndTotalMatch = CurrentAndTotalCpuUtilRegex.Match(message);
            if (currentAndTotalMatch.Success)
            {
                record = ResourceManagerCpuSample.GetEventWithNullCheck(
                    baseEvent, 
                    logLine,
                    processName, 
                    TryParseIntWithLogging(currentAndTotalMatch, "current_process_util", logLine),
                    TryParseIntWithLogging(currentAndTotalMatch, "total_processes_util", logLine)
                );

                _cpuSamplesWriter.AddLine(record);
                return;
            }
            
            var currentMatch = CurrentCpuUtilRegex.Match(message);
            if (currentMatch.Success)
            {
                record = ResourceManagerCpuSample.GetEventWithNullCheck(
                    baseEvent,
                    logLine,
                    processName,
                    TryParseIntWithLogging(currentMatch, "current_process_util", logLine),
                    null
                );

                _cpuSamplesWriter.AddLine(record);
                return;
            }

            _processingNotificationsCollector.ReportError("Failed to process line as CpuSampleEvent.", logLine, nameof(ResourceManagerPlugin));
        }
        
        // Gets byte counts for current & total utilization by greedily capturing numeric sequences (composed of digits and comma separators) from between the ":" & "bytes" and ";" & "bytes" token pairs.
        private static readonly Regex CurrentAndTotalMemoryUtilRegex = new Regex(@".*: (?<current_process_util>[\d,]+?) bytes.*;(?<tableau_total_util>[\d,]+?) bytes.*; (?<total_util>[\d,]+?) bytes", RegexOptions.Compiled | RegexOptions.ExplicitCapture);
        private void AddMemorySampleEvent(NativeJsonLogsBaseEvent baseEvent, string message, LogLine logLine, string processName)
        {
            if (processName=="hyper") //due to a space after semicolon in hyper logs only - https://sourcegraph.prod.tableautools.com/github.com/sf-analyticscloud/monolith@252/tableau-2024-3/-/blob/modules/foundation/lego/tabsys/main/monitor/ResourceManagerImpl.cpp?L283-285
            {
                message = message.Replace("(current process); ", "(current process);");
            }
                var currentAndTotalMatch = CurrentAndTotalMemoryUtilRegex.Match(message);
            if (currentAndTotalMatch.Success)
            {
                var record = ResourceManagerMemorySample.GetEventWithNullCheck(
                    baseEvent,
                    logLine,
                    processName,
                    TryParseLongWithLogging(currentAndTotalMatch, "current_process_util", logLine),
                    TryParseLongWithLogging(currentAndTotalMatch, "tableau_total_util", logLine),
                    TryParseLongWithLogging(currentAndTotalMatch, "total_util", logLine)
                );

                _memorySamplesWriter.AddLine(record);
                return;
            }

            _processingNotificationsCollector.ReportError("Failed to process line as MemorySampleEvent.", logLine, nameof(ResourceManagerPlugin));
        }

        private void AddHyperMemorySampleEvent(NativeJsonLogsBaseEvent baseEvent, string message, LogLine logLine, string processName)
        {
            try
            {
                // Parse the JSON message to extract all resource metrics information
                var jsonObject = JObject.Parse(message);
                
                // Extract has-load
                bool? hasLoad = jsonObject["has-load"]?.ToObject<bool?>();
                
                // Extract memory information (convert from MB to GB)
                var memoryObject = jsonObject["memory"];
                double? totalVirtualMemoryGb = ConvertMbToGb(memoryObject?["total_virtual_memory_mb"]?.ToObject<double?>());
                double? systemVirtualMemoryGb = ConvertMbToGb(memoryObject?["system_virtual_memory_mb"]?.ToObject<double?>());
                double? processVirtualMemoryGb = ConvertMbToGb(memoryObject?["process_virtual_memory_mb"]?.ToObject<double?>());
                double? totalPhysicalMemoryGb = ConvertMbToGb(memoryObject?["total_physical_memory_mb"]?.ToObject<double?>());
                double? systemPhysicalMemoryGb = ConvertMbToGb(memoryObject?["system_physical_memory_mb"]?.ToObject<double?>());
                double? processPhysicalMemoryGb = ConvertMbToGb(memoryObject?["process_physical_memory_mb"]?.ToObject<double?>());
                double? processFileMappingsGb = ConvertMbToGb(memoryObject?["process_file_mappings_mb"]?.ToObject<double?>());
                
                // Extract memory trackers - current usage (keep in MB - not converted)
                var memTrackersObject = jsonObject["mem-trackers"];
                var currentUsageObject = memTrackersObject?["memory_tracked_current_usage_mb"];
                double? memoryTrackedCurrentGlobalMb = currentUsageObject?["global"]?.ToObject<double?>();
                double? memoryTrackedCurrentGlobalNetworkWritebufferMb = currentUsageObject?["global_network_writebuffer"]?.ToObject<double?>();
                double? memoryTrackedCurrentDbcacheResourcesTrackerMb = currentUsageObject?["dbcache_resources_tracker"]?.ToObject<double?>();
                double? memoryTrackedCurrentGlobalNetworkReadbufferMb = currentUsageObject?["global_network_readbuffer"]?.ToObject<double?>();
                double? memoryTrackedCurrentGlobalMetricsMb = currentUsageObject?["global_metrics"]?.ToObject<double?>();
                double? memoryTrackedCurrentGlobalStringpoolMb = currentUsageObject?["global_stringpool"]?.ToObject<double?>();
                double? memoryTrackedCurrentGlobalTupleDataMb = currentUsageObject?["global_tuple_data"]?.ToObject<double?>();
                double? memoryTrackedCurrentGlobalLockedMb = currentUsageObject?["global_locked"]?.ToObject<double?>();
                double? memoryTrackedCurrentGlobalTransactionsMb = currentUsageObject?["global_transactions"]?.ToObject<double?>();
                double? memoryTrackedCurrentGlobalPlanCacheMb = currentUsageObject?["global_plan_cache"]?.ToObject<double?>();
                double? memoryTrackedCurrentGlobalExternalTableCacheMb = currentUsageObject?["global_external_table_cache"]?.ToObject<double?>();
                double? memoryTrackedCurrentGlobalExternalMetadataMb = currentUsageObject?["global_external_metadata"]?.ToObject<double?>();
                double? memoryTrackedCurrentGlobalDiskNetworkReadbufferMb = currentUsageObject?["global_disk_network_readbuffer"]?.ToObject<double?>();
                double? memoryTrackedCurrentGlobalDiskNetworkWritebufferMb = currentUsageObject?["global_disk_network_writebuffer"]?.ToObject<double?>();
                double? memoryTrackedCurrentGlobalDiskStringpoolMb = currentUsageObject?["global_disk_stringpool"]?.ToObject<double?>();
                double? memoryTrackedCurrentGlobalDiskTransactionMb = currentUsageObject?["global_disk_transaction"]?.ToObject<double?>();
                double? memoryTrackedCurrentStorageLayerUnflushedMemoryMb = currentUsageObject?["storage_layer_unflushed_memory"]?.ToObject<double?>();
                double? memoryTrackedCurrentStorageLayerTempBuffersMb = currentUsageObject?["storage_layer_temp_buffers"]?.ToObject<double?>();
                double? memoryTrackedCurrentIoCacheMb = currentUsageObject?["io_cache"]?.ToObject<double?>();
                double? memoryTrackedCurrentGlobalDiskCacheMb = currentUsageObject?["global_disk_cache"]?.ToObject<double?>();
                
                // Extract memory trackers - peak usage (keep in MB - not converted)
                var peakUsageObject = memTrackersObject?["memory_tracked_peak_usage_mb"];
                double? memoryTrackedPeakGlobalMb = peakUsageObject?["global"]?.ToObject<double?>();
                double? memoryTrackedPeakGlobalNetworkWritebufferMb = peakUsageObject?["global_network_writebuffer"]?.ToObject<double?>();
                double? memoryTrackedPeakDbcacheResourcesTrackerMb = peakUsageObject?["dbcache_resources_tracker"]?.ToObject<double?>();
                double? memoryTrackedPeakGlobalNetworkReadbufferMb = peakUsageObject?["global_network_readbuffer"]?.ToObject<double?>();
                double? memoryTrackedPeakGlobalMetricsMb = peakUsageObject?["global_metrics"]?.ToObject<double?>();
                double? memoryTrackedPeakGlobalStringpoolMb = peakUsageObject?["global_stringpool"]?.ToObject<double?>();
                double? memoryTrackedPeakGlobalTupleDataMb = peakUsageObject?["global_tuple_data"]?.ToObject<double?>();
                double? memoryTrackedPeakGlobalLockedMb = peakUsageObject?["global_locked"]?.ToObject<double?>();
                double? memoryTrackedPeakGlobalTransactionsMb = peakUsageObject?["global_transactions"]?.ToObject<double?>();
                double? memoryTrackedPeakGlobalPlanCacheMb = peakUsageObject?["global_plan_cache"]?.ToObject<double?>();
                double? memoryTrackedPeakGlobalExternalTableCacheMb = peakUsageObject?["global_external_table_cache"]?.ToObject<double?>();
                double? memoryTrackedPeakGlobalExternalMetadataMb = peakUsageObject?["global_external_metadata"]?.ToObject<double?>();
                double? memoryTrackedPeakGlobalDiskNetworkReadbufferMb = peakUsageObject?["global_disk_network_readbuffer"]?.ToObject<double?>();
                double? memoryTrackedPeakGlobalDiskNetworkWritebufferMb = peakUsageObject?["global_disk_network_writebuffer"]?.ToObject<double?>();
                double? memoryTrackedPeakGlobalDiskStringpoolMb = peakUsageObject?["global_disk_stringpool"]?.ToObject<double?>();
                double? memoryTrackedPeakGlobalDiskTransactionMb = peakUsageObject?["global_disk_transaction"]?.ToObject<double?>();
                double? memoryTrackedPeakStorageLayerUnflushedMemoryMb = peakUsageObject?["storage_layer_unflushed_memory"]?.ToObject<double?>();
                double? memoryTrackedPeakStorageLayerTempBuffersMb = peakUsageObject?["storage_layer_temp_buffers"]?.ToObject<double?>();
                double? memoryTrackedPeakIoCacheMb = peakUsageObject?["io_cache"]?.ToObject<double?>();
                double? memoryTrackedPeakGlobalDiskCacheMb = peakUsageObject?["global_disk_cache"]?.ToObject<double?>();
                
                // Extract load information
                var loadObject = jsonObject["load"];
                double? overallLoad = loadObject?["overall_load"]?.ToObject<double?>();
                double? schedulerLoad = loadObject?["scheduler_load"]?.ToObject<double?>();
                double? workspaceLoad = loadObject?["workspace_load"]?.ToObject<double?>();
                double? memoryLoad = loadObject?["memory_load"]?.ToObject<double?>();
                double? cpuLoad = loadObject?["cpu_load"]?.ToObject<double?>();
                
                // Extract scheduler thread count information
                var schedulerThreadCountObject = jsonObject["scheduler-thread-count"];
                int? schedulerWaitingTasksCount = schedulerThreadCountObject?["scheduler_waiting_tasks_count"]?.ToObject<int?>();
                var threadCountObject = schedulerThreadCountObject?["scheduler_thread_count"];
                int? schedulerThreadCountActive = threadCountObject?["active"]?.ToObject<int?>();
                int? schedulerThreadCountInactive = threadCountObject?["inactive"]?.ToObject<int?>();

                var record = ResourceMetricsMemorySample.GetEventWithNullCheck(
                    baseEvent,
                    logLine,
                    processName,
                    hasLoad,
                    totalVirtualMemoryGb,
                    systemVirtualMemoryGb,
                    processVirtualMemoryGb,
                    totalPhysicalMemoryGb,
                    systemPhysicalMemoryGb,
                    processPhysicalMemoryGb,
                    processFileMappingsGb,
                    memoryTrackedCurrentGlobalMb,
                    memoryTrackedCurrentGlobalNetworkWritebufferMb,
                    memoryTrackedCurrentDbcacheResourcesTrackerMb,
                    memoryTrackedCurrentGlobalNetworkReadbufferMb,
                    memoryTrackedCurrentGlobalMetricsMb,
                    memoryTrackedCurrentGlobalStringpoolMb,
                    memoryTrackedCurrentGlobalTupleDataMb,
                    memoryTrackedCurrentGlobalLockedMb,
                    memoryTrackedCurrentGlobalTransactionsMb,
                    memoryTrackedCurrentGlobalPlanCacheMb,
                    memoryTrackedCurrentGlobalExternalTableCacheMb,
                    memoryTrackedCurrentGlobalExternalMetadataMb,
                    memoryTrackedCurrentGlobalDiskNetworkReadbufferMb,
                    memoryTrackedCurrentGlobalDiskNetworkWritebufferMb,
                    memoryTrackedCurrentGlobalDiskStringpoolMb,
                    memoryTrackedCurrentGlobalDiskTransactionMb,
                    memoryTrackedCurrentStorageLayerUnflushedMemoryMb,
                    memoryTrackedCurrentStorageLayerTempBuffersMb,
                    memoryTrackedCurrentIoCacheMb,
                    memoryTrackedCurrentGlobalDiskCacheMb,
                    memoryTrackedPeakGlobalMb,
                    memoryTrackedPeakGlobalNetworkWritebufferMb,
                    memoryTrackedPeakDbcacheResourcesTrackerMb,
                    memoryTrackedPeakGlobalNetworkReadbufferMb,
                    memoryTrackedPeakGlobalMetricsMb,
                    memoryTrackedPeakGlobalStringpoolMb,
                    memoryTrackedPeakGlobalTupleDataMb,
                    memoryTrackedPeakGlobalLockedMb,
                    memoryTrackedPeakGlobalTransactionsMb,
                    memoryTrackedPeakGlobalPlanCacheMb,
                    memoryTrackedPeakGlobalExternalTableCacheMb,
                    memoryTrackedPeakGlobalExternalMetadataMb,
                    memoryTrackedPeakGlobalDiskNetworkReadbufferMb,
                    memoryTrackedPeakGlobalDiskNetworkWritebufferMb,
                    memoryTrackedPeakGlobalDiskStringpoolMb,
                    memoryTrackedPeakGlobalDiskTransactionMb,
                    memoryTrackedPeakStorageLayerUnflushedMemoryMb,
                    memoryTrackedPeakStorageLayerTempBuffersMb,
                    memoryTrackedPeakIoCacheMb,
                    memoryTrackedPeakGlobalDiskCacheMb,
                    overallLoad,
                    schedulerLoad,
                    workspaceLoad,
                    memoryLoad,
                    cpuLoad,
                    schedulerWaitingTasksCount,
                    schedulerThreadCountActive,
                    schedulerThreadCountInactive
                );

                _resourceMetricsMemorySamplesWriter.AddLine(record);
                return;
            }
            catch (Exception ex)
            {
                _processingNotificationsCollector.ReportError($"Failed to parse Hyper resource-metrics JSON: {ex.Message}", logLine, nameof(ResourceManagerPlugin));
                return;
            }
        }

        // Extract an optionally comma-separated numeric total memory byte count value (and optional process memory byte count) from the end of a static Resource Manager string
        private static readonly Regex TotalMemoryUsageExceededRegex = new Regex(@"Resource Manager: Exceeded allowed memory usage across all processes\D+\s(?<process_usage>[\d,]+?)?\s?bytes\s\(current process\)\D+(?<tableau_usage>[\d,]+?)?\sbytes\s\(Tableau total\)\D+(?<total_usage>[\d,]+?)\sbytes\s\(total of all processes\)", RegexOptions.Compiled | RegexOptions.ExplicitCapture);
        // Extract an optionally comma-separated numeric process memory byte count value from the end of a static Resource Manager string
        private static readonly Regex ProcessMemoryUsageExceededRegex = new Regex(@"^Resource Manager: Exceeded allowed memory usage per process.\s(?<process_usage>[\d,]+?)\s.*", RegexOptions.Compiled | RegexOptions.ExplicitCapture);
        private static readonly Regex CpuUsageExceededRegex = new Regex(@"^Resource Manager: Exceeded sustained high CPU threshold above\s(?<cpu_threshold>\d+?)%.*\s(?<duration>\d+?)\s.*\s(?<process_cpu_util>\d+?)%", RegexOptions.Compiled | RegexOptions.ExplicitCapture);
        private void AddActionEvent(NativeJsonLogsBaseEvent baseEvent, string message, LogLine logLine, string processName)
        {
            ResourceManagerAction record = null;

            var cpuUsageMatch = CpuUsageExceededRegex.Match(message);
            if (cpuUsageMatch.Success)
            {
                record = ResourceManagerAction.GetCpuTerminationEvent(
                    baseEvent,
                    logLine,
                    processName,
                    TryParseIntWithLogging(cpuUsageMatch, "process_cpu_util", logLine)
                );
            }
            
            var processMemoryUsageMatch = ProcessMemoryUsageExceededRegex.Match(message);
            if (processMemoryUsageMatch.Success)
            {
                record = ResourceManagerAction.GetProcessMemoryTerminationEvent(
                    baseEvent,
                    logLine,
                    processName,
                    TryParseLongWithLogging(processMemoryUsageMatch, "process_usage", logLine)
                );
            }
            
            var totalMemoryUsageMatch = TotalMemoryUsageExceededRegex.Match(message);
            if (totalMemoryUsageMatch.Success)
            {
                record = ResourceManagerAction.GetTotalMemoryTerminationEvent(
                    baseEvent,
                    logLine,
                    processName,
                    TryParseLongWithLogging(totalMemoryUsageMatch, "process_usage", logLine, true),
                    TryParseLongWithLogging(totalMemoryUsageMatch, "tableau_usage", logLine, true),
                    TryParseLongWithLogging(totalMemoryUsageMatch, "total_usage", logLine)
                );
            }

            if (record != null)
            {
                _actionsWriter.AddLine(record);
                return;
            }

            _processingNotificationsCollector.ReportError("Failed to process line as ActionEvent.", logLine, nameof(ResourceManagerPlugin));
        }
        
        private static readonly Regex CpuLimitRegex = new Regex(@".*\s(?<cpu_limit>\d+?)%.*", RegexOptions.Compiled | RegexOptions.ExplicitCapture);
        private void AddCpuLimitEvent(NativeJsonLogsBaseEvent baseEvent, string message, LogLine logLine, string processName)
        {
            var match = CpuLimitRegex.Match(message);
            if (match.Success)
            {
                var record = ResourceManagerThreshold.GetCpuLimitRecord(
                    baseEvent,
                    logLine,
                    processName,
                    TryParseIntWithLogging(match, "cpu_limit", logLine)
                );

                _thresholdsWriters.AddLine(record);
                return;
            }

            _processingNotificationsCollector.ReportError("Failed to process line as CpuLimitEvent.", logLine, nameof(ResourceManagerPlugin));
        }
        
        // Gets byte count by greedily capturing a numeric sequence (composed of digits and comma separators) from between the "Limit:" and "bytes\n" tokens.
        private static readonly Regex MemoryLimitRegex = new Regex(@".*Limit: (?<memory_limit>[\d,]+?) bytes$", RegexOptions.Compiled | RegexOptions.ExplicitCapture);
        private void AddPerProcessMemoryLimitEvent(NativeJsonLogsBaseEvent baseEvent, string message, LogLine logLine, string processName)
        {
            var match = MemoryLimitRegex.Match(message);
            if (match.Success)
            {
                var record = ResourceManagerThreshold.GetPerProcessMemoryLimitRecord(
                    baseEvent,
                    logLine,
                    processName,
                    TryParseLongWithLogging(match, "memory_limit", logLine)
                );

                _thresholdsWriters.AddLine(record);
                return;
            }

            _processingNotificationsCollector.ReportError("Failed to process line as MemoryLimitEvent.", logLine, nameof(ResourceManagerPlugin));
        }

        private void AddTotalMemoryLimitEvent(NativeJsonLogsBaseEvent baseEvent, string message, LogLine logLine, string processName)
        {
            var match = MemoryLimitRegex.Match(message);
            if (match.Success)
            {
                var record = ResourceManagerThreshold.GetTotalMemoryLimitRecord(
                    baseEvent,
                    logLine,
                    processName,
                    TryParseLongWithLogging(match, "memory_limit", logLine)
                );

                _thresholdsWriters.AddLine(record);
                return;
            }

            _processingNotificationsCollector.ReportError("Failed to process line as TotalMemoryMemoryLimitEvent.", logLine, nameof(ResourceManagerPlugin));
        }

        private static readonly Regex CpuUsageRegex = new Regex(@"^.*usage[.:] (?<cpu_usage>[\d,]+)%$", RegexOptions.Compiled | RegexOptions.ExplicitCapture);
        private void AddHighCpuUsageEvent(NativeJsonLogsBaseEvent baseEvent, string message, LogLine logLine, string processName)
        {
            var match = CpuUsageRegex.Match(message);
            if (match.Success)
            {
                var record =  ResourceManagerHighCpuUsage.GetEventWithNullCheck(
                    baseEvent,
                    logLine,
                    processName,
                    TryParseIntWithLogging(match, "cpu_usage", logLine)
                );

                _cpuUsageWriter.AddLine(record);
                return;
            }

            _processingNotificationsCollector.ReportError("Failed to process line as HighCpuUsageEvent.", logLine, nameof(ResourceManagerPlugin));
        }

        private int? TryParseIntWithLogging(Match match, string groupName, LogLine logLine)
        {
            var rawValue = match.Groups[groupName].Value;
            var parseSuccess = int.TryParse(rawValue, out var parsedValue);

            if (!parseSuccess)
            {
                var errorMessage = $"Failed to parse value `{rawValue}` of match group `{groupName}` as integer";
                _processingNotificationsCollector.ReportError(errorMessage, logLine, nameof(ResourceManagerPlugin));
            }

            return parseSuccess ? parsedValue : (int?) null;
        }
        
        private long? TryParseLongWithLogging(Match match, string groupName, LogLine logLine, bool optional = false)
        {
            var rawValue = match.Groups[groupName].Value;
            var rawValueWithoutCommas = rawValue.Replace(",", "");
            var parseSuccess = long.TryParse(rawValueWithoutCommas, out var parsedValue);

            if (!parseSuccess && !optional)
            {
                var errorMessage = $"Failed to parse value `{rawValue}` of match group `{groupName}` as long integer";
                _processingNotificationsCollector.ReportError(errorMessage, logLine, nameof(ResourceManagerPlugin));
            }

            return parseSuccess ? parsedValue : (long?) null;
        }
        
        private double? TryParseDoubleWithLogging(Match match, string groupName, LogLine logLine, bool optional = false)
        {
            var rawValue = match.Groups[groupName].Value;
            var rawValueWithoutCommas = rawValue.Replace(",", "");
            var parseSuccess = double.TryParse(rawValueWithoutCommas, out var parsedValue);

            if (!parseSuccess && !optional)
            {
                var errorMessage = $"Failed to parse value `{rawValue}` of match group `{groupName}` as double";
                _processingNotificationsCollector.ReportError(errorMessage, logLine, nameof(ResourceManagerPlugin));
            }

            return parseSuccess ? parsedValue : (double?) null;
        }

        private double? ConvertMbToGb(double? mbValue)
        {
            return mbValue.HasValue ? mbValue / 1024.0 : null;
        }
    }
}