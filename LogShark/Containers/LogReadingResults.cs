using System.Collections.Generic;
using LogShark.Writers.Containers;

namespace LogShark.Containers
{
    public class LogReadingResults
    {
        public IList<string> Errors { get; }
        public long FullLogSetSizeBytes { get; }
        public bool IsDirectory { get; }
        public HashSet<string> LoadedPlugins { get; }
        public Dictionary<LogType, LogProcessingStatistics> LogProcessingStatistics { get; }
        public PluginsExecutionResults PluginsExecutionResults { get; }
        public HashSet<string> PluginsReceivedAnyData { get; }

        public LogReadingResults(
            IList<string> errors,
            long fullLogSetSizeBytes,
            bool isDirectory,
            HashSet<string> loadedPlugins,
            Dictionary<LogType, LogProcessingStatistics> logProcessingStatistics,
            PluginsExecutionResults pluginsExecutionResults,
            HashSet<string> pluginsReceivedAnyData)
        {
            Errors = errors;
            FullLogSetSizeBytes = fullLogSetSizeBytes;
            IsDirectory = isDirectory;
            LoadedPlugins = loadedPlugins;
            LogProcessingStatistics = logProcessingStatistics;
            PluginsExecutionResults = pluginsExecutionResults;
            PluginsReceivedAnyData = pluginsReceivedAnyData;
        }
    }
}