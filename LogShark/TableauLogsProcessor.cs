using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
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
    public class TableauLogsProcessor
    {
        private readonly LogSharkConfiguration _config;
        private readonly ILogger _logger;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogTypeDetails _logTypeDetails;
        private readonly IPluginManager _pluginManager;
        private readonly IProcessingNotificationsCollector _processingNotificationsCollector;
        private readonly IWriterFactory _writerFactory;

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
        }
        
        public ProcessLogSetResult ProcessLogSet()
        {
            _logger.LogInformation("Using temp folder `{tempDir}`", _config.TempDir);

            using var logsExtractor = new TableauLogsExtractor(_config.LogSetLocation, _config.TempDir, _processingNotificationsCollector, _loggerFactory.CreateLogger<TableauLogsExtractor>());

            if (!_pluginManager.IsValidPluginConfiguration(out var badPluginNames))
            {
                return ProcessLogSetResult.Failed(FailureReasonMessageGenerator.BadPluginNamesSpecified(badPluginNames), ExitReason.IncorrectConfiguration);
            }

            var loadedPlugins = _pluginManager.CreatePlugins(_writerFactory, _processingNotificationsCollector).ToList();
            var requiredLogTypes = _pluginManager.GetRequiredLogTypes().ToList();
            
            var processedLogTypes = string.Join(", ", requiredLogTypes.OrderBy(name => name));
            _logger.LogInformation("Based on requested plugins, the following log types will be processed: {processedLogTypes}", processedLogTypes);

            var logProcessingStatistics = new Dictionary<LogType, ProcessLogTypeResult>();
            var pluginsReceivedAnyData = new HashSet<string>();
            ProcessLogTypeResult failedLogTypeResult = null;
            
            foreach (var logType in requiredLogTypes)
            {
                _logger.LogInformation("Starting to process {logType} logs", logType);
                    
                var logTypeInfo = _logTypeDetails.GetInfoForLogType(logType);
                var applicablePlugins = loadedPlugins
                    .Where(plugin => plugin.ConsumedLogTypes.Contains(logType))
                    .ToList();

                try
                {
                    var logTypeProcessingResult = ProcessLogType(logsExtractor.LogSetParts, logTypeInfo, applicablePlugins);
                    logProcessingStatistics.Add(logType, logTypeProcessingResult);
                    
                    if (!logTypeProcessingResult.IsSuccessful)
                    {
                        failedLogTypeResult = logTypeProcessingResult;
                        break;
                    }

                    _logger.LogInformation("Done processing {logType} logs. {processingResult}", logType, logTypeProcessingResult);

                    if (logTypeProcessingResult.FilesProcessed > 0)
                    {
                        foreach (var plugin in applicablePlugins)
                        {
                            pluginsReceivedAnyData.Add(plugin.Name);
                        }
                    }
                }
                catch (Exception ex)
                {
                    var unhandledExceptionMessage = $"Unhandled exception occurred while processing log type {logType}. Exception: {ex.Message}";
                    _logger.LogError(ex, unhandledExceptionMessage);
                    var fakeProcessingFileResult = new ProcessFileResult(0, unhandledExceptionMessage, ExitReason.UnclassifiedError);
                    var fakeProcessingTypeResults = new ProcessLogTypeResult();
                    fakeProcessingTypeResults.AddProcessingInfo(TimeSpan.Zero, 0, fakeProcessingFileResult);
                    failedLogTypeResult = fakeProcessingTypeResults;
                    break;
                }
            }
            
            _logger.LogInformation("Telling all plugins to complete processing of any cached data");
            var pluginsExecutionResults = _pluginManager.SendCompleteProcessingSignalToPlugins(failedLogTypeResult != null);
            _logger.LogInformation("Completed reading log set and generating data");

            var (errorMessage, existReason) = GetExitReasonAndErrorMessageIfApplicable(failedLogTypeResult, logProcessingStatistics);
            var loadedPluginNames = loadedPlugins.Select(plugin => plugin.Name).ToHashSet();
            return new ProcessLogSetResult(
                errorMessage,
                existReason,
                logsExtractor.LogSetSizeBytes,
                logsExtractor.IsDirectory,
                loadedPluginNames,
                logProcessingStatistics,
                pluginsExecutionResults,
                pluginsReceivedAnyData);
        }

        private ProcessLogTypeResult ProcessLogType(IEnumerable<LogSetInfo> logSetParts, LogTypeInfo logTypeInfo, IList<IPlugin> plugins)
        {
            var overallProcessingNumbers = new ProcessLogTypeResult();
            using var _ = _logger.BeginScope(logTypeInfo.LogType);
            
            foreach (var logSetInfo in logSetParts)
            {
                _logger.LogInformation("Starting to process log set part `{logSetPartPath}` for {logType} logs", logSetInfo.Path, logTypeInfo.LogType);
                var partProcessingResults = logSetInfo.IsZip
                    ? ProcessZip(logSetInfo, logTypeInfo, plugins)
                    : ProcessDir(logSetInfo, logTypeInfo, plugins);
                _logger.LogInformation("Completed processing `{logSetPartPath}` for {logType} logs. {partProcessingResults}", logSetInfo.Path, logTypeInfo.LogType, partProcessingResults);
                overallProcessingNumbers.AddNumbersFrom(partProcessingResults);

                if (!partProcessingResults.IsSuccessful)
                {
                    break;
                }
            }

            return overallProcessingNumbers;
        }

        private ProcessLogTypeResult ProcessZip(LogSetInfo logSetInfo, LogTypeInfo logTypeInfo, IList<IPlugin> plugins)
        {
            var processingStatistics = new ProcessLogTypeResult();

            using var zip = ZipFile.Open(logSetInfo.Path, ZipArchiveMode.Read);
            foreach (var fileEntry in zip.Entries)
            {
                if (!logTypeInfo.FileBelongsToThisType(fileEntry.FullName))
                {
                    continue;
                }

                var fileNameWithPrefix = string.IsNullOrWhiteSpace(logSetInfo.Prefix)
                    ? fileEntry.FullName
                    : $"{logSetInfo.Prefix}/{fileEntry.FullName}";
                    
                _logger.LogInformation("Processing file {logFile}", fileNameWithPrefix);
                var fileTimer = Stopwatch.StartNew();
                var processFileResult = ProcessZippedFile(fileEntry, fileNameWithPrefix, logTypeInfo, plugins);
                processingStatistics.AddProcessingInfo(fileTimer.Elapsed, fileEntry.Length, processFileResult);

                if (!processFileResult.IsSuccessful)
                {
                    break;
                }

                LogFileProcessingResults(fileNameWithPrefix, fileEntry.Length, processFileResult, fileTimer.Elapsed);
            }

            return processingStatistics;
        }
        
        private ProcessLogTypeResult ProcessDir(LogSetInfo logSetInfo, LogTypeInfo logTypeInfo, IList<IPlugin> plugins)
        {
            var processingStatistics = new ProcessLogTypeResult();

            foreach (var filePath in Directory.EnumerateFiles(logSetInfo.Path, "*", SearchOption.AllDirectories))
            {
                var normalizedPath = filePath.NormalizePath(logSetInfo.Path);
                if (!logTypeInfo.FileBelongsToThisType(normalizedPath))
                {
                    continue;
                }

                _logger.LogInformation("Processing file {}", normalizedPath);
                var fileSizeBytes = new FileInfo(filePath).Length;
                var fileTimer = Stopwatch.StartNew();
                var processFileResult = ProcessFile(filePath, normalizedPath, logTypeInfo, plugins);
                processingStatistics.AddProcessingInfo(fileTimer.Elapsed, fileSizeBytes, processFileResult);

                if (!processFileResult.IsSuccessful)
                {
                    break;
                }
                
                LogFileProcessingResults(normalizedPath, fileSizeBytes, processFileResult, fileTimer.Elapsed);
            }

            return processingStatistics;
        }
        
        private ProcessFileResult ProcessZippedFile(ZipArchiveEntry fileEntry, string filePathWithPrefix, LogTypeInfo logTypeInfo, IList<IPlugin> plugins)
        {
            var logFileInfo = new LogFileInfo(
                fileEntry.Name,
                filePathWithPrefix,
                filePathWithPrefix.GetWorkerIdFromFilePath(),
                new DateTimeOffset(fileEntry.LastWriteTime.Ticks, TimeSpan.Zero).UtcDateTime // the ZipArchiveEntry doesn't currently support reading the timezone of the zip entry... so we strip it for consistency
            );
            
            // currently because of how zips store (or don't) timezone info for entries, the zipped and unzipped versions of this method produce different output.  Perhaps we can do better in the future.

            using var stream = fileEntry.Open();
            return ProcessStream(stream, logTypeInfo, logFileInfo, plugins);
        }
        
        private ProcessFileResult ProcessFile(string rawFilePath, string normalizedFilePath, LogTypeInfo logTypeInfo, IList<IPlugin> plugins)
        {
            var fileInfo = new FileInfo(rawFilePath); 
            var logFileInfo = new LogFileInfo(
                fileInfo.Name,
                normalizedFilePath,
                normalizedFilePath.GetWorkerIdFromFilePath(),
                new DateTimeOffset(fileInfo.LastWriteTime.Ticks, TimeSpan.Zero).UtcDateTime
            );

            using var stream = File.Open(rawFilePath, FileMode.Open);
            return ProcessStream(stream, logTypeInfo, logFileInfo, plugins);
        }

        private ProcessFileResult ProcessStream(Stream stream, LogTypeInfo logTypeInfo, LogFileInfo logFileInfo, IList<IPlugin> plugins)
        {
            var linesProcessed = 0;
            var reader = logTypeInfo.LogReaderProvider(stream, logFileInfo.FilePath);
            try
            {
                foreach (var line in reader.ReadLines())
                {
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
            catch (OutOfMemoryException ex)
            {
                var errorMessage = FailureReasonMessageGenerator.OutOfMemoryError(logFileInfo, plugins);
                _logger.LogInformation(ex, errorMessage);
                return new ProcessFileResult(linesProcessed, errorMessage, ExitReason.OutOfMemory);
            }

            return new ProcessFileResult(linesProcessed);
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

            var processedAnyFiles = logProcessingStatistics.Any(kvp => kvp.Value.FilesProcessed > 0);
            return processedAnyFiles 
                ? (null, ExitReason.CompletedSuccessfully) 
                : (FailureReasonMessageGenerator.NoTableauLogFilesFound(_config.RequestedPlugins, _config.OriginalLocation), ExitReason.LogSetDoesNotContainRelevantLogs);
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
        }
    }
}