using System.Collections.Generic;

namespace LogShark.Containers
{
    public class ProcessLogSetResult
    {
        public string ErrorMessage { get; }
        public ExitReason ExitReason { get; }
        public long FullLogSetSizeBytes { get; }
        public bool IsDirectory { get; }
        public bool IsSuccessful => ExitReason == ExitReason.CompletedSuccessfully;
        public HashSet<string> LoadedPlugins { get; }
        public Dictionary<LogType, ProcessLogTypeResult> LogProcessingStatistics { get; }
        public PluginsExecutionResults PluginsExecutionResults { get; }
        public HashSet<string> PluginsReceivedAnyData { get; }

        public ProcessLogSetResult(
            string errorMessage,
            ExitReason exitReason,
            long fullLogSetSizeBytes,
            bool isDirectory,
            HashSet<string> loadedPlugins,
            Dictionary<LogType, ProcessLogTypeResult> logProcessingStatistics,
            PluginsExecutionResults pluginsExecutionResults,
            HashSet<string> pluginsReceivedAnyData
            )
        {
            ErrorMessage = errorMessage;
            ExitReason = exitReason;
            FullLogSetSizeBytes = fullLogSetSizeBytes;
            IsDirectory = isDirectory;
            LoadedPlugins = loadedPlugins;
            LogProcessingStatistics = logProcessingStatistics;
            PluginsExecutionResults = pluginsExecutionResults;
            PluginsReceivedAnyData = pluginsReceivedAnyData;
        }

        public static ProcessLogSetResult Failed(string errorMessage, ExitReason exitReason)
        {
            return new ProcessLogSetResult(
                errorMessage,
                exitReason,
                default,
                default,
                new HashSet<string>(),
                new Dictionary<LogType, ProcessLogTypeResult>(),
                default,
                new HashSet<string>());
        }
    }
}