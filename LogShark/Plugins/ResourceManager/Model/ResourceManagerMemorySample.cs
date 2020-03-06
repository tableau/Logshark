using LogShark.Containers;
using LogShark.Plugins.Shared;

namespace LogShark.Plugins.ResourceManager.Model
{
    public class ResourceManagerMemorySample : ResourceManagerEvent
    {
        public long ProcessMemoryUtil { get; }
        public long TotalMemoryUtil { get; }

        public ResourceManagerMemorySample(
            NativeJsonLogsBaseEvent baseEvent,
            LogLine logLine,
            string processName,
            long processMemoryUtil,
            long totalMemoryUtil
            ) : base(baseEvent, logLine, processName)
        {
            ProcessMemoryUtil = processMemoryUtil;
            TotalMemoryUtil = totalMemoryUtil;
        }
        
        public static ResourceManagerMemorySample GetEventWithNullCheck(
            NativeJsonLogsBaseEvent baseEvent,
            LogLine logLine,
            string processName,
            long? processMemoryUtil,
            long? totalMemoryUtil)
        {
            return processMemoryUtil == null || totalMemoryUtil == null 
                ? null 
                : new ResourceManagerMemorySample(baseEvent, logLine, processName, processMemoryUtil.Value, totalMemoryUtil.Value);
        }
    }
}