using System;
using LogShark.Shared.LogReading.Containers;

namespace LogShark.Plugins.Config.Models
{
    public class ConfigEntry
    {
        public DateTime FileLastModifiedUtc { get; }
        public string FileName { get; }
        public string FilePath { get; }
        public string Key { get; }
        public string RootKey { get; }
        public string Value { get; }
        public string Worker { get; }

        public ConfigEntry(LogFileInfo logFileInfo, string key, string rootKey, string value)
        {
            FileLastModifiedUtc = logFileInfo.LastModifiedUtc;
            FileName = logFileInfo.FileName;
            FilePath = logFileInfo.FilePath;
            Key = key;
            RootKey = rootKey;
            Value = value;
            Worker = logFileInfo.Worker;
        }
    }
}