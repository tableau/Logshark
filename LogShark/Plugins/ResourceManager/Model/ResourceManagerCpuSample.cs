using LogShark.Containers;
using LogShark.Plugins.Shared;

namespace LogShark.Plugins.ResourceManager.Model
{
    public class ResourceManagerCpuSample : ResourceManagerEvent
    {
        public int ProcessCpuUtil { get; }

        //We only get this in the logs when the process CPU Util value is greater than zero, for some reason.
        public int? TotalCpuUtil { get; }

        private ResourceManagerCpuSample(
            NativeJsonLogsBaseEvent baseEvent,
            LogLine logLine,
            string processName,
            int processCpuUtil,
            int? totalCpuUtil
            ) : base(baseEvent, logLine, processName)
        {
            ProcessCpuUtil = processCpuUtil;
            TotalCpuUtil = totalCpuUtil;
        }

        public static ResourceManagerCpuSample GetEventWithNullCheck(
            NativeJsonLogsBaseEvent baseEvent,
            LogLine logLine,
            string processName,
            int? processCpuUtil,
            int? totalCpuUtil)
        {
            return processCpuUtil == null 
                ? null 
                : new ResourceManagerCpuSample(baseEvent, logLine, processName, processCpuUtil.Value, totalCpuUtil);
        }
    }
}