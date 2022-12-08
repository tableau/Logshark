using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LogShark.Containers;
using LogShark.Plugins;
using LogShark.Shared;
using LogShark.Shared.Extensions;
using LogShark.Shared.LogReading;
using LogShark.Shared.LogReading.Containers;
using LogShark.Writers;
using Microsoft.Extensions.Logging;

namespace LogShark
{
    public class TableauLogsProcessor : IDisposable
    {
        private readonly LogSharkConfiguration _config;
        private readonly ILogger _logger;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogTypeDetails _logTypeDetails;
        private readonly IPluginManager _pluginManager;
        private readonly IProcessingNotificationsCollector _processingNotificationsCollector;
        private readonly IWriterFactory _writerFactory;

        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly SemaphoreSlim _semaphore;
        private readonly ZipArchivePool _zipArchivePool;
        private TableauLogsExtractor _logsExtractor;

        public TableauLogsProcessor(
            LogSharkConfiguration config,
            IPluginManager pluginManager,
            IWriterFactory writerFactory,
            ILogTypeDetails logTypeDetails,
            IProcessingNotificationsCollector processingNotificationsCollector,
            ILoggerFactory loggerFactory)
        {
            _config = config;
            _loggerFactory = loggerFactory;
            _logger = _loggerFactory.CreateLogger<TableauLogsProcessor>();
            _logTypeDetails = logTypeDetails;
            _pluginManager = pluginManager;
            _processingNotificationsCollector = processingNotificationsCollector;
            _writerFactory = writerFactory;
            
            _zipArchivePool = new ZipArchivePool(loggerFactory.CreateLogger<ZipArchivePool>());
            _cancellationTokenSource = new CancellationTokenSource();
            _semaphore = new SemaphoreSlim(_config.NumberOfParallelThreads);
            _logsExtractor = new TableauLogsExtractor(_config.LogSetLocation, _config.TempDir, _processingNotificationsCollector, _loggerFactory.CreateLogger<TableauLogsExtractor>());

        }

        public async Task<ProcessLogSetResult> ProcessLogSet()
        {
            _logger.LogInformation("Using temp folder `{tempDir}`", _config.TempDir);


            if (!_pluginManager.IsValidPluginConfiguration(out var badPluginNames))
            {
                return ProcessLogSetResult.Failed(FailureReasonMessageGenerator.BadPluginNamesSpecified(badPluginNames), ExitReason.IncorrectConfiguration);
            }

            var loadedPlugins = _pluginManager.CreatePlugins(_writerFactory, _processingNotificationsCollector).ToList();
            var requiredLogTypes = _pluginManager.GetRequiredLogTypes().ToList();
            
            var processedLogTypes = string.Join(", ", requiredLogTypes.OrderBy(name => name));
            _logger.LogInformation("Based on requested plugins, the following log types will be processed: {processedLogTypes}", processedLogTypes);
            
            _logger.LogInformation("Generating list of tasks to process all logs files");
            ProcessLogTypeResult failedLogTypeResult = null;
            Dictionary<LogType, ProcessLogTypeResult> logProcessingStatistics = null;
            HashSet<string> pluginsReceivedAnyData = null;
            try
            { 
                var tasksToPerform = new List<Task<ProcessFileResult>>();
                foreach (var logType in requiredLogTypes)
                {
                    var logTypeInfo = _logTypeDetails.GetInfoForLogType(logType);
                    var applicablePlugins = GetApplicablePluginsForLogType(loadedPlugins, logType);
                    
                    _logger.LogInformation("Generating tasks for {logType} logs", logType);
                    var tasksForLogType = GenerateTasksForLogType(_logsExtractor.LogSetParts, logTypeInfo, applicablePlugins);
                    tasksToPerform.AddRange(tasksForLogType);
                    _logger.LogInformation("Generated {taskCount} tasks for {logType} logs", tasksForLogType.Count, logType);
                }
                
                _logger.LogInformation("A total of {tasksTotal} tasks generated. Waiting for all tasks to complete", tasksToPerform.Count);
                Task.WaitAll(tasksToPerform.ToArray());
                logProcessingStatistics = await CombineProcessingResults(tasksToPerform);
                pluginsReceivedAnyData = CalculatePluginsReceivedAnyData(logProcessingStatistics, loadedPlugins);
            }
            catch (Exception ex)
            {
                var unhandledExceptionMessage = $"Unhandled exception occurred while processing logs. Exception: {ex.Message}";
                _logger.LogError(ex, unhandledExceptionMessage);
                var fakeProcessingFileResult = new ProcessFileResult(LogType.UnknownLogTypeForErrors, unhandledExceptionMessage, ExitReason.UnclassifiedError);
                var fakeProcessingTypeResults = new ProcessLogTypeResult();
                fakeProcessingTypeResults.AddProcessFileResults(fakeProcessingFileResult);
                failedLogTypeResult = fakeProcessingTypeResults;
            }
            
            _logger.LogInformation("Telling all plugins to complete processing of any cached data");
            var pluginsExecutionResults = _pluginManager.SendCompleteProcessingSignalToPlugins(logProcessingStatistics?.Values.Any(stat => !stat.IsSuccessful) ?? true);
            _logger.LogInformation("Completed reading log set and generating data");

            var (errorMessage, exitReason) = GetExitReasonAndErrorMessageIfApplicable(failedLogTypeResult, logProcessingStatistics);
            var loadedPluginNames = loadedPlugins.Select(plugin => plugin.Name).ToHashSet();
            return new ProcessLogSetResult(
                errorMessage,
                exitReason,
                _logsExtractor.LogSetSizeBytes,
                _logsExtractor.IsDirectory,
                loadedPluginNames,
                logProcessingStatistics,
                pluginsExecutionResults,
                pluginsReceivedAnyData);
        }

        private List<Task<ProcessFileResult>> GenerateTasksForLogType(IEnumerable<LogSetInfo> logSetParts, LogTypeInfo logTypeInfo, IList<IPlugin> plugins)
        {
            var tasksList = new List<Task<ProcessFileResult>>();
            
            foreach (var logSetInfo in logSetParts)
            {
                tasksList.AddRange(logSetInfo.IsZip
                    ? AddZipTasks(logSetInfo, logTypeInfo, plugins)
                    : AddDirTasks(logSetInfo, logTypeInfo, plugins));
            }

            return tasksList;
        }

        private IEnumerable<Task<ProcessFileResult>> AddZipTasks(LogSetInfo logSetInfo, LogTypeInfo logTypeInfo, IList<IPlugin> plugins)
        {
            return logSetInfo.FilePaths
                .Where(logTypeInfo.FileBelongsToThisType)
                .Select(path => Task.Run(() => ProcessZippedFileTask(logSetInfo, path, logTypeInfo, plugins, _cancellationTokenSource.Token)));
        }
        
        private IEnumerable<Task<ProcessFileResult>> AddDirTasks(LogSetInfo logSetInfo, LogTypeInfo logTypeInfo, IList<IPlugin> plugins)
        {
            return logSetInfo.FilePaths
                .Select(filePath => new
                {
                    FilePath = filePath,
                    FileSizeBytes = new FileInfo(filePath).Length,
                    NormalizedPath = filePath.NormalizePath(logSetInfo.Path)
                })
                .Where(pair => logTypeInfo.FileBelongsToThisType(pair.NormalizedPath))
                .Select(pair => Task.Run(() => ProcessUnzippedFileTask(pair.FilePath, pair.NormalizedPath, pair.FileSizeBytes, logTypeInfo, plugins, _cancellationTokenSource.Token)));
        }
        
        private async Task<ProcessFileResult> ProcessZippedFileTask(LogSetInfo logSetInfo, string filePath, LogTypeInfo logTypeInfo, IList<IPlugin> plugins, CancellationToken cancellationToken)
        {
            await _semaphore.WaitAsync(); // Not using method with cancellation token support, as it throws when cancellation occurs

            try
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return new ProcessFileResult(logTypeInfo.LogType, "Task cancelled", ExitReason.TaskCancelled);
                }
                
                using var zipArchive = _zipArchivePool.CheckOut(logSetInfo.Path);
                var fileEntry = zipArchive.ZipArchive.Entries.First(entry => entry.FullName == filePath);
            
                var filePathWithPrefix = string.IsNullOrWhiteSpace(logSetInfo.Prefix)
                    ? filePath
                    : $"{logSetInfo.Prefix}/{filePath}";
            
                var logFileInfo = new LogFileInfo(
                    fileEntry.Name,
                    filePathWithPrefix,
                    filePathWithPrefix.GetWorkerIdFromFilePath(),
                    new DateTimeOffset(fileEntry.LastWriteTime.Ticks, TimeSpan.Zero).UtcDateTime // the ZipArchiveEntry doesn't currently support reading the timezone of the zip entry... so we strip it for consistency
                );
            
                // currently because of how zips store (or don't) timezone info for entries, the zipped and unzipped versions of this method produce different output.  Perhaps we can do better in the future.
                
                _logger.BeginScope(logTypeInfo.LogType);
                _logger.BeginScope(filePathWithPrefix);
                _logger.LogInformation("Starting to process log file {logFileName}", filePathWithPrefix);
                var stopwatch = Stopwatch.StartNew();
                await using var stream = fileEntry.Open();
                var processStreamResult = ProcessStream(stream, logTypeInfo, logFileInfo, plugins, cancellationToken);
                var elapsed = stopwatch.Elapsed;
                var processFileResult = new ProcessFileResult(logTypeInfo.LogType, processStreamResult, fileEntry.Length, stopwatch.Elapsed); 
                if (!processFileResult.IsSuccessful)
                {
                    _cancellationTokenSource.Cancel();
                }
                else
                {
                    LogFileProcessingResults(filePath, fileEntry.Length, processFileResult, elapsed);
                }
                
                return processFileResult;
            }
            finally
            {
                _semaphore.Release();
            }
        }
        
        private async Task<ProcessFileResult> ProcessUnzippedFileTask(string rawFilePath, string normalizedFilePath, long fileSize, LogTypeInfo logTypeInfo, IList<IPlugin> plugins, CancellationToken cancellationToken)
        {
            await _semaphore.WaitAsync(); // Not using method with cancellation token support, as it throws when cancellation occurs

            try
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return new ProcessFileResult(logTypeInfo.LogType, "Task cancelled", ExitReason.TaskCancelled);
                }
                
                _logger.BeginScope(logTypeInfo.LogType);
                _logger.LogInformation("Starting to process log file {logFileName}", normalizedFilePath);
                var stopwatch = Stopwatch.StartNew();
                var fileInfo = new FileInfo(rawFilePath);
                var logFileInfo = new LogFileInfo(
                    fileInfo.Name,
                    normalizedFilePath,
                    normalizedFilePath.GetWorkerIdFromFilePath(),
                    new DateTimeOffset(fileInfo.LastWriteTime.Ticks, TimeSpan.Zero).UtcDateTime
                );

                await using var stream = File.Open(rawFilePath, FileMode.Open);
                var processStreamResult = ProcessStream(stream, logTypeInfo, logFileInfo, plugins, cancellationToken);
                var elapsed = stopwatch.Elapsed;
                var processFileResult = new ProcessFileResult(logTypeInfo.LogType, processStreamResult, fileSize, elapsed);

                if (!processFileResult.IsSuccessful)
                {
                    _cancellationTokenSource.Cancel();
                }
                else
                {
                    LogFileProcessingResults(normalizedFilePath, fileSize, processFileResult, elapsed);
                }

                return processFileResult;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private ProcessStreamResult ProcessStream(Stream stream, LogTypeInfo logTypeInfo, LogFileInfo logFileInfo, IList<IPlugin> plugins, CancellationToken cancellationToken)
        {
            var linesProcessed = 0;
            var reader = logTypeInfo.LogReaderProvider(stream, logFileInfo.FilePath);
            try
            {
                foreach (var line in reader.ReadLines())
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return new ProcessStreamResult(linesProcessed, "Task cancelled", ExitReason.TaskCancelled);
                    }
                    
                    if (!line.HasContent)
                    {
                        continue;
                    }

                    var logLine = new LogLine(line, logFileInfo);

                    foreach (var plugin in plugins)
                    {
                        plugin.ProcessLogLine(logLine, logTypeInfo.LogType);
                    }

                    ++linesProcessed;
                }
            }
            catch (OutOfMemoryException outOfMemoryException)
            {
                var errorMessage = FailureReasonMessageGenerator.OutOfMemoryError(logFileInfo, plugins);
                _logger.LogInformation(outOfMemoryException, errorMessage);
                return new ProcessStreamResult(linesProcessed, errorMessage, ExitReason.OutOfMemory);
            }
            catch (ArgumentOutOfRangeException argumentOutOfRangeException)
                when (argumentOutOfRangeException.StackTrace?
                    .Split("\n")
                    .FirstOrDefault()
                    ?.Contains("at System.Text.StringBuilder.Append(") ?? false)
            {
                var errorMessage = FailureReasonMessageGenerator.LogLineTooLong(logFileInfo, plugins);
                _logger.LogInformation(argumentOutOfRangeException, errorMessage);
                return new ProcessStreamResult(linesProcessed, errorMessage, ExitReason.LogLineTooLong);
            }
            catch (ObjectDisposedException objectDisposedException)
                when (objectDisposedException.ObjectName == "inserter")
            {
                var errorMessage = $"Failed to use Hyper writer as it has been disposed.  This is likely due to an error on another thread.  Logshark will attempt to resume processing for other plugins.";
                _logger.LogInformation(objectDisposedException, errorMessage);
                return new ProcessStreamResult(linesProcessed, errorMessage, ExitReason.InserterDisposed);
            }
            catch (Exception ex)
            {
                var errorMessage = $"Unhandled exception occurred while processing file stream from file `{logFileInfo.FilePath}`. Aborting processing.";
                _logger.LogError(ex, errorMessage);
                return new ProcessStreamResult(linesProcessed, errorMessage, ExitReason.UnclassifiedError);
            }

            return new ProcessStreamResult(linesProcessed);
        }

        private void LogFileProcessingResults(string filePath, long fileSizeBytes, ProcessFileResult processFileResult, TimeSpan elapsed)
        {
            var fileSizeMb = fileSizeBytes / 1024 / 1024;
            var mbPerSecond = elapsed.TotalSeconds > 0
                ? fileSizeMb / elapsed.TotalSeconds
                : fileSizeMb;
            
            _logger.LogInformation("Completed processing file {logFile} in {logFileElapsed} ({logFileSizeMb} MB, {logFileLines} lines, {logFileMbPerSecond:F2} MB/s)", filePath, elapsed, fileSizeMb, processFileResult.LinesProcessed, mbPerSecond);           
        }
        
        private (string, ExitReason) GetExitReasonAndErrorMessageIfApplicable(ProcessLogTypeResult failedProcessLogTypeResult, Dictionary<LogType, ProcessLogTypeResult> logProcessingStatistics)
        {
            if (failedProcessLogTypeResult != null)
            {
                return (failedProcessLogTypeResult.ErrorMessage, failedProcessLogTypeResult.ExitReason);
            }

            if (logProcessingStatistics.Values.Any(logTypeStats => !logTypeStats.IsSuccessful))
            {
                var failedLogType = logProcessingStatistics.Values.First(logTypeStats => !logTypeStats.IsSuccessful);
                return (failedLogType.ErrorMessage, failedLogType.ExitReason);
            }

            var processedAnyFiles = logProcessingStatistics.Any(kvp => kvp.Value.FilesProcessed > 0);
            return processedAnyFiles 
                ? (null, ExitReason.CompletedSuccessfully) 
                : (FailureReasonMessageGenerator.NoTableauLogFilesFound(_config.RequestedPlugins, _config.OriginalLocation), ExitReason.LogSetDoesNotContainRelevantLogs);
        }
        
        private async Task<Dictionary<LogType, ProcessLogTypeResult>> CombineProcessingResults(List<Task<ProcessFileResult>> tasksToPerform)
        {
            var logProcessingStatistics = new Dictionary<LogType, ProcessLogTypeResult>();
            
            foreach (var task in tasksToPerform)
            {
                var processFileResult = await task; // There should be no real wait here, as Tasks were awaited before calling this

                var alreadyHaveLogTypeRecord = logProcessingStatistics.TryGetValue(processFileResult.LogType, out var record);
                if (alreadyHaveLogTypeRecord)
                {
                    record.AddProcessFileResults(processFileResult);
                }
                else
                {
                    var processLogTypeResult = new ProcessLogTypeResult();
                    processLogTypeResult.AddProcessFileResults(processFileResult);
                    logProcessingStatistics.Add(processFileResult.LogType, processLogTypeResult);
                }
            }

            return logProcessingStatistics;
        }
        
        private HashSet<string> CalculatePluginsReceivedAnyData(Dictionary<LogType, ProcessLogTypeResult> logProcessingStatistics, ICollection<IPlugin> loadedPlugins)
        {
            var pluginsReceivedAnyData = new HashSet<string>();

            foreach (var (logType, processingResult) in logProcessingStatistics)
            {
                if (processingResult.FilesProcessed > 0)
                {
                    var applicablePlugins = GetApplicablePluginsForLogType(loadedPlugins, logType);
                    foreach (var plugin in applicablePlugins)
                    {
                        pluginsReceivedAnyData.Add(plugin.Name);
                    }
                }                
            }

            return pluginsReceivedAnyData;
        }

        private IList<IPlugin> GetApplicablePluginsForLogType(IEnumerable<IPlugin> loadedPlugins, LogType logType)
        {
            return loadedPlugins
                .Where(plugin => plugin.ConsumedLogTypes.Contains(logType))
                .ToList();
        }

        private static class FailureReasonMessageGenerator
        {
            public static string NoTableauLogFilesFound(string requestedPlugins, string logSetLocation)
            {
                var selectedPluginsPart = string.IsNullOrWhiteSpace(requestedPlugins)
                    ? "No plugins were explicitly requested, so LogShark ran with all plugins enabled."
                    : $"Plugins requested: `{requestedPlugins}`.";
                return $"Did not find any Tableau log files associated with requested plugins in `{logSetLocation}`. " +
                                   $"{selectedPluginsPart} Please make sure that provided path is a zip file generated " +
                                   "by Tableau Server, or unzipped copy of the same file (with file structure preserved), or zip file with Desktop logs";
            }
            
            public static string BadPluginNamesSpecified(IEnumerable<string> badPluginNames)
            {
                var pluginNamesMerged = string.Join(", ", badPluginNames.OrderBy(name => name));
                return $"The following plugins were requested but cannot be found: `{pluginNamesMerged}`. Use \"LogShark --listplugins\" option to see available plugins.";
            }

            public static string OutOfMemoryError(LogFileInfo logFileInfo, IEnumerable<IPlugin> pluginsUsed)
            {
                var pluginNames = string.Join(", ", pluginsUsed.Select(plugin => plugin.Name));
                var errorMessage = new StringBuilder();
                errorMessage.AppendLine($"Out of memory exception occurred while processing log file `{logFileInfo.FilePath}`. Processing of the log set aborted. The most common reason for this to happen is a very wide line in a log file. It could still be possible to process this log set, but you will have to do one of the following:");
                errorMessage.AppendLine("\t* Use a machine with more memory available.");
                errorMessage.AppendLine($"\t* Skip the plugin(s) that ran out of memory. Plugins LogShark attempted to run were: `{pluginNames}`");
                errorMessage.AppendLine("\t* Remove file in question from the log set. (sometimes other files of the same type might contain the same wide line though)");
                return errorMessage.ToString();
            }
            
            public static string LogLineTooLong(LogFileInfo logFileInfo, IEnumerable<IPlugin> pluginsUsed)
            {
                var pluginNames = string.Join(", ", pluginsUsed.Select(plugin => plugin.Name));
                var errorMessage = new StringBuilder();
                errorMessage.AppendLine($"Very long log line encountered while processing log file `{logFileInfo.FilePath}`. Processing of the log set aborted as log reader is in the bad state at this point. This error happens if a single line from the log cannot fit into C# array that has a max length of {int.MaxValue}. It could still be possible to process other files in the log set, but you will have to do one of the following:");
                errorMessage.AppendLine($"\t* Skip the plugin(s) that process this log file. Plugins LogShark attempted to run were: `{pluginNames}`");
                errorMessage.AppendLine("\t* Remove file in question from the log set. (sometimes other files of the same type might contain the same wide line though)");
                return errorMessage.ToString();
            }
        }

        public void Dispose()
        {
            _cancellationTokenSource?.Dispose();
            _semaphore?.Dispose();
            _zipArchivePool?.Dispose();
            _logsExtractor?.Dispose();
        }
    }
}