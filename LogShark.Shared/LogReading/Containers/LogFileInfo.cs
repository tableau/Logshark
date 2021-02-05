using System;

namespace LogShark.Shared.LogReading.Containers
{
    public class LogFileInfo
    {
        public string FileName { get; }
        public string FilePath { get; }
        public string Worker { get; }
        public DateTime LastModifiedUtc { get; }

        public LogFileInfo(string fileName, string filePath, string worker, DateTime lastModifiedUtc)
        {
            FileName = fileName;
            FilePath = filePath;
            Worker = worker;
            LastModifiedUtc = lastModifiedUtc;
        }
    }
}