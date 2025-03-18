using LogShark.Shared.LogReading.Containers;

namespace LogShark.Plugins.ResourceManager.Model
{
    public class ResourceManagerMemorySample : ResourceManagerEvent
    {
        public long ProcessMemoryUtil { get; }
        public long TableauTotalMemoryUtil { get; }
        public long TotalMemoryUtil { get; }

        public ResourceManagerMemorySample(
            NativeJsonLogsBaseEvent baseEvent,
            LogLine logLine,
            string processName,
            long processMemoryUtil,
            long tableautotalMemoryUtil,
            long totalMemoryUtil
            ) : base(baseEvent, logLine, processName)
        {
            ProcessMemoryUtil = processMemoryUtil;
            TableauTotalMemoryUtil = tableautotalMemoryUtil;
            TotalMemoryUtil = totalMemoryUtil;
        }
        
        public static ResourceManagerMemorySample GetEventWithNullCheck(
            NativeJsonLogsBaseEvent baseEvent,
            LogLine logLine,
            string processName,
            long? processMemoryUtil,
            long? tableautotalMemoryUtil,
            long? totalMemoryUtil)
        {
            return processMemoryUtil == null || totalMemoryUtil == null || tableautotalMemoryUtil == null
                ? null 
                : new ResourceManagerMemorySample(baseEvent, logLine, processName, processMemoryUtil.Value,tableautotalMemoryUtil.Value ,totalMemoryUtil.Value);
        }
    }
}