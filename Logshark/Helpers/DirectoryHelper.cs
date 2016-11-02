using LogParsers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Logshark.Helpers
{
    /// <summary>
    /// Helper methods for working with directories.
    /// </summary>
    public static class DirectoryHelper
    {
        /// <summary>
        /// Retrieves a list of FileInfo objects for all files within a given directory and any nested subdirectories.
        /// </summary>
        /// <param name="path">Path to a directory to traverse.</param>
        /// <returns>Collection of FileInfo objects for all files within the given directory.</returns>
        public static IEnumerable<FileInfo> GetAllFiles(string path)
        {
            // Process the list of files found in the directory.
            string[] fileEntries = Directory.GetFiles(path);
            List<FileInfo> files = fileEntries.Select(fileName => new FileInfo(fileName)).ToList();

            // Recurse into subdirectories of this directory.
            string[] subdirectoryEntries = Directory.GetDirectories(path);
            foreach (string subdirectory in subdirectoryEntries)
            {
                files.AddRange(GetAllFiles(subdirectory));
            }

            return files;
        }

        /// <summary>
        /// Get a list of all the supported files in a root log directory.
        /// </summary>
        /// <param name="rootLogDirectory">The root directory of a logset.</param>
        /// <returns>Collection of all supported files in the directory.</returns>
        public static IList<FileInfo> GetSupportedFiles(string rootLogDirectory)
        {
            IList<FileInfo> supportedFiles = new List<FileInfo>();
            var parserFactory = new ParserFactory(rootLogDirectory);
            foreach (FileInfo file in GetAllFiles(rootLogDirectory))
            {
                try
                {
                    if (parserFactory.IsSupported(file.FullName))
                    {
                        supportedFiles.Add(file);
                    }
                }
                // Just swallow any downstream exceptions for the sake of stability.
                catch (Exception) { }
            }
            return supportedFiles;
        }

        /// <summary>
        /// Deletes a directory.
        /// </summary>
        /// <param name="path">The path of the directory.</param>
        /// <param name="isRecursive">Specifies whether subdirectories should be recursively deleted.</param>
        public static void DeleteDirectory(string path, bool isRecursive = true)
        {
            // Bail out if location doesn't exist.
            if (!Directory.Exists(path))
            {
                return;
            }

            // Delete contents of location.
            var directory = new DirectoryInfo(path);
            directory.Delete(recursive: true);
        }

        /// <summary>
        /// Retrieves the volume name for a given path.
        /// </summary>
        /// <param name="directory">The path to a directory.</param>
        /// <returns>The volume name for the path.</returns>
        public static string GetVolumeName(string directory)
        {
            var directoryInfo = new DirectoryInfo(directory);
            var volumeName = directoryInfo.Root.FullName;

            return volumeName;
        }
    }
}