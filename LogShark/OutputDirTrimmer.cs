using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace LogShark
{
    public static class OutputDirTrimmer
    {
        public static void TrimOldResults(string outputDir, int maxResultsAllowed, ILogger logger)
        {
            if (maxResultsAllowed <= 0)
            {
                logger.LogInformation("Skipping removing old results from output dir, as max results set to {maxResultsAllowedInOutput}", maxResultsAllowed);
                return;
            }

            var dirs = Directory.Exists(outputDir)
                ? Directory.GetDirectories(outputDir, "*", SearchOption.TopDirectoryOnly)
                : new string[0];

            var dirsToLeave = maxResultsAllowed - 1; // -1 for the current run
            if (dirs.Length <= dirsToLeave)
            {
                logger.LogInformation("Including spot for the current run, output dir has less than {maxResultsAllowedInOutput} results. No need to remove anything", maxResultsAllowed);
            }

            var dirsToRemove = dirs
                .Select(dirPath => new DirectoryInfo(dirPath))
                .OrderByDescending(dirInfo => dirInfo.CreationTime)
                .Skip(dirsToLeave)
                .ToList();

            foreach (var directoryInfo in dirsToRemove)
            {
                logger.LogInformation("Deleting `{removedOutputDir}`...", directoryInfo.FullName);
                directoryInfo.Delete(true);
            }
            
            logger.LogInformation("Deleted {removedOutputDirCount} oldest run result(s) from `{outputDir}`", dirsToRemove.Count, outputDir);
        }
    }
}