using System;
using LogShark.Containers;

namespace LogShark.Plugins
{
    public abstract class BaseEvent
    {
        public string FileName { get; }
        public string FilePath { get; }
        public int LineNumber { get; }
        public DateTime Timestamp { get; }
        public string Worker { get; }

        protected BaseEvent(LogLine logLine, DateTime timestamp)
        {
            Timestamp = timestamp;
            Worker = logLine.LogFileInfo.Worker;
            FilePath = logLine.LogFileInfo.FilePath;
            FileName = logLine.LogFileInfo.FileName;
            LineNumber = logLine.LineNumber;
        }
    }
}