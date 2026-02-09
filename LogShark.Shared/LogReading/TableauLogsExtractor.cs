using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Formats.Tar;
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
        private const string NestedTarTempDirName = "NestedTarFiles";

        private readonly ILogger _logger;
        private readonly TempFileTracker _tempFileTracker;
        private readonly string _rootPath;

        private static readonly IList<Regex> NestedZipRegexes = new List<Regex>
        {
            new Regex(@"^worker\d+.zip", RegexOptions.Compiled), // Tabadmin logs
            new Regex(@".*\Bridge.*.zip$", RegexOptions.Compiled), // Bridge logs
            new Regex(@"^[^/]*/tabadminagent[^/]*\.zip", RegexOptions.Compiled) // TSMv0 (2018.1 Linux only)
        };

        private static readonly IList<Regex> NestedTarRegexes = new List<Regex>
        {
            new Regex(@"^[^/]*\.tar$", RegexOptions.Compiled), // Bridge logs - .tar files in root
            new Regex(@".*[/\\][^/]*\.tar$", RegexOptions.Compiled), // Bridge logs - .tar files in subdirectories
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

                // Check if it's a .tar file first
                if (path.EndsWith(".tar", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        using var tarStream = File.OpenRead(path);
                        using var tarReader = new TarReader(tarStream);
                        var firstEntry = tarReader.GetNextEntry();
                        if (firstEntry == null)
                        {
                            return FileCanBeOpenedResult.Failure("Tar file appears to not contain any entries");
                        }
                        return FileCanBeOpenedResult.Success();
                    }
                    catch (Exception tarEx)
                    {
                        // Try Windows tar command as fallback for Linux/compressed tar files
                        try
                        {
                            var processInfo = new ProcessStartInfo
                            {
                                FileName = "tar",
                                Arguments = $"-tf \"{path}\"",
                                UseShellExecute = false,
                                RedirectStandardOutput = true,
                                RedirectStandardError = true,
                                CreateNoWindow = true
                            };

                            using (var process = Process.Start(processInfo))
                            {
                                process.WaitForExit();
                                if (process.ExitCode == 0)
                                {
                                    return FileCanBeOpenedResult.Success();
                                }
                            }
                        }
                        catch
                        {
                            // Ignore fallback errors
                        }
                        
                        return FileCanBeOpenedResult.Failure($"Failed to read TAR file: {tarEx.Message}. This might be a compressed TAR file (.tar.gz, .tar.bz2) or an unsupported TAR format.");
                    }
                }

                // Default to ZIP file handling
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

                var fileCanBeOpened = FileCanBeOpened(path, logger);
                if (!fileCanBeOpened.FileCanBeOpened)
                {
                    return FileIsZipWithLogsResult.InvalidZip($"Archive file cannot be opened. Reported error: `{fileCanBeOpened.ErrorMessage}`");
                }

                var allKnownLogLocations = LogTypeDetails.GetAllKnownLogFileLocations().ToList();
                
                // Handle .tar files
                if (path.EndsWith(".tar", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        using var tarStream = File.OpenRead(path);
                        using var tarReader = new TarReader(tarStream);
                        
                        while (tarReader.GetNextEntry() is TarEntry entry)
                        {
                            if (allKnownLogLocations.Any(regex => regex.IsMatch(entry.Name)))
                            {
                                return FileIsZipWithLogsResult.Success();
                            }
                        }
                        
                        return FileIsZipWithLogsResult.NoLogsFound();
                    }
                    catch
                    {
                        // Fallback to Windows tar command for Linux/compressed tar files
                        try
                        {
                            var processInfo = new ProcessStartInfo
                            {
                                FileName = "tar",
                                Arguments = $"-tf \"{path}\"",
                                UseShellExecute = false,
                                RedirectStandardOutput = true,
                                RedirectStandardError = true,
                                CreateNoWindow = true
                            };

                            using (var process = Process.Start(processInfo))
                            {
                                process.WaitForExit();
                                if (process.ExitCode == 0)
                                {
                                    var output = process.StandardOutput.ReadToEnd();
                                    var fileList = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                                    
                                    foreach (var file in fileList)
                                    {
                                        if (allKnownLogLocations.Any(regex => regex.IsMatch(file.Trim())))
                                        {
                                            return FileIsZipWithLogsResult.Success();
                                        }
                                    }
                                    
                                    return FileIsZipWithLogsResult.NoLogsFound();
                                }
                            }
                        }
                        catch
                        {
                            // Ignore fallback errors
                        }
                        
                        return FileIsZipWithLogsResult.InvalidZip("TAR file could not be read with either .NET or Windows tar command");
                    }
                }

                // Handle .zip files
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
            var isTarFile = !IsDirectory && logSetPath.EndsWith(".tar", StringComparison.OrdinalIgnoreCase);
            
            var logSetType = IsDirectory ? "directory" : (isTarFile ? "tar file" : "zip file");
            _logger.LogInformation("Provided log set appears to be {logSetType}", logSetType);

            var zipInfoList = new List<LogSetInfo>();

            if (isTarFile)
            {
                // Handle direct .tar file input
                var tarExtractDir = Path.Combine(tempDirRoot, "TarExtraction");
                Directory.CreateDirectory(tarExtractDir);
                _tempFileTracker.AddDirectory(tarExtractDir);

                try
                {
                    using (var tarStream = File.OpenRead(logSetPath))
                    {
                        TarFile.ExtractToDirectory(tarStream, tarExtractDir, overwriteFiles: true);
                    }

                    var tarFilePaths = Directory.EnumerateFiles(tarExtractDir, "*", SearchOption.AllDirectories).ToList();
                    var rootSetInfo = new LogSetInfo(tarFilePaths, tarExtractDir, string.Empty, false, _rootPath);
                    zipInfoList.Add(rootSetInfo);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("System.Formats.Tar failed to extract {tarFile}. Trying Windows tar command as fallback. Error: {error}", logSetPath, ex.Message);
                    
                    // Fallback to Windows tar command for Linux/compressed tar files
                    try
                    {
                        var processInfo = new ProcessStartInfo
                        {
                            FileName = "tar",
                            Arguments = $"-xf \"{logSetPath}\" -C \"{tarExtractDir}\"",
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            CreateNoWindow = true
                        };

                        using (var process = Process.Start(processInfo))
                        {
                            process.WaitForExit();
                            if (process.ExitCode == 0)
                            {
                                var tarFilePaths = Directory.EnumerateFiles(tarExtractDir, "*", SearchOption.AllDirectories).ToList();
                                var rootSetInfo = new LogSetInfo(tarFilePaths, tarExtractDir, string.Empty, false, _rootPath);
                                zipInfoList.Add(rootSetInfo);
                                _logger.LogInformation("Successfully extracted tar file using Windows tar command: {tarFile}", logSetPath);
                            }
                            else
                            {
                                var error = $"Could not extract tar file \"{logSetPath}\" using either .NET or Windows tar. Windows tar exit code: {process.ExitCode}";
                                processingNotificationsCollector.ReportError(error, logSetPath, 0, nameof(TableauLogsExtractor));
                                _logger.LogError(error);
                            }
                        }
                    }
                    catch (Exception fallbackEx)
                    {
                        var error = $"Could not extract tar file \"{logSetPath}\". .NET error: {ex.Message}. Windows tar fallback error: {fallbackEx.Message}";
                        processingNotificationsCollector.ReportError(error, logSetPath, 0, nameof(TableauLogsExtractor));
                        _logger.LogError(error);
                    }
                }
            }
            else
            {
                // Handle directory or zip file input
                var rootFilePaths = GetFilePaths(logSetPath, IsDirectory);
                var rootSetInfo = new LogSetInfo(rootFilePaths, logSetPath, string.Empty, !IsDirectory, _rootPath);
                zipInfoList.Add(rootSetInfo);

                // Check if this is a Bridge log archive that needs special handling
                if (!IsDirectory && IsBridgeLogArchive(logSetPath))
                {
                    // Use Bridge-specific recursive extraction
                    zipInfoList.AddRange(HandleBridgeLogArchives(logSetPath, tempDirRoot, processingNotificationsCollector));
                }
                else
                {
                    // Use original server log extraction logic
                    zipInfoList.AddRange(IsDirectory
                        ? LookForZippedPartsInDirectory(logSetPath)
                        : HandleNestedZips(logSetPath, tempDirRoot, processingNotificationsCollector)
                    );
                }
            }

            LogSetSizeBytes = IsDirectory
                ? GetDirSize(_rootPath)
                : new FileInfo(_rootPath).Length;

            return zipInfoList;
        }

        private bool IsBridgeLogArchive(string zipPath)
        {
            try
            {
                using var zip = ZipFile.Open(zipPath, ZipArchiveMode.Read);
                
                // Check for Bridge log indicators:
                // 1. Contains .tar files (Linux Bridge logs)
                // 2. Contains bridge-related folder names
                // 3. Contains TabBridgeClientWorker log files
                var entries = zip.Entries.ToList();
                
                // Look for .tar files (common in Linux Bridge logs)
                if (entries.Any(e => e.FullName.EndsWith(".tar", StringComparison.OrdinalIgnoreCase)))
                {
                    return true;
                }
                
                // Look for bridge-related folder patterns
                var bridgePatterns = new[]
                {
                    "bridge-",
                    "Bridge-",
                    "TabBridge",
                    "tabbridge"
                };
                
                if (entries.Any(e => bridgePatterns.Any(pattern => e.FullName.Contains(pattern, StringComparison.OrdinalIgnoreCase))))
                {
                    return true;
                }
                
                // Look for Bridge-specific log files
                if (entries.Any(e => e.FullName.Contains("TabBridgeClientWorker", StringComparison.OrdinalIgnoreCase)))
                {
                    return true;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Could not determine if archive is Bridge logs: {error}", ex.Message);
                return false;
            }
        }

        private IEnumerable<LogSetInfo> HandleBridgeLogArchives(string bridgeLogArchive, string tempDirRoot, IProcessingNotificationsCollector processingNotificationsCollector)
        {
            _logger.LogInformation("Processing Bridge log archive with recursive extraction: '{logSetPath}'", bridgeLogArchive);

            var allExtractedArchives = new List<LogSetInfo>();
            var archivesToProcess = new Queue<string>();
            var processedArchives = new HashSet<string>();
            
            archivesToProcess.Enqueue(bridgeLogArchive);

            while (archivesToProcess.Count > 0)
            {
                var currentArchive = archivesToProcess.Dequeue();
                
                if (processedArchives.Contains(currentArchive))
                    continue;
                    
                processedArchives.Add(currentArchive);

                try
                {
                    using var zip = ZipFile.Open(currentArchive, ZipArchiveMode.Read);
                    var extractedArchives = ExtractAllBridgeNestedArchives(zip, tempDirRoot, processingNotificationsCollector);
                    
                    foreach (var extractedArchive in extractedArchives)
                    {
                        allExtractedArchives.Add(extractedArchive);
                        
                        // If the extracted archive is also an archive file, queue it for processing
                        if (extractedArchive.IsZip && IsBridgeArchiveFile(extractedArchive.Path))
                        {
                            archivesToProcess.Enqueue(extractedArchive.Path);
                        }
                    }
                }
                catch (Exception ex)
                {
                    var error = $"Could not process Bridge archive \"{currentArchive}\" for nested archives. Error: {ex.Message}";
                    processingNotificationsCollector.ReportError(error, currentArchive, 0, nameof(TableauLogsExtractor));
                    _logger.LogError(error);
                }
            }

            _logger.LogInformation("Found and extracted {numberOfNestedArchives} nested archives from Bridge logs in `{logSetPath}`", allExtractedArchives.Count, bridgeLogArchive);
            return allExtractedArchives;
        }

        private IEnumerable<LogSetInfo> ExtractAllBridgeNestedArchives(ZipArchive zip, string tempDirRoot, IProcessingNotificationsCollector processingNotificationsCollector)
        {
            var extractedArchives = new List<LogSetInfo>();
            
            // Find ALL .zip and .tar files in the archive (not just known patterns)
            var nestedZips = zip.Entries.Where(entry => entry.FullName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)).ToList();
            var nestedTars = zip.Entries.Where(entry => entry.FullName.EndsWith(".tar", StringComparison.OrdinalIgnoreCase)).ToList();

            // Handle nested ZIP files
            if (nestedZips.Count > 0)
            {
                var tempDir = Path.Combine(tempDirRoot, "BridgeNestedZips", Guid.NewGuid().ToString("N")[..8]);
                Directory.CreateDirectory(tempDir);
                _tempFileTracker.AddDirectory(tempDir);

                foreach (var nestedZip in nestedZips)
                {
                    var extractPath = Path.Combine(tempDir, nestedZip.Name);
                    _logger.LogInformation("Extracting Bridge nested archive {nestedZipName} into '{nestedZipExtractPath}'", nestedZip.Name, extractPath);

                    nestedZip.ExtractToFile(extractPath);
                    _logger.LogInformation("Successfully extracted Bridge nested archive {nestedZipName}", nestedZip.Name);

                    var checkResult = FileCanBeOpened(extractPath, _logger); 
                    if (checkResult.FileCanBeOpened)
                    {
                        var filePaths = GetFilePaths(extractPath, false);
                        extractedArchives.Add(new LogSetInfo(filePaths, extractPath.NormalizeSeparatorsToUnix(), nestedZip.FullName.RemoveZipFromTail(), true, _rootPath));
                    }
                    else
                    {
                        var error = $"Bridge nested zip \"{nestedZip.FullName}\" appears to be corrupt. It will not be processed. Underlying error: {checkResult.ErrorMessage ?? "(null)"}";
                        processingNotificationsCollector.ReportError(error, nestedZip.FullName, 0, nameof(TableauLogsExtractor));
                    }
                }
            }

            // Handle nested TAR files  
            if (nestedTars.Count > 0)
            {
                var tempDir = Path.Combine(tempDirRoot, "BridgeNestedTars", Guid.NewGuid().ToString("N")[..8]);
                Directory.CreateDirectory(tempDir);
                _tempFileTracker.AddDirectory(tempDir);

                foreach (var nestedTar in nestedTars)
                {
                    var extractPath = Path.Combine(tempDir, nestedTar.Name);
                    _logger.LogInformation("Extracting Bridge nested tar archive {nestedTarName} into '{nestedTarExtractPath}'", nestedTar.Name, extractPath);

                    nestedTar.ExtractToFile(extractPath);
                    _logger.LogInformation("Successfully extracted Bridge nested tar archive {nestedTarName}", nestedTar.Name);

                    try
                    {
                        // Extract the tar file to a directory
                        var tarExtractDir = Path.Combine(tempDir, Path.GetFileNameWithoutExtension(nestedTar.Name));
                        Directory.CreateDirectory(tarExtractDir);
                        
                        bool extracted = false;
                        Exception lastException = null;
                        
                        // Try .NET tar extraction first
                        try
                        {
                            using (var tarStream = File.OpenRead(extractPath))
                            {
                                TarFile.ExtractToDirectory(tarStream, tarExtractDir, overwriteFiles: true);
                            }
                            extracted = true;
                        }
                        catch (Exception ex)
                        {
                            lastException = ex;
                            _logger.LogWarning("System.Formats.Tar failed to extract Bridge nested tar {nestedTarName}. Trying Windows tar command as fallback. Error: {error}", nestedTar.Name, ex.Message);
                            
                            // Fallback to Windows tar command for Linux/compressed tar files
                            try
                            {
                                var processInfo = new ProcessStartInfo
                                {
                                    FileName = "tar",
                                    Arguments = $"-xf \"{extractPath}\" -C \"{tarExtractDir}\"",
                                    UseShellExecute = false,
                                    RedirectStandardOutput = true,
                                    RedirectStandardError = true,
                                    CreateNoWindow = true
                                };

                                using (var process = Process.Start(processInfo))
                                {
                                    process.WaitForExit();
                                    if (process.ExitCode == 0)
                                    {
                                        extracted = true;
                                        _logger.LogInformation("Successfully extracted Bridge nested tar {nestedTarName} using Windows tar command", nestedTar.Name);
                                    }
                                    else
                                    {
                                        lastException = new Exception($"Windows tar command failed with exit code {process.ExitCode}");
                                    }
                                }
                            }
                            catch (Exception fallbackEx)
                            {
                                lastException = fallbackEx;
                            }
                        }

                        if (extracted)
                        {
                            // Get all files from the extracted tar directory
                            var tarFilePaths = Directory.EnumerateFiles(tarExtractDir, "*", SearchOption.AllDirectories).ToList();
                            
                            extractedArchives.Add(new LogSetInfo(tarFilePaths, tarExtractDir.NormalizeSeparatorsToUnix(), nestedTar.FullName.RemoveTarFromTail(), false, _rootPath));
                            
                            // Clean up the intermediate tar file
                            File.Delete(extractPath);
                        }
                        else
                        {
                            var error = $"Bridge nested tar \"{nestedTar.FullName}\" could not be extracted. Final error: {lastException?.Message}";
                            processingNotificationsCollector.ReportError(error, nestedTar.FullName, 0, nameof(TableauLogsExtractor));
                        }
                    }
                    catch (Exception ex)
                    {
                        var error = $"Bridge nested tar \"{nestedTar.FullName}\" could not be extracted. Underlying error: {ex.Message}";
                        processingNotificationsCollector.ReportError(error, nestedTar.FullName, 0, nameof(TableauLogsExtractor));
                    }
                }
            }

            return extractedArchives;
        }

        private bool IsBridgeArchiveFile(string path)
        {
            return path.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) || 
                   path.EndsWith(".tar", StringComparison.OrdinalIgnoreCase);
        }

        private IEnumerable<LogSetInfo> LookForZippedPartsInDirectory(string logSetDirectoryPath)
        {
            _logger.LogInformation("Looking for typical zip and tar files inside '{logSetPath}' folder", logSetDirectoryPath);

            var allFiles = Directory.EnumerateFiles(logSetDirectoryPath, "*", SearchOption.AllDirectories);
            var allKnownZips = allFiles
                .Where(path => IsNestedZip(path, true) || IsNestedTar(path, true))
                .Select(path =>
                {
                    var isTar = IsNestedTar(path, true);
                    if (isTar)
                    {
                        // For tar files, we need to extract them and return the directory contents
                        var tarExtractDir = Path.Combine(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path) + "_extracted");
                        
                        try
                        {
                            if (!Directory.Exists(tarExtractDir))
                            {
                                Directory.CreateDirectory(tarExtractDir);
                                using (var tarStream = File.OpenRead(path))
                                {
                                    TarFile.ExtractToDirectory(tarStream, tarExtractDir, overwriteFiles: true);
                                }
                            }
                            
                            var tarFilePaths = Directory.EnumerateFiles(tarExtractDir, "*", SearchOption.AllDirectories).ToList();
                            return new LogSetInfo(tarFilePaths, tarExtractDir.NormalizeSeparatorsToUnix(), path.NormalizePath(_rootPath).RemoveTarFromTail(), false, _rootPath);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning("Failed to extract tar file {tarPath}: {error}", path, ex.Message);
                            return null;
                        }
                    }
                    else
                    {
                        var filePaths = GetFilePaths(path, false);
                        return new LogSetInfo(filePaths, path.NormalizeSeparatorsToUnix(), path.NormalizePath(_rootPath).RemoveZipFromTail(), true, _rootPath);
                    }
                })
                .Where(logSetInfo => logSetInfo != null)
                .ToList();

            if (allKnownZips.Count > 0)
            {
                _logger.LogInformation("Found {numberOfNestedArchives} known archives inside `{logSetPath}`. Not extracting them, as they are not nested", allKnownZips.Count, logSetDirectoryPath);
            }
            else
            {
                _logger.LogInformation("Did not found any known archives inside `{logSetPath}`", logSetDirectoryPath);
            }

            return allKnownZips;
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

        private IEnumerable<LogSetInfo> HandleNestedZips(string zippedLogSetPath, string tempDirRoot, IProcessingNotificationsCollector processingNotificationsCollector)
        {
            _logger.LogInformation("Looking for known nested zip files inside zip file '{logSetPath}'", zippedLogSetPath);

            using var zip = ZipFile.Open(zippedLogSetPath, ZipArchiveMode.Read);
            var nestedZips = zip.Entries.Where(IsNestedZip).ToList();
            var nestedTars = zip.Entries.Where(IsNestedTar).ToList();
            var nestedZipFilesInfo = new List<LogSetInfo>();

            // Handle nested ZIP files
            if (nestedZips.Count > 0)
            {
                var tempDir = Path.Combine(tempDirRoot, NestedZipTempDirName);
                var extractedDir = Path.Combine(tempDirRoot, "extracted");
                Directory.CreateDirectory(tempDir);
                _tempFileTracker.AddDirectory(tempDir);

                foreach (var nestedZip in nestedZips)
                {
                    var extractPath = Path.Combine(tempDir, nestedZip.Name);
                    _logger.LogInformation("Extracting nested archive {nestedZipName} into '{nestedZipExtractPath}'", nestedZip.Name, extractPath);
                    var zipFile = nestedZip.FullName;
                   
                    nestedZip.ExtractToFile(extractPath, overwrite: true);
                  //  ZipFile.ExtractToDirectory(extractPath,Path.Combine( extractedDir,zipFile),System.Text.Encoding.Default ,true);
                   // File.Delete(extractPath);
                    _logger.LogInformation("Successfully extracted {nestedZipName} into '{nestedZipExtractPath}'", nestedZip.Name, extractPath);

                    var checkResult = FileCanBeOpened(extractPath, _logger); 
                    if (checkResult.FileCanBeOpened)
                    {
                        var filePaths = GetFilePaths(extractPath, false);
                        nestedZipFilesInfo.Add(new LogSetInfo(filePaths, extractPath.NormalizeSeparatorsToUnix(), nestedZip.FullName.RemoveZipFromTail(), true, _rootPath));
                     //   var zipFilePaths = Directory.EnumerateFiles(extractedDir, "*", SearchOption.AllDirectories).ToList();
                      //  var normalizedZipPaths = zipFilePaths.Select(path => path.NormalizePath(tempDir)).ToList();

                     //   nestedZipFilesInfo.Add(new LogSetInfo(normalizedZipPaths, tempDir.NormalizeSeparatorsToUnix(), nestedZip.FullName.RemoveTarFromTail(), false, _rootPath));


                      
                    }
                    else
                    {
                        var error = $"Nested zip \"{nestedZip.FullName}\" appears to be corrupt. It will not be processed. Underlying error: {checkResult.ErrorMessage ?? "(null)"}";
                        processingNotificationsCollector.ReportError(error, nestedZip.FullName, 0, nameof(TableauLogsExtractor));
                    }
                }
            }

            // Handle nested TAR files  
            if (nestedTars.Count > 0)
            {
                var tempDir = Path.Combine(tempDirRoot, NestedTarTempDirName);
                Directory.CreateDirectory(tempDir);
                _tempFileTracker.AddDirectory(tempDir);

                foreach (var nestedTar in nestedTars)
                {
                    var extractPath = Path.Combine(tempDir, nestedTar.Name);
                    _logger.LogInformation("Extracting nested tar archive {nestedTarName} into '{nestedTarExtractPath}'", nestedTar.Name, extractPath);

                    nestedTar.ExtractToFile(extractPath);
                    _logger.LogInformation("Successfully extracted {nestedTarName} into '{nestedTarExtractPath}'", nestedTar.Name, extractPath);

                    try
                    {
                        // Extract the tar file to a directory
                        var tarExtractDir = Path.Combine(tempDir, Path.GetFileNameWithoutExtension(nestedTar.Name));
                        Directory.CreateDirectory(tarExtractDir);
                        
                        bool extracted = false;
                        Exception lastException = null;
                        
                        // Try .NET tar extraction first
                        try
                        {
                            using (var tarStream = File.OpenRead(extractPath))
                            {
                                TarFile.ExtractToDirectory(tarStream, tarExtractDir, overwriteFiles: true);
                            }
                            extracted = true;
                        }
                        catch (Exception ex)
                        {
                            lastException = ex;
                            _logger.LogWarning("System.Formats.Tar failed to extract nested tar {nestedTarName}. Trying Windows tar command as fallback. Error: {error}", nestedTar.Name, ex.Message);
                            
                            // Fallback to Windows tar command for Linux/compressed tar files
                            try
                            {
                                var processInfo = new ProcessStartInfo
                                {
                                    FileName = "tar",
                                    Arguments = $"-xf \"{extractPath}\" -C \"{tarExtractDir}\"",
                                    UseShellExecute = false,
                                    RedirectStandardOutput = true,
                                    RedirectStandardError = true,
                                    CreateNoWindow = true
                                };

                                using (var process = Process.Start(processInfo))
                                {
                                    process.WaitForExit();
                                    if (process.ExitCode == 0)
                                    {
                                        extracted = true;
                                        _logger.LogInformation("Successfully extracted nested tar {nestedTarName} using Windows tar command", nestedTar.Name);
                                    }
                                    else
                                    {
                                        lastException = new Exception($"Windows tar command failed with exit code {process.ExitCode}");
                                    }
                                }
                            }
                            catch (Exception fallbackEx)
                            {
                                lastException = fallbackEx;
                            }
                        }

                        if (extracted)
                        {
                            // Get all files from the extracted tar directory
                            var tarFilePaths = Directory.EnumerateFiles(tarExtractDir, "*", SearchOption.AllDirectories).ToList();
                            var normalizedTarPaths = tarFilePaths.Select(path => path.NormalizePath(tarExtractDir)).ToList();
                            
                            nestedZipFilesInfo.Add(new LogSetInfo(normalizedTarPaths, tarExtractDir.NormalizeSeparatorsToUnix(), nestedTar.FullName.RemoveTarFromTail(), false, _rootPath));
                            
                            // Clean up the intermediate tar file
                            File.Delete(extractPath);
                        }
                        else
                        {
                            var error = $"Nested tar \"{nestedTar.FullName}\" could not be extracted. Final error: {lastException?.Message}";
                            processingNotificationsCollector.ReportError(error, nestedTar.FullName, 0, nameof(TableauLogsExtractor));
                        }
                    }
                    catch (Exception ex)
                    {
                        var error = $"Nested tar \"{nestedTar.FullName}\" could not be extracted. Underlying error: {ex.Message}";
                        processingNotificationsCollector.ReportError(error, nestedTar.FullName, 0, nameof(TableauLogsExtractor));
                    }
                }
            }

            if (nestedZips.Count == 0 && nestedTars.Count == 0)
            {
                _logger.LogInformation("No known nested archives found in '{logSetPath}", zippedLogSetPath);
            }
            else
            {
                _logger.LogInformation("Found and extracted {numberOfNestedArchives} known nested archives inside `{logSetPath}`", nestedZipFilesInfo.Count, zippedLogSetPath);
            }

            return nestedZipFilesInfo;
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

        private bool IsNestedTar(ZipArchiveEntry zipEntry)
        {
            return IsNestedTar(zipEntry.FullName);
        }

        private bool IsNestedTar(string path, bool needsNormalization = false)
        {
            var normalizedPath = needsNormalization
                ? path.NormalizePath(_rootPath)
                : path;

            return NestedTarRegexes.Any(regex => regex.IsMatch(normalizedPath));
        }
    }
}