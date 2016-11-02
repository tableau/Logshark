using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using log4net;
using Logshark.Exceptions;
using Logshark.Extensions;
using Logshark.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Logshark.Controller.Extraction
{
    /// <summary>
    /// Helper class that defines unzip parameters.
    /// </summary>
    internal sealed class UnzipStrategy
    {
        public bool UnzipNestedArchives { get; set; }
        public bool PreserveDirectoryStructure { get; set; }
        public Regex WhitelistPattern { get; set; }
        public IList<Regex> BlacklistPatterns { get; set; }

        public UnzipStrategy(bool unzipNestedArchives = false, bool preserveDirectoryStructure = true, Regex whitelistPattern = null, IList<Regex> blacklistPatterns = null)
        {
            UnzipNestedArchives = unzipNestedArchives;
            PreserveDirectoryStructure = preserveDirectoryStructure;
            WhitelistPattern = whitelistPattern ?? new Regex("^.*");
            BlacklistPatterns = blacklistPatterns ?? new List<Regex>();
        }
    }

    /// <summary>
    /// Helper class for tallying unzip result metrics.
    /// </summary>
    internal class UnzipResult
    {
        public long CompressedSize { get; set; }
        public long FullUncompressedSize { get; set; }
        public long FullUncompressedFileCount { get; set; }
        public long ExtractedSize { get; set; }
        public long ExtractedFileCount { get; set; }
    }

    /// <summary>
    /// Handles extraction of .zip archives.
    /// </summary>
    internal class Unzipper
    {
        private UnzipStrategy Strategy { get; set; }
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public Unzipper(UnzipStrategy strategy)
        {
            Strategy = strategy;
        }

        #region Public Methods

        /// <summary>
        /// Extracts a target archive to a target directory.
        /// </summary>
        /// <param name="archive">The path to the archive.</param>
        /// <param name="destinationDirectory">The directory to extract the archive to.</param>
        /// <param name="deleteOnFinish">Indicates whether this archive should be deleted after unzipping it.</param>
        public UnzipResult Unzip(string archive, string destinationDirectory, bool deleteOnFinish = false)
        {
            // Validate archive exists.
            if (!File.Exists(archive))
            {
                throw new ArgumentException(String.Format("Archive '{0}' does not exist!", archive));
            }

            Directory.CreateDirectory(destinationDirectory);
            ValidateSufficientDiskSpaceToUnpack(archive, destinationDirectory);

            Log.InfoFormat("Extracting contents of archive '{0}' to '{1}'.. ({2})", Path.GetFileName(archive), destinationDirectory, DiskSpaceHelper.GetSize(archive).ToPrettySize(decimalPlaces: 1));
            return ExtractArchive(archive, destinationDirectory, deleteOnFinish);
        }

        #endregion Public Methods

        #region Protected Methods

        /// <summary>
        /// Retrieves the disk space necessary to unpack all of the required files out of an archive.
        /// </summary>
        protected long GetRequiredUnpackedSize(string archive, string destinationDirectory)
        {
            ZipFile zipFile = null;
            try
            {
                FileStream archiveFileStream = File.OpenRead(archive);
                zipFile = new ZipFile(archiveFileStream);

                long requiredSize = 0;
                foreach (ZipEntry zipEntry in zipFile)
                {
                    if (QualifiesForExtraction(zipEntry, destinationDirectory))
                    {
                        requiredSize += zipEntry.Size;
                    }
                }

                return requiredSize;
            }
            finally
            {
                if (zipFile != null)
                {
                    zipFile.IsStreamOwner = true;
                    zipFile.Close();
                }
            }
        }

        /// <summary>
        /// Extracts an archive to a target destination directory.
        /// </summary>
        protected UnzipResult ExtractArchive(string archive, string destinationDirectory, bool deleteOnFinish = false)
        {
            UnzipResult result;
            using (FileStream fs = File.OpenRead(archive))
            {
                result = ExtractZip(fs, Path.GetFileName(archive), destinationDirectory);
            }

            if (deleteOnFinish)
            {
                File.Delete(archive);
            }

            return result;
        }

        /// <summary>
        /// Extracts a .zip archive and tallies the work done.
        /// </summary>
        protected UnzipResult ExtractZip(Stream zipStream, string zipName, string destinationDirectory)
        {
            ZipFile zipFile = new ZipFile(zipStream) { IsStreamOwner = true };
            try
            {
                UnzipResult result = ExtractZipContents(zipFile, zipName, destinationDirectory);
                result.CompressedSize = zipFile.Cast<ZipEntry>().Sum(zipEntry => zipEntry.CompressedSize);

                Log.InfoFormat("Extracted {0} files from '{1}'. ({2} unpacked)", result.ExtractedFileCount, zipName, result.ExtractedSize.ToPrettySize(decimalPlaces: 1));

                return result;
            }
            finally
            {
                zipFile.Close();
            }
        }

        /// <summary>
        /// Extract the contents of a zip to a destination directory.
        /// </summary>
        protected UnzipResult ExtractZipContents(ZipFile zipFile, string zipName, string destinationDirectory)
        {
            UnzipResult result = new UnzipResult();
            foreach (ZipEntry zipEntry in zipFile)
            {
                if (zipEntry.IsFile)
                {
                    result.FullUncompressedFileCount++;
                    result.FullUncompressedSize += zipEntry.Size;
                }

                if (QualifiesForExtraction(zipEntry, destinationDirectory))
                {
                    bool isItemAnArchive = IsSupportedArchiveType(zipEntry);
                    if (isItemAnArchive)
                    {
                        Log.InfoFormat("Extracting nested archive '{0}' from '{1}'.. ({2})", Path.GetFileName(zipEntry.Name),
                                        zipName, zipEntry.CompressedSize.ToPrettySize(decimalPlaces: 1));
                    }

                    string extractedItemLocation = ExtractFileFromZip(zipFile, zipEntry, destinationDirectory);
                    result.ExtractedFileCount++;
                    result.ExtractedSize += zipEntry.Size;

                    if (isItemAnArchive && Strategy.UnzipNestedArchives)
                    {
                        string destinationSubDirectory = Path.Combine(destinationDirectory, Path.GetFileNameWithoutExtension(zipEntry.Name));
                        UnzipResult nestedResult = ExtractArchive(extractedItemLocation, destinationSubDirectory, deleteOnFinish: true);

                        // Combine tallied metrics with those of nested result.
                        result.FullUncompressedFileCount += nestedResult.FullUncompressedFileCount - 1;
                        result.FullUncompressedSize += nestedResult.FullUncompressedSize - zipEntry.Size;
                        result.ExtractedFileCount += nestedResult.ExtractedFileCount - 1;
                        result.ExtractedSize += nestedResult.ExtractedSize - zipEntry.Size;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Extracts a single embedded file from a ZIP archive.
        /// </summary>
        protected string ExtractFileFromZip(ZipFile zipFile, ZipEntry zipEntry, string destinationDirectory)
        {
            string destinationFilePath = Path.Combine(destinationDirectory, zipEntry.Name);
            string directoryName = Path.GetDirectoryName(destinationFilePath);
            if (!String.IsNullOrWhiteSpace(directoryName))
            {
                Directory.CreateDirectory(directoryName);
            }

            using (Stream zipStream = zipFile.GetInputStream(zipEntry))
            {
                using (FileStream streamWriter = File.Create(destinationFilePath))
                {
                    byte[] buffer = new byte[LogsharkConstants.EXTRACTION_STREAM_BUFFER_SIZE];
                    StreamUtils.Copy(zipStream, streamWriter, buffer);
                }
            }

            // Preserve original last modified time.
            File.SetLastWriteTime(destinationFilePath, zipEntry.DateTime);

            return destinationFilePath;
        }

        /// <summary>
        /// Indicates whether a given file is an archive or not.
        /// </summary>
        protected static bool IsSupportedArchiveType(ZipEntry zipEntry)
        {
            var fileExtension = Path.GetExtension(zipEntry.Name);
            if (fileExtension != null && fileExtension.Equals(".zip", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Indicates whether a path contains any invalid characters.
        /// </summary>
        protected static bool ContainsInvalidPathCharacters(string pathName)
        {
            return pathName.IndexOfAny(Path.GetInvalidPathChars()) >= 0;
        }

        /// <summary>
        /// Indicates whether an archive item should be extracted or not.
        /// </summary>
        protected virtual bool QualifiesForExtraction(ZipEntry zipEntry, string destinationDirectory)
        {
            // Skip over directories.
            if (zipEntry.IsDirectory)
            {
                return false;
            }

            // Check filename against whitelist filter.
            if (!Strategy.WhitelistPattern.IsMatch(zipEntry.Name))
            {
                return false;
            }

            // Check filename against blacklist patterns.
            foreach (var blacklistPattern in Strategy.BlacklistPatterns)
            {
                if (blacklistPattern.IsMatch(zipEntry.Name))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Validates that sufficient disk space exists to unpack a target archive. Throws exception if space is insufficient.
        /// </summary>
        protected void ValidateSufficientDiskSpaceToUnpack(string archive, string destinationDirectory)
        {
            long availableBytes = DiskSpaceHelper.GetAvailableFreeSpace(destinationDirectory);
            long requiredBytes = GetRequiredUnpackedSize(archive, destinationDirectory);
            if (availableBytes < requiredBytes)
            {
                throw new InsufficientDiskSpaceException(String.Format("Not enough free disk space available to unpack '{0}'! ({1} available, {2} required)",
                                                                        Path.GetFileName(archive), availableBytes.ToPrettySize(), requiredBytes.ToPrettySize()));
            }
        }

        #endregion Protected Methods
    }
}