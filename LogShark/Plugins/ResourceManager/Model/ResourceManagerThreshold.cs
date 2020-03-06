using LogShark.Containers;
using LogShark.Plugins.Shared;

namespace LogShark.Plugins.ResourceManager.Model
{
    public class ResourceManagerThreshold : ResourceManagerEvent
    {
        public int? CpuLimit { get; }
        public long? PerProcessMemoryLimit { get; }
        public long? TotalMemoryLimit { get; }

        public ResourceManagerThreshold(
            NativeJsonLogsBaseEvent baseEvent,
            LogLine logLine,
            string processName,
            int? cpuLimit,
            long? perProcessMemoryLimit,
            long? totalMemoryLimit
        ) : base(baseEvent, logLine, processName)
        {
            CpuLimit = cpuLimit;
            PerProcessMemoryLimit = perProcessMemoryLimit;
            TotalMemoryLimit = totalMemoryLimit;
        }

        public static ResourceManagerThreshold GetCpuLimitRecord(
            NativeJsonLogsBaseEvent baseEvent,
            LogLine logLine,
            string processName,
            int? cpuLimit)
        {
            return cpuLimit == null 
                ? null 
                : new ResourceManagerThreshold(baseEvent, logLine, processName, cpuLimit, null, null);
        }
        
        public static ResourceManagerThreshold GetPerProcessMemoryLimitRecord(
            NativeJsonLogsBaseEvent baseEvent,
            LogLine logLine,
            string processName,
            long? perProcessMemoryLimit)
        {
            return perProcessMemoryLimit == null
                ? null
                : new ResourceManagerThreshold(baseEvent, logLine, processName, null, perProcessMemoryLimit, null);
        }
        
        public static ResourceManagerThreshold GetTotalMemoryLimitRecord(
            NativeJsonLogsBaseEvent baseEvent,
            LogLine logLine,
            string processName,
            long? totalMemoryLimit)
        {
            return totalMemoryLimit == null
                ? null
                : new ResourceManagerThreshold(baseEvent, logLine, processName, null, null, totalMemoryLimit);
        }
    }
}
