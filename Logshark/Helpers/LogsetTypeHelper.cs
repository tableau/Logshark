using System.IO;

namespace Logshark.Helpers
{
    /// <summary>
    /// Helper class for determining what product type a logset is from.
    /// </summary>
    internal static class LogsetTypeHelper
    {
        /// <summary>
        /// Retrieves the type of logset for a given directory.
        /// </summary>
        /// <param name="rootLogDirectory">Absolute path to the root of a log directory.</param>
        /// <returns>Product type that the given directory is a logset for.</returns>
        public static LogsetType GetLogsetType(string rootLogDirectory)
        {
            if (IsDesktopLogSet(rootLogDirectory))
            {
                return LogsetType.Desktop;
            }
            else if (IsServerLogSet(rootLogDirectory))
            {
                return LogsetType.Server;
            }
            else
            {
                return LogsetType.Unknown;
            }
        }

        /// <summary>
        /// Indicates whether the given path appears to be a Tableau Desktop logset.
        /// </summary>
        private static bool IsDesktopLogSet(string rootLogDirectory)
        {
            bool hasTabsvcYmlFile = File.Exists(Path.Combine(rootLogDirectory, "tabsvc.yml"));

            // Given that these logs get zipped by hand usually we need to check either the root or the Logs subdirectory.
            bool hasLogTxtInRoot = File.Exists(Path.Combine(rootLogDirectory, "log.txt"));
            bool hasLogTxtInLogsSubdir = File.Exists(Path.Combine(rootLogDirectory, "Logs", "log.txt"));

            // If we don't have a tabsvc.yml file then we know it's not a server log.
            // If we have a log.txt then we know it's most likely a desktop log.
            return !hasTabsvcYmlFile && (hasLogTxtInRoot || hasLogTxtInLogsSubdir);
        }

        /// <summary>
        /// Indicates whether the given path appears to be a Tableau Server logset.
        /// </summary>
        private static bool IsServerLogSet(string rootLogDirectory)
        {
            bool hasBuildVersionFile = File.Exists(Path.Combine(rootLogDirectory, "buildversion.txt"));
            bool hasWorkgroupYmlFile = File.Exists(Path.Combine(rootLogDirectory, "config", "workgroup.yml"));

            return hasBuildVersionFile && hasWorkgroupYmlFile;
        }
    }
}