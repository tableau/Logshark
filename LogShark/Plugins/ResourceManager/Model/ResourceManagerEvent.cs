using LogShark.Plugins.Shared;
using LogShark.Shared.LogReading.Containers;

namespace LogShark.Plugins.ResourceManager.Model
{
    public class ResourceManagerEvent : BaseEvent
    {
        public string ProcessName { get; }
        public int? ProcessIndex { get; }
        public int ProcessId { get; }

        public ResourceManagerEvent(NativeJsonLogsBaseEvent baseEvent, LogLine logLine, string processName)
            : base(logLine, baseEvent.Timestamp)
        {
            ProcessName = processName;
            ProcessIndex = ProcessInfoParser.ParseProcessIndex(logLine.LogFileInfo.FileName);
            ProcessId = baseEvent.ProcessId;
        }       
    }
}