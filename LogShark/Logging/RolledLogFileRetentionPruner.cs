using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace LogShark.Logging
{
    public static class RolledLogFileRetentionPruner
    {
        public static void PruneOldRolledFiles(string directory, string baseFileNameWithoutExtension, string extension, int retainedFileCountLimit)
        {
            if (retainedFileCountLimit <= 0 || !Directory.Exists(directory))
            {
                return;
            }

            // Anchored so a run whose base name happens to be a string-prefix of another run's (e.g.
            // "server" vs "server_v2") can't match and prune that other run's still-open files.
            var fileNamePattern = new Regex($"^{Regex.Escape(baseFileNameWithoutExtension)}(_\\d+)?{Regex.Escape(extension)}$");

            var filesToDelete = Directory.GetFiles(directory)
                .Where(path => fileNamePattern.IsMatch(Path.GetFileName(path)))
                .Select(path => new FileInfo(path))
                .OrderByDescending(file => file.CreationTimeUtc)
                .Skip(retainedFileCountLimit);

            foreach (var file in filesToDelete)
            {
                try
                {
                    file.Delete();
                }
                catch (IOException ex)
                {
                    // Deliberately writes to stderr instead of routing through ILogger: this runs from inside
                    // RollingFileLoggerProvider's own roll, so logging through the shared ILoggerFactory would
                    // dispatch right back into the log file this method is pruning around.
                    Console.Error.WriteLine($"LogShark: failed to delete old rolled log file '{file.FullName}': {ex.Message}");
                }
            }
        }
    }
}
