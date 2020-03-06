using LogShark.Containers;
using LogShark.Plugins.Shared;

namespace LogShark.Plugins.ResourceManager.Model
{
    public class ResourceManagerAction : ResourceManagerEvent
    {
        public bool CpuUtilTermination { get; }
        public int? CpuUtil { get; }
        public bool ProcessMemoryUtilTermination { get; }
        public long? ProcessMemoryUtil { get; }
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
            bool totalMemoryUtilTermination,
            long? totalMemoryUtil
            ) : base(baseEvent, logLine, processName)
        {
            CpuUtilTermination = cpuUtilTermination;
            CpuUtil = cpuUtil;
            ProcessMemoryUtilTermination = processMemoryUtilTermination;
            ProcessMemoryUtil = processMemoryUtil;
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
                : new ResourceManagerAction(baseEvent, logLine, processName, true, cpuUtil, false, null, false, null);
        }
        
        public static ResourceManagerAction GetProcessMemoryTerminationEvent(
            NativeJsonLogsBaseEvent baseEvent,
            LogLine logLine,
            string processName,
            long? processMemoryUtil)
        {
            return processMemoryUtil == null
                ? null
                : new ResourceManagerAction(baseEvent, logLine, processName, false, null, true, processMemoryUtil, false, null);
        }
        
        public static ResourceManagerAction GetTotalMemoryTerminationEvent(
            NativeJsonLogsBaseEvent baseEvent,
            LogLine logLine,
            string processName,
            long? totalMemoryUtil)
        {
            return totalMemoryUtil == null
                ? null
                : new ResourceManagerAction(baseEvent, logLine, processName, false, null, false, null, true, totalMemoryUtil);
        }
    }
}