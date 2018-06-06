using ICSharpCode.SharpZipLib.Zip;
using log4net;
using Logshark.Common.Extensions;
using Logshark.Common.Helpers;
using Logshark.Core.Exceptions;
using Logshark.Core.Helpers.Timers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Logshark.Core.Controller.Initialization.Archive.Extraction
{
    /// <summary>
    /// Handles program-specific logic for extracting logsets.
    /// </summary>
    internal class LogsetExtractor
    {
        protected readonly ISet<Regex> fileWhitelistPatterns;

        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public LogsetExtractor(ISet<Regex> fileWhitelistPatterns)
        {
            this.fileWhitelistPatterns = fileWhitelistPatterns;
        }

        #region Public Methods

        /// <summary>
        /// Extracts the target logset.
        /// </summary>
        /// <returns>The root path where files were extracted.</returns>
        public ExtractionResult Extract(string target, string destination)
        {
            // Unpack files.
            try
            {
                using (var unpackTimer = new LogsharkTimer("Unpack Archives", GlobalEventTimingData.Add))
                {
                    ICollection<string> archivesToUnpack = GetArchivesToUnpack(target, destination);
                    var unpackResults = UnpackArchives(archivesToUnpack, destination, PathHelper.IsDirectory(target));

                    if (unpackResults.Any())
                    {
                        long inputSize = DiskSpaceHelper.GetSize(target);
                        long extractedSize = DiskSpaceHelper.GetDirectorySize(destination);
                        Log.InfoFormat("Finished extracting required files from logset! Unpacked {0} out of {1}. [{2}]", extractedSize.ToPrettySize(), inputSize.ToPrettySize(), unpackTimer.Elapsed.Print());
                    }

                    return new ExtractionResult(destination, unpackResults);
                }
            }
            catch (ZipException ex)
            {
                throw new InvalidLogsetException(String.Format("Cannot read logset archive: {0}", ex.Message));
            }
            catch (InsufficientDiskSpaceException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new ExtractionException(ex.Message, ex);
            }
        }

        #endregion Public Methods

        #region Protected Methods

        /// <summary>
        /// Returns a list of archives that need to be unpacked.
        /// </summary>
        protected ICollection<string> GetArchivesToUnpack(string target, string destination)
        {
            var archivesToUnpack = new List<string>();

            if (PathHelper.IsDirectory(target))
            {
                IEnumerable<string> nestedArchivePaths = Directory.GetFiles(destination).Where(PathHelper.IsArchive);
                archivesToUnpack.AddRange(nestedArchivePaths);
            }
            else if (PathHelper.IsArchive(target))
            {
                archivesToUnpack.Add(target);
            }

            return archivesToUnpack;
        }

        /// <summary>
        /// Unpacks a set of archives into a given directory.
        /// </summary>
        protected ICollection<UnzipResult> UnpackArchives(IEnumerable<string> archivesToUnpack, string unpackDirectory, bool isTargetDirectory)
        {
            var results = new List<UnzipResult>();

            foreach (string archiveToUnpack in archivesToUnpack)
            {
                UnzipResult result = UnpackArchive(archiveToUnpack, unpackDirectory, isTargetDirectory);
                results.Add(result);
            }

            return results;
        }

        /// <summary>
        /// Unpacks an archive into a given directory.
        /// </summary>
        protected UnzipResult UnpackArchive(string archiveToUnpack, string unpackDirectory, bool isTargetDirectory)
        {
            if (isTargetDirectory)
            {
                // When the target is a directory, we need to unpack any inner archives to a subdirectory.
                unpackDirectory = Path.Combine(unpackDirectory, Path.GetFileNameWithoutExtension(archiveToUnpack));
            }

            var unzipStrategy = new UnzipStrategy(fileWhitelistPatterns, unzipNestedArchives: true);

            var unzipper = new LogsetUnzipper(unzipStrategy);
            return unzipper.Unzip(archiveToUnpack, unpackDirectory, deleteOnFinish: isTargetDirectory);
        }

        #endregion Protected Methods
    }
}