using System;
using System.IO;
using Microsoft.Extensions.Logging;

namespace LogShark.Writers.Shared
{
    public static class OutputDirInitializer
    {
        public const string OutputDirAlreadyExistsMessageTail = "Stopping LogShark to prevent damage to existing results. If you would like to append to existing results, use `--append-to` option instead"; 
            
        public static (string OutputDir, string WorkbooksDir) InitDirs(
            string outputDirectory,
            string runId,
            string appendToRunId,
            string writerType,
            ILoggerFactory loggerFactory,
            bool throwIfOutputDirExists)
        {
            var logger = loggerFactory.CreateLogger<IWriterFactory>();
            
            var outputDir = Path.Combine(outputDirectory, runId);
            if (Directory.Exists(outputDir) && throwIfOutputDirExists)
            {
                throw new ArgumentException($"Output dir already exists for Run ID `{runId}`! " + OutputDirAlreadyExistsMessageTail);
            }
            
            var dataOutputDir = Path.Combine(outputDirectory, runId, writerType);
            var workbooksDir = Path.Combine(outputDirectory, runId, "workbooks");

            Directory.CreateDirectory(dataOutputDir);
            Directory.CreateDirectory(workbooksDir);

            var appending = !string.IsNullOrWhiteSpace(appendToRunId);
            if (appending)
            {
                logger.LogInformation("Copying results from Run Id {appendToRunId} into new output directory `{outputDir}`", appendToRunId, dataOutputDir);
                var previousOutputDir = Path.Combine(outputDirectory, appendToRunId, writerType);
                var numberOfFilesCopied = CopyPreviousResults(previousOutputDir, dataOutputDir, logger);
                logger.LogInformation("Copied {numberOfPreviousResultsCopied} files into output directory", numberOfFilesCopied);
            }
            return (dataOutputDir, workbooksDir);
        }

        private static int CopyPreviousResults(string previousResultsDir, string newOutputDir, ILogger logger)
        {
            if (!Directory.Exists(previousResultsDir))
            {
                throw new ArgumentException($"Directory {previousResultsDir} does not exist. Unable to append to the provided Run Id. " +
                                            $"Make sure that Run Id specified correctly and its results are still present in the output directory");
            }

            var files = Directory.GetFiles(previousResultsDir, "*", SearchOption.TopDirectoryOnly);

            foreach (var file in files)
            {
                logger.LogDebug("Copying file `{previousResultFileCopied}`", file);
                var fileInfo = new FileInfo(file);
                var destinationFileName = Path.Combine(newOutputDir, fileInfo.Name);
                fileInfo.CopyTo(destinationFileName);
            }

            return files.Length;
        }
    }
}