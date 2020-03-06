using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using LogShark.Containers;
using LogShark.Extensions;
using LogShark.LogParser.Containers;
using LogShark.Plugins;
using Microsoft.Extensions.Logging;

namespace LogShark.LogParser
{
    public class TableauLogsReader
    {
        private readonly ILogger _logger;

        public TableauLogsReader(ILogger logger)
        {
            _logger = logger;
        }

        public LogProcessingStatistics ProcessLogs(IEnumerable<LogSetInfo> logSetParts, LogTypeInfo logTypeInfo, IList<IPlugin> plugins)
        {
            var overallProcessingNumbers = new LogProcessingStatistics();

            using (_logger.BeginScope(logTypeInfo.LogType))
            {
                foreach (var logSetInfo in logSetParts)
                {
                    _logger.LogInformation("Starting to process log set part `{logSetPartPath}` for {logType} logs", logSetInfo.Path, logTypeInfo.LogType);
                    var partProcessingResults = logSetInfo.IsZip
                        ? ProcessZip(logSetInfo, logTypeInfo, plugins)
                        : ProcessDir(logSetInfo, logTypeInfo, plugins);
                    _logger.LogInformation("Completed processing `{logSetPartPath}` for {logType} logs. {partProcessingResults}", logSetInfo.Path, logTypeInfo.LogType, partProcessingResults);
                    overallProcessingNumbers.AddNumbersFrom(partProcessingResults);
                }
            }

            return overallProcessingNumbers;
        }

        private LogProcessingStatistics ProcessZip(LogSetInfo logSetInfo, LogTypeInfo logTypeInfo, IList<IPlugin> plugins)
        {
            var processingStatistics = new LogProcessingStatistics();
            
            using (var zip = ZipFile.Open(logSetInfo.Path, ZipArchiveMode.Read))
            {
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
                    var linesInFile = ProcessZippedFile(fileEntry, fileNameWithPrefix, logTypeInfo, plugins);
                    processingStatistics.AddProcessingInfo(fileTimer.Elapsed, fileEntry.Length, linesInFile);

                    LogFileProcessingResults(fileNameWithPrefix, fileEntry.Length, linesInFile, fileTimer.Elapsed);
                }
            }

            return processingStatistics;
        }
        
        private LogProcessingStatistics ProcessDir(LogSetInfo logSetInfo, LogTypeInfo logTypeInfo, IList<IPlugin> plugins)
        {
            var processingStatistics = new LogProcessingStatistics();

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
                var linesInFile = ProcessFile(filePath, normalizedPath, logTypeInfo, plugins);
                processingStatistics.AddProcessingInfo(fileTimer.Elapsed, fileSizeBytes, linesInFile);

                LogFileProcessingResults(normalizedPath, fileSizeBytes, linesInFile, fileTimer.Elapsed);
            }

            return processingStatistics;
        }
        
        private static int ProcessZippedFile(ZipArchiveEntry fileEntry, string filePathWithPrefix, LogTypeInfo logTypeInfo, IList<IPlugin> plugins)
        {
            var logFileInfo = new LogFileInfo(
                fileEntry.Name,
                filePathWithPrefix,
                filePathWithPrefix.GetWorkerIdFromFilePath(),
                new DateTimeOffset(fileEntry.LastWriteTime.Ticks, TimeSpan.Zero).UtcDateTime // the ZipArchiveEntry doesn't currently support reading the timezone of the zip entry... so we strip it for consistency
            );
            
            // currently because of how zips store (or don't) timezone info for entries, the zipped and unzipped versions of this method produce different output.  Perhaps we can do better in the future.

            using (var stream = fileEntry.Open())
            {
                return ProcessStream(stream, logTypeInfo, logFileInfo, plugins);
            }
        }
        
        private static int ProcessFile(string rawFilePath, string normalizedFilePath, LogTypeInfo logTypeInfo, IList<IPlugin> plugins)
        {
            var fileInfo = new FileInfo(rawFilePath); 
            var logFileInfo = new LogFileInfo(
                fileInfo.Name,
                normalizedFilePath,
                normalizedFilePath.GetWorkerIdFromFilePath(),
                new DateTimeOffset(fileInfo.LastWriteTime.Ticks, TimeSpan.Zero).UtcDateTime
            );
            
            using (var stream = File.Open(rawFilePath, FileMode.Open))
            {
                return ProcessStream(stream, logTypeInfo, logFileInfo, plugins);
            }
        }

        private static int ProcessStream(Stream stream, LogTypeInfo logTypeInfo, LogFileInfo logFileInfo, IList<IPlugin> plugins)
        {
            var linesProcessed = 0;
            var reader = logTypeInfo.LogReaderProvider(stream, logFileInfo.FilePath);
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

            return linesProcessed;
        }

        private void LogFileProcessingResults(string filePath, long fileSizeBytes, int linesInFile, TimeSpan elapsed)
        {
            var fileSizeMb = fileSizeBytes / 1024 / 1024;
            var mbPerSecond = elapsed.TotalSeconds > 0
                ? fileSizeMb / elapsed.TotalSeconds
                : fileSizeMb;
            
            _logger.LogInformation("Completed processing file {logFile} in {logFileElapsed} ({logFileSizeMb} MB, {logFileLines} lines, {logFileMbPerSecond:F2} MB/s)", filePath, elapsed, fileSizeMb, linesInFile, mbPerSecond);           
        }
    }
}