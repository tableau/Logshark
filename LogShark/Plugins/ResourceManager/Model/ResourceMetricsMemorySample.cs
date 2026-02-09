using System;
using LogShark.Shared.LogReading.Containers;

namespace LogShark.Plugins.ResourceManager.Model
{
    public class ResourceMetricsMemorySample
    {
        public string File { get; set; }
        public int Line { get; set; }
        public DateTime Timestamp { get; set; }
        public string ProcessName { get; set; }
        public string Worker { get; set; }
        public string RequestId { get; set; }
        public string SessionId { get; set; }
        public string Site { get; set; }
        public string Username { get; set; }
        
        // has-load
        public bool? HasLoad { get; set; }
        
        // Memory fields
        public double? TotalVirtualMemoryGb { get; set; }
        public double? SystemVirtualMemoryGb { get; set; }
        public double? ProcessVirtualMemoryGb { get; set; }
        public double? TotalPhysicalMemoryGb { get; set; }
        public double? SystemPhysicalMemoryGb { get; set; }
        public double? ProcessPhysicalMemoryGb { get; set; }
        public double? ProcessFileMappingsGb { get; set; }
        
        // Memory trackers - current usage (in MB - not converted)
        public double? MemoryTrackedCurrentGlobalMb { get; set; }
        public double? MemoryTrackedCurrentGlobalNetworkWritebufferMb { get; set; }
        public double? MemoryTrackedCurrentDbcacheResourcesTrackerMb { get; set; }
        public double? MemoryTrackedCurrentGlobalNetworkReadbufferMb { get; set; }
        public double? MemoryTrackedCurrentGlobalMetricsMb { get; set; }
        public double? MemoryTrackedCurrentGlobalStringpoolMb { get; set; }
        public double? MemoryTrackedCurrentGlobalTupleDataMb { get; set; }
        public double? MemoryTrackedCurrentGlobalLockedMb { get; set; }
        public double? MemoryTrackedCurrentGlobalTransactionsMb { get; set; }
        public double? MemoryTrackedCurrentGlobalPlanCacheMb { get; set; }
        public double? MemoryTrackedCurrentGlobalExternalTableCacheMb { get; set; }
        public double? MemoryTrackedCurrentGlobalExternalMetadataMb { get; set; }
        public double? MemoryTrackedCurrentGlobalDiskNetworkReadbufferMb { get; set; }
        public double? MemoryTrackedCurrentGlobalDiskNetworkWritebufferMb { get; set; }
        public double? MemoryTrackedCurrentGlobalDiskStringpoolMb { get; set; }
        public double? MemoryTrackedCurrentGlobalDiskTransactionMb { get; set; }
        public double? MemoryTrackedCurrentStorageLayerUnflushedMemoryMb { get; set; }
        public double? MemoryTrackedCurrentStorageLayerTempBuffersMb { get; set; }
        public double? MemoryTrackedCurrentIoCacheMb { get; set; }
        public double? MemoryTrackedCurrentGlobalDiskCacheMb { get; set; }
        
        // Memory trackers - peak usage (in MB - not converted)
        public double? MemoryTrackedPeakGlobalMb { get; set; }
        public double? MemoryTrackedPeakGlobalNetworkWritebufferMb { get; set; }
        public double? MemoryTrackedPeakDbcacheResourcesTrackerMb { get; set; }
        public double? MemoryTrackedPeakGlobalNetworkReadbufferMb { get; set; }
        public double? MemoryTrackedPeakGlobalMetricsMb { get; set; }
        public double? MemoryTrackedPeakGlobalStringpoolMb { get; set; }
        public double? MemoryTrackedPeakGlobalTupleDataMb { get; set; }
        public double? MemoryTrackedPeakGlobalLockedMb { get; set; }
        public double? MemoryTrackedPeakGlobalTransactionsMb { get; set; }
        public double? MemoryTrackedPeakGlobalPlanCacheMb { get; set; }
        public double? MemoryTrackedPeakGlobalExternalTableCacheMb { get; set; }
        public double? MemoryTrackedPeakGlobalExternalMetadataMb { get; set; }
        public double? MemoryTrackedPeakGlobalDiskNetworkReadbufferMb { get; set; }
        public double? MemoryTrackedPeakGlobalDiskNetworkWritebufferMb { get; set; }
        public double? MemoryTrackedPeakGlobalDiskStringpoolMb { get; set; }
        public double? MemoryTrackedPeakGlobalDiskTransactionMb { get; set; }
        public double? MemoryTrackedPeakStorageLayerUnflushedMemoryMb { get; set; }
        public double? MemoryTrackedPeakStorageLayerTempBuffersMb { get; set; }
        public double? MemoryTrackedPeakIoCacheMb { get; set; }
        public double? MemoryTrackedPeakGlobalDiskCacheMb { get; set; }
        
        // Load fields
        public double? OverallLoad { get; set; }
        public double? SchedulerLoad { get; set; }
        public double? WorkspaceLoad { get; set; }
        public double? MemoryLoad { get; set; }
        public double? CpuLoad { get; set; }
        
        // Scheduler thread count
        public int? SchedulerWaitingTasksCount { get; set; }
        public int? SchedulerThreadCountActive { get; set; }
        public int? SchedulerThreadCountInactive { get; set; }

        public static ResourceMetricsMemorySample GetEventWithNullCheck(
            NativeJsonLogsBaseEvent baseEvent,
            LogLine logLine,
            string processName,
            bool? hasLoad = null,
            double? totalVirtualMemoryGb = null,
            double? systemVirtualMemoryGb = null,
            double? processVirtualMemoryGb = null,
            double? totalPhysicalMemoryGb = null,
            double? systemPhysicalMemoryGb = null,
            double? processPhysicalMemoryGb = null,
            double? processFileMappingsGb = null,
            double? memoryTrackedCurrentGlobalMb = null,
            double? memoryTrackedCurrentGlobalNetworkWritebufferMb = null,
            double? memoryTrackedCurrentDbcacheResourcesTrackerMb = null,
            double? memoryTrackedCurrentGlobalNetworkReadbufferMb = null,
            double? memoryTrackedCurrentGlobalMetricsMb = null,
            double? memoryTrackedCurrentGlobalStringpoolMb = null,
            double? memoryTrackedCurrentGlobalTupleDataMb = null,
            double? memoryTrackedCurrentGlobalLockedMb = null,
            double? memoryTrackedCurrentGlobalTransactionsMb = null,
            double? memoryTrackedCurrentGlobalPlanCacheMb = null,
            double? memoryTrackedCurrentGlobalExternalTableCacheMb = null,
            double? memoryTrackedCurrentGlobalExternalMetadataMb = null,
            double? memoryTrackedCurrentGlobalDiskNetworkReadbufferMb = null,
            double? memoryTrackedCurrentGlobalDiskNetworkWritebufferMb = null,
            double? memoryTrackedCurrentGlobalDiskStringpoolMb = null,
            double? memoryTrackedCurrentGlobalDiskTransactionMb = null,
            double? memoryTrackedCurrentStorageLayerUnflushedMemoryMb = null,
            double? memoryTrackedCurrentStorageLayerTempBuffersMb = null,
            double? memoryTrackedCurrentIoCacheMb = null,
            double? memoryTrackedCurrentGlobalDiskCacheMb = null,
            double? memoryTrackedPeakGlobalMb = null,
            double? memoryTrackedPeakGlobalNetworkWritebufferMb = null,
            double? memoryTrackedPeakDbcacheResourcesTrackerMb = null,
            double? memoryTrackedPeakGlobalNetworkReadbufferMb = null,
            double? memoryTrackedPeakGlobalMetricsMb = null,
            double? memoryTrackedPeakGlobalStringpoolMb = null,
            double? memoryTrackedPeakGlobalTupleDataMb = null,
            double? memoryTrackedPeakGlobalLockedMb = null,
            double? memoryTrackedPeakGlobalTransactionsMb = null,
            double? memoryTrackedPeakGlobalPlanCacheMb = null,
            double? memoryTrackedPeakGlobalExternalTableCacheMb = null,
            double? memoryTrackedPeakGlobalExternalMetadataMb = null,
            double? memoryTrackedPeakGlobalDiskNetworkReadbufferMb = null,
            double? memoryTrackedPeakGlobalDiskNetworkWritebufferMb = null,
            double? memoryTrackedPeakGlobalDiskStringpoolMb = null,
            double? memoryTrackedPeakGlobalDiskTransactionMb = null,
            double? memoryTrackedPeakStorageLayerUnflushedMemoryMb = null,
            double? memoryTrackedPeakStorageLayerTempBuffersMb = null,
            double? memoryTrackedPeakIoCacheMb = null,
            double? memoryTrackedPeakGlobalDiskCacheMb = null,
            double? overallLoad = null,
            double? schedulerLoad = null,
            double? workspaceLoad = null,
            double? memoryLoad = null,
            double? cpuLoad = null,
            int? schedulerWaitingTasksCount = null,
            int? schedulerThreadCountActive = null,
            int? schedulerThreadCountInactive = null)
        {
            return new ResourceMetricsMemorySample
            {
                File = logLine.LogFileInfo.FileName,
                Line = logLine.LineNumber,
                Timestamp = baseEvent?.Timestamp ?? DateTime.MinValue,
                ProcessName = processName,
                Worker = logLine.LogFileInfo.Worker,
                RequestId = baseEvent?.RequestId,
                SessionId = baseEvent?.SessionId,
                Site = baseEvent?.Site,
                Username = baseEvent?.Username,
                HasLoad = hasLoad,
                TotalVirtualMemoryGb = totalVirtualMemoryGb,
                SystemVirtualMemoryGb = systemVirtualMemoryGb,
                ProcessVirtualMemoryGb = processVirtualMemoryGb,
                TotalPhysicalMemoryGb = totalPhysicalMemoryGb,
                SystemPhysicalMemoryGb = systemPhysicalMemoryGb,
                ProcessPhysicalMemoryGb = processPhysicalMemoryGb,
                ProcessFileMappingsGb = processFileMappingsGb,
                MemoryTrackedCurrentGlobalMb = memoryTrackedCurrentGlobalMb,
                MemoryTrackedCurrentGlobalNetworkWritebufferMb = memoryTrackedCurrentGlobalNetworkWritebufferMb,
                MemoryTrackedCurrentDbcacheResourcesTrackerMb = memoryTrackedCurrentDbcacheResourcesTrackerMb,
                MemoryTrackedCurrentGlobalNetworkReadbufferMb = memoryTrackedCurrentGlobalNetworkReadbufferMb,
                MemoryTrackedCurrentGlobalMetricsMb = memoryTrackedCurrentGlobalMetricsMb,
                MemoryTrackedCurrentGlobalStringpoolMb = memoryTrackedCurrentGlobalStringpoolMb,
                MemoryTrackedCurrentGlobalTupleDataMb = memoryTrackedCurrentGlobalTupleDataMb,
                MemoryTrackedCurrentGlobalLockedMb = memoryTrackedCurrentGlobalLockedMb,
                MemoryTrackedCurrentGlobalTransactionsMb = memoryTrackedCurrentGlobalTransactionsMb,
                MemoryTrackedCurrentGlobalPlanCacheMb = memoryTrackedCurrentGlobalPlanCacheMb,
                MemoryTrackedCurrentGlobalExternalTableCacheMb = memoryTrackedCurrentGlobalExternalTableCacheMb,
                MemoryTrackedCurrentGlobalExternalMetadataMb = memoryTrackedCurrentGlobalExternalMetadataMb,
                MemoryTrackedCurrentGlobalDiskNetworkReadbufferMb = memoryTrackedCurrentGlobalDiskNetworkReadbufferMb,
                MemoryTrackedCurrentGlobalDiskNetworkWritebufferMb = memoryTrackedCurrentGlobalDiskNetworkWritebufferMb,
                MemoryTrackedCurrentGlobalDiskStringpoolMb = memoryTrackedCurrentGlobalDiskStringpoolMb,
                MemoryTrackedCurrentGlobalDiskTransactionMb = memoryTrackedCurrentGlobalDiskTransactionMb,
                MemoryTrackedCurrentStorageLayerUnflushedMemoryMb = memoryTrackedCurrentStorageLayerUnflushedMemoryMb,
                MemoryTrackedCurrentStorageLayerTempBuffersMb = memoryTrackedCurrentStorageLayerTempBuffersMb,
                MemoryTrackedCurrentIoCacheMb = memoryTrackedCurrentIoCacheMb,
                MemoryTrackedCurrentGlobalDiskCacheMb = memoryTrackedCurrentGlobalDiskCacheMb,
                MemoryTrackedPeakGlobalMb = memoryTrackedPeakGlobalMb,
                MemoryTrackedPeakGlobalNetworkWritebufferMb = memoryTrackedPeakGlobalNetworkWritebufferMb,
                MemoryTrackedPeakDbcacheResourcesTrackerMb = memoryTrackedPeakDbcacheResourcesTrackerMb,
                MemoryTrackedPeakGlobalNetworkReadbufferMb = memoryTrackedPeakGlobalNetworkReadbufferMb,
                MemoryTrackedPeakGlobalMetricsMb = memoryTrackedPeakGlobalMetricsMb,
                MemoryTrackedPeakGlobalStringpoolMb = memoryTrackedPeakGlobalStringpoolMb,
                MemoryTrackedPeakGlobalTupleDataMb = memoryTrackedPeakGlobalTupleDataMb,
                MemoryTrackedPeakGlobalLockedMb = memoryTrackedPeakGlobalLockedMb,
                MemoryTrackedPeakGlobalTransactionsMb = memoryTrackedPeakGlobalTransactionsMb,
                MemoryTrackedPeakGlobalPlanCacheMb = memoryTrackedPeakGlobalPlanCacheMb,
                MemoryTrackedPeakGlobalExternalTableCacheMb = memoryTrackedPeakGlobalExternalTableCacheMb,
                MemoryTrackedPeakGlobalExternalMetadataMb = memoryTrackedPeakGlobalExternalMetadataMb,
                MemoryTrackedPeakGlobalDiskNetworkReadbufferMb = memoryTrackedPeakGlobalDiskNetworkReadbufferMb,
                MemoryTrackedPeakGlobalDiskNetworkWritebufferMb = memoryTrackedPeakGlobalDiskNetworkWritebufferMb,
                MemoryTrackedPeakGlobalDiskStringpoolMb = memoryTrackedPeakGlobalDiskStringpoolMb,
                MemoryTrackedPeakGlobalDiskTransactionMb = memoryTrackedPeakGlobalDiskTransactionMb,
                MemoryTrackedPeakStorageLayerUnflushedMemoryMb = memoryTrackedPeakStorageLayerUnflushedMemoryMb,
                MemoryTrackedPeakStorageLayerTempBuffersMb = memoryTrackedPeakStorageLayerTempBuffersMb,
                MemoryTrackedPeakIoCacheMb = memoryTrackedPeakIoCacheMb,
                MemoryTrackedPeakGlobalDiskCacheMb = memoryTrackedPeakGlobalDiskCacheMb,
                OverallLoad = overallLoad,
                SchedulerLoad = schedulerLoad,
                WorkspaceLoad = workspaceLoad,
                MemoryLoad = memoryLoad,
                CpuLoad = cpuLoad,
                SchedulerWaitingTasksCount = schedulerWaitingTasksCount,
                SchedulerThreadCountActive = schedulerThreadCountActive,
                SchedulerThreadCountInactive = schedulerThreadCountInactive
            };
        }
    }
} 