using System.IO;
using System.Linq;

namespace Logshark.Helpers
{
    internal static class DiskSpaceHelper
    {
        /// <summary>
        /// Get the size of a file or directory.
        /// </summary>
        /// <param name="path">The path to the file or directory.</param>
        /// <returns>Size of file or directory, in bytes.</returns>
        public static long GetSize(string path)
        {
            if (PathHelper.IsDirectory(path))
            {
                return GetDirectorySize(path);
            }

            return GetFileSize(path);
        }

        /// <summary>
        /// Get the size of a directory.
        /// </summary>
        /// <param name="directoryPath">Absolute path to the directory.</param>
        /// <returns>Size of directory, in bytes.</returns>
        public static long GetDirectorySize(string directoryPath)
        {
            return new DirectoryInfo(directoryPath).GetFiles("*", SearchOption.AllDirectories).Sum(file => file.Length);
        }

        /// <summary>
        /// Get the size of a file.
        /// </summary>
        /// <param name="filePath">Absolute path to the file.</param>
        /// <returns>Size of file, in bytes.</returns>
        public static long GetFileSize(string filePath)
        {
            var fileInfo = new FileInfo(filePath);
            return fileInfo.Length;
        }

        /// <summary>
        /// Get the available disk space for a path.
        /// </summary>
        /// <param name="directoryPath">The path to a valid directory.</param>
        /// <returns>Available disk space, in bytes.</returns>
        public static long GetAvailableFreeSpace(string directoryPath)
        {
            DriveInfo volumeName = new DriveInfo(DirectoryHelper.GetVolumeName(directoryPath));

            return GetAvailableFreeSpace(volumeName);
        }

        /// <summary>
        /// Get the available disk space for a drive.
        /// </summary>
        /// <param name="driveInfo">The target volume to check.</param>
        /// <returns>Available disk space, in bytes.</returns>
        public static long GetAvailableFreeSpace(DriveInfo driveInfo)
        {
            return driveInfo.AvailableFreeSpace;
        }
    }
}