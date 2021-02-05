using LogShark.Shared.LogReading.Containers;

namespace LogShark.Plugins.ResourceManager.Model
{
    public class ResourceManagerAction : ResourceManagerEvent
    {
        public bool CpuUtilTermination { get; }
        public int? CpuUtil { get; }
        public bool ProcessMemoryUtilTermination { get; }
        public long? ProcessMemoryUtil { get; }
        public long? TableauTotalMemoryUtil { get; }
        public bool TotalMemoryUtilTermination { get; }
        public long? TotalMemoryUtil { get; }

        public ResourceManagerAction(
            NativeJsonLogsBaseEvent baseEvent,
            LogLine logLine,
            string processName,
            bool cpuUtilTermination,
            int? cpuUtil,
            bool processMemoryUtilTermination,
            long? processMemoryUtil,
            long? tableauTotalMemoryUtil,
            bool totalMemoryUtilTermination,
            long? totalMemoryUtil
            ) : base(baseEvent, logLine, processName)
        {
            CpuUtilTermination = cpuUtilTermination;
            CpuUtil = cpuUtil;
            ProcessMemoryUtilTermination = processMemoryUtilTermination;
            ProcessMemoryUtil = processMemoryUtil;
            TableauTotalMemoryUtil = tableauTotalMemoryUtil;
            TotalMemoryUtilTermination = totalMemoryUtilTermination;
            TotalMemoryUtil = totalMemoryUtil;
        }

        public static ResourceManagerAction GetCpuTerminationEvent(
            NativeJsonLogsBaseEvent baseEvent,
            LogLine logLine,
            string processName,
            int? cpuUtil)
        {
            return cpuUtil == null
                ? null 
                : new ResourceManagerAction(baseEvent, logLine, processName, true, cpuUtil, false, null, null, false, null);
        }
        
        public static ResourceManagerAction GetProcessMemoryTerminationEvent(
            NativeJsonLogsBaseEvent baseEvent,
            LogLine logLine,
            string processName,
            long? processMemoryUtil)
        {
            return processMemoryUtil == null
                ? null
                : new ResourceManagerAction(baseEvent, logLine, processName, false, null, true, processMemoryUtil, null, false, null);
        }
        
        public static ResourceManagerAction GetTotalMemoryTerminationEvent(
            NativeJsonLogsBaseEvent baseEvent,
            LogLine logLine,
            string processName,
            long? processMemoryUtil,
            long? tableauTotalMemoryUtil,
            long? totalMemoryUtil)
        {
            return totalMemoryUtil == null
                ? null
                : new ResourceManagerAction(baseEvent, logLine, processName, false, null, false, processMemoryUtil, tableauTotalMemoryUtil, true, totalMemoryUtil);
        }
    }
}