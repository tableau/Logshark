using LogShark.Containers;
using LogShark.Plugins.Shared;

namespace LogShark.Plugins.ResourceManager.Model
{
    public class ResourceManagerHighCpuUsage : ResourceManagerEvent
    {
        public int CpuUsagePercent { get; }

        private ResourceManagerHighCpuUsage(NativeJsonLogsBaseEvent baseEvent,
            LogLine logLine,
            string processName,
            int cpuUsagePercent
            ) : base(baseEvent, logLine, processName)
        {
            CpuUsagePercent = cpuUsagePercent;
        }

        public static ResourceManagerHighCpuUsage GetEventWithNullCheck(NativeJsonLogsBaseEvent baseEvent,
            LogLine logLine,
            string processName,
            int? cpuUsagePercent
            )
        {
            return cpuUsagePercent == null ? null : new ResourceManagerHighCpuUsage(baseEvent, logLine, processName, cpuUsagePercent.Value);
        }
    }
}
