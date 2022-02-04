using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using LogShark.LogParser;
using LogShark.Shared.Extensions;
using LogShark.Shared.LogReading.Containers;
using LogShark.Shared.LogReading.Readers;
using Microsoft.Extensions.Logging;

namespace LogShark.Shared.LogReading
{
    public class TableauLogsExtractor : IDisposable
    {
        private const string NestedZipTempDirName = "NestedZipFiles";

        private readonly ILogger _logger;
        private readonly TempFileTracker _tempFileTracker;
        private readonly string _rootPath;

        private static readonly IList<Regex> NestedZipRegexes = new List<Regex>
        {
            new Regex(@"^worker\d+.zip", RegexOptions.Compiled), // Tabadmin logs
            new Regex(@"^[^/]*/tabadminagent[^/]*\.zip", RegexOptions.Compiled) // TSMv0 (2018.1 Linux only)
        };

        public IList<LogSetInfo> LogSetParts { get; }
        public long LogSetSizeBytes { get; private set; }
        public bool IsDirectory { get; private set; }

        public TableauLogsExtractor(string logSetPath, string tempDir, IProcessingNotificationsCollector processingNotificationsCollector, ILogger logger)
        {
            _logger = logger;
            _logger.LogInformation("Starting {extractorName} for log set `{logSetPath}`", nameof(TableauLogsExtractor), logSetPath ?? "(null)");

            if (!File.Exists(logSetPath) && !Directory.Exists(logSetPath))
            {
                var message = $"`{logSetPath}` does not exist";
                _logger.LogError(message);
                throw new ArgumentException(message);
            }

            _rootPath = logSetPath;
            _tempFileTracker = new TempFileTracker();
            LogSetParts = EvaluateLogSetStructure(logSetPath, tempDir, processingNotificationsCollector);
            
            _logger.LogInformation("Completed {extractorName} for log set `{logSetPath}`", nameof(TableauLogsExtractor), logSetPath ?? "(null)");
        }
        
        public static FileCanBeOpenedResult FileCanBeOpened(string path, ILogger logger)
        {
            try
            {
                if (IsPathADirectory(path))
                {
                    return FileCanBeOpenedResult.Success();
                }

                using var zip = ZipFile.Open(path, ZipArchiveMode.Read);
                var records = zip.Entries.ToList(); // This would fail if zip is corrupt or not a zip file at all
                var firstFileEntry = records.FirstOrDefault(record => record.Name != ""); // Only files have names in ZipArchiveEntry (per https://stackoverflow.com/questions/40223451/how-to-tell-if-a-ziparchiveentry-is-directory)
                if (firstFileEntry == null)
                {
                    return FileCanBeOpenedResult.Failure("Zip file appears to not contain any files");
                }
                    
                using (var stream = firstFileEntry.Open())
                {
                    var reader = new SimpleLinePerLineReader(stream);
                    var line = reader.ReadLines().FirstOrDefault(); // This would fail if zip is password protected 
                }
                
                return FileCanBeOpenedResult.Success();
            }
            catch (Exception ex)
            {
                logger?.LogDebug($"{nameof(FileCanBeOpened)} failed with `{ex.Message}`");
                return FileCanBeOpenedResult.Failure(ex.Message);
            }
        }

        public static FileIsZipWithLogsResult FileIsAZipWithLogs(string path, ILogger logger)
        {
            try
            {
                if (IsPathADirectory(path))
                {
                    return FileIsZipWithLogsResult.InvalidZip("Path is directory and not a file");
                }

                var zipCanBeOpened = FileCanBeOpened(path, logger);
                if (!zipCanBeOpened.FileCanBeOpened)
                {
                    return FileIsZipWithLogsResult.InvalidZip($"Zip file cannot be opened. Reported error: `{zipCanBeOpened.ErrorMessage}`");
                }

                var allKnownLogLocations = LogTypeDetails.GetAllKnownLogFileLocations().ToList();
                using (var zip = ZipFile.Open(path, ZipArchiveMode.Read))
                {
                    foreach (var fileEntry in zip.Entries)
                    {
                        if (allKnownLogLocations.Any(regex => regex.IsMatch(fileEntry.FullName)))
                        {
                            return FileIsZipWithLogsResult.Success();
                        }
                    }
                }
                
                return FileIsZipWithLogsResult.NoLogsFound();
            }
            catch (Exception ex)
            {
                logger?.LogDebug($"{nameof(FileIsAZipWithLogs)} failed with `{ex.Message}`");
                return FileIsZipWithLogsResult.InvalidZip(ex.Message);
            }
        }

        public void Dispose()
        {
            _logger.LogInformation("Disposing log extractor temporary files...");
            _tempFileTracker.Dispose();
            _logger.LogInformation("Log extractor temporary files disposed");
        }

        private static bool IsPathADirectory(string path)
        {
            return Directory.Exists(path);
        }

        private IList<LogSetInfo> EvaluateLogSetStructure(string logSetPath, string tempDirRoot, IProcessingNotificationsCollector processingNotificationsCollector)
        {
            IsDirectory = IsPathADirectory(logSetPath);
            _logger.LogInformation("Provided log set appears to be {logSetType}", IsDirectory ? "directory" : "zip file");

            var rootFilePaths = GetFilePaths(logSetPath, IsDirectory);
            var rootSetInfo = new LogSetInfo(rootFilePaths, logSetPath, string.Empty, !IsDirectory, _rootPath);
            var zipInfoList = new List<LogSetInfo> {rootSetInfo};

            zipInfoList.AddRange(IsDirectory
                ? LookForZippedPartsInDirectory(logSetPath)
                : HandleNestedZips(logSetPath, tempDirRoot, processingNotificationsCollector)
            );

            LogSetSizeBytes = IsDirectory
                ? GetDirSize(_rootPath)
                : new FileInfo(_rootPath).Length;

            return zipInfoList;
        }

        private IEnumerable<LogSetInfo> HandleNestedZips(string zippedLogSetPath, string tempDirRoot, IProcessingNotificationsCollector processingNotificationsCollector)
        {
            _logger.LogInformation("Looking for known nested zip files inside zip file '{logSetPath}'", zippedLogSetPath);

            using var zip = ZipFile.Open(zippedLogSetPath, ZipArchiveMode.Read);
            var nestedZips = zip.Entries.Where(IsNestedZip).ToList();
            var nestedZipFilesInfo = new List<LogSetInfo>();

            if (nestedZips.Count == 0)
            {
                _logger.LogInformation("No known nested zip files found in '{logSetPath}", zippedLogSetPath);
                return nestedZipFilesInfo;
            }

            var tempDir = Path.Combine(tempDirRoot, NestedZipTempDirName);
            Directory.CreateDirectory(tempDir);
            _tempFileTracker.AddDirectory(tempDir);

            foreach (var nestedZip in nestedZips)
            {
                var extractPath = Path.Combine(tempDir, nestedZip.Name);
                _logger.LogInformation("Extracting nested archive {nestedZipName} into '{nestedZipExtractPath}'", nestedZip.Name, extractPath);

                nestedZip.ExtractToFile(extractPath);
                _logger.LogInformation("Successfully extracted {nestedZipName} into '{nestedZipExtractPath}'", nestedZip.Name, extractPath);

                var checkResult = FileCanBeOpened(extractPath, _logger); 
                if (checkResult.FileCanBeOpened)
                {
                    var filePaths = GetFilePaths(extractPath, false);
                    nestedZipFilesInfo.Add(new LogSetInfo(filePaths, extractPath.NormalizeSeparatorsToUnix(), nestedZip.FullName.RemoveZipFromTail(), true, _rootPath));
                }
                else
                {
                    var error = $"Nested zip \"{nestedZip.FullName}\" appears to be corrupt. It will not be processed. Underlying error: {checkResult.ErrorMessage ?? "(null)"}";
                    processingNotificationsCollector.ReportError(error, nestedZip.FullName, 0, nameof(TableauLogsExtractor));
                }
            }

            _logger.LogInformation("Found and extracted {numberOfNestedZips} known nested zip files inside `{logSetPath}`", nestedZipFilesInfo.Count, zippedLogSetPath);
            return nestedZipFilesInfo;
        }

        private IEnumerable<LogSetInfo> LookForZippedPartsInDirectory(string logSetDirectoryPath)
        {
            _logger.LogInformation("Looking for typical zip files inside '{logSetPath}' folder", logSetDirectoryPath);

            var allFiles = Directory.EnumerateFiles(logSetDirectoryPath, "*", SearchOption.AllDirectories);
            var allKnownZips = allFiles
                .Where(path => IsNestedZip(path, true))
                .Select(path =>
                {
                    var filePaths = GetFilePaths(path, false);
                    return new LogSetInfo(filePaths, path.NormalizeSeparatorsToUnix(), path.NormalizePath(_rootPath).RemoveZipFromTail(), true, _rootPath);
                })
                .ToList();

            if (allKnownZips.Count > 0)
            {
                _logger.LogInformation("Found {numberOfNestedZips} known zip files inside `{logSetPath}`. Not extracting them, as they are not nested", allKnownZips.Count, logSetDirectoryPath);
            }
            else
            {
                _logger.LogInformation("Did not found any known zip files inside `{logSetPath}`", logSetDirectoryPath);
            }

            return allKnownZips;
        }

        private bool IsNestedZip(ZipArchiveEntry zipEntry)
        {
            return IsNestedZip(zipEntry.FullName);
        }

        private bool IsNestedZip(string path, bool needsNormalization = false)
        {
            var normalizedPath = needsNormalization
                ? path.NormalizePath(_rootPath)
                : path;

            return NestedZipRegexes.Any(regex => regex.IsMatch(normalizedPath));
        }

        private static long GetDirSize(string path)
        {
            return new DirectoryInfo(path)
                .GetFiles("*", SearchOption.AllDirectories)
                .Sum(fileInfo => fileInfo.Length);
        }

        private static List<string> GetFilePaths(string path, bool isDirectory)
        {
            if (isDirectory)
            {
                return Directory
                    .EnumerateFiles(path, "*", SearchOption.AllDirectories)
                    .ToList();
            }
            
            using var zipArchive = ZipFile.Open(path, ZipArchiveMode.Read);
            return zipArchive.Entries
                .Select(entry => entry.FullName)
                .ToList();
        }
    }
}