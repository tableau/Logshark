using System.Collections.Generic;
using LogShark.Shared;
using LogShark.Shared.LogReading.Containers;

namespace LogShark.Plugins.Config
{
    /// <summary>
    /// Container to keep all information about single config file together
    /// </summary>
    public class ConfigFile
    {
        public LogFileInfo LogFileInfo { get; }
        public IDictionary<string, string> Values { get; }
        public LogType LogType { get; }

        public ConfigFile(LogFileInfo logFileInfo, IDictionary<string, string> values, LogType logType)
        {
            LogFileInfo = logFileInfo;
            Values = values;
            LogType = logType;
        }
    }
}