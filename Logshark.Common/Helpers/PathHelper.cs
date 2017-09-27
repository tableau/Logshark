using System;
using System.IO;
using System.Linq;

namespace Logshark.Common.Helpers
{
    /// <summary>
    /// Helper methods for fetching & manipulating paths.
    /// </summary>
    public static class PathHelper
    {
        /// <summary>
        /// Check whether this path exists on a local disk or not.
        /// </summary>
        /// <param name="path">The path to a file or directory.</param>
        /// <returns>True if path refers to a local fixed volume.</returns>
        public static bool ExistsOnLocalDrive(string path)
        {
            var pathRoot = Path.GetPathRoot(Path.GetFullPath(path));
            var fixedDrives = DriveInfo.GetDrives()
                                        .Where(drive => drive.DriveType == DriveType.Fixed);

            return fixedDrives.Any(drive => pathRoot.StartsWith(drive.Name, StringComparison.InvariantCultureIgnoreCase));
        }

        /// <summary>
        /// Indicates whether a given path is a directory or not.
        /// </summary>
        /// <param name="path">The path to the item to analyze.</param>
        /// <returns>True if the item at path is a directory.</returns>
        public static bool IsDirectory(string path)
        {
            // Check if the target is a directory or a file.
            FileAttributes targetFileAttributes;
            try
            {
                targetFileAttributes = File.GetAttributes(path);
            }
            catch (Exception ex)
            {
                throw new ArgumentException(String.Format("Could not retrieve file attributes for object '{0}': {1}", path, ex.Message));
            }

            return targetFileAttributes.HasFlag(FileAttributes.Directory);
        }

        /// <summary>
        /// Indicates whether a given path is a valid absolute or relative path to an existing file or directory.
        /// </summary>
        /// <param name="path">An absolute or relative path.</param>
        /// <returns>True if path leads to an existing file or directory.</returns>
        public static bool IsPathToExistingResource(string path)
        {
            try
            {
                string absolutePath = GetAbsolutePath(path);
                return Directory.Exists(absolutePath) || File.Exists(absolutePath);
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Attempts to get the absolute path of a given path string.  This is a slight improvement over Path.GetFullPath()
        /// in that we take into account paths which may be relative to the current working directory.
        /// </summary>
        public static string GetAbsolutePath(string path)
        {
            // If this is a relative path, tack it on to the path of the CWD.
            if (!Path.IsPathRooted(path))
            {
                path = Path.Combine(Environment.CurrentDirectory, path);
            }

            return Path.GetFullPath(path);
        }

        /// <summary>
        /// Indicates whether a given path is to an archive.
        /// </summary>
        /// <param name="path">The path to the resource.</param>
        /// <returns>True if path is a zip archive.</returns>
        public static bool IsArchive(string path)
        {
            return path.EndsWith(".zip", StringComparison.InvariantCultureIgnoreCase);
        }
    }
}