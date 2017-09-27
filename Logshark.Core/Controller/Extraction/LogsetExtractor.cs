using ICSharpCode.SharpZipLib.Zip;
using log4net;
using Logshark.Common.Extensions;
using Logshark.Common.Helpers;
using Logshark.Core.Exceptions;
using Logshark.Core.Helpers;
using Logshark.RequestModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Logshark.Core.Controller.Extraction
{
    /// <summary>
    /// Handles program-specific logic for extracting log sets & managing logset locations.
    /// </summary>
    public class LogsetExtractor
    {
        protected LogsharkRequest request;

        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private ISet<Regex> WhitelistPatterns;

        public LogsetExtractor(LogsharkRequest request, ISet<Regex> whitelistPatterns)
        {
            this.request = request;
            this.WhitelistPatterns = whitelistPatterns;
        }

        #region Public Methods

        /// <summary>
        /// Process extraction of the request.
        /// </summary>
        public void Process()
        {
            Log.InfoFormat("Preparing logset target '{0}' for processing..", request.Target);

            // If target is a directory and/or exists on a remote drive, make a local copy to avoid
            // destructive operations on the original & possibly improve extraction speed.
            if (request.Target.IsDirectory || !PathHelper.ExistsOnLocalDrive(request.Target))
            {
                try
                {
                    request.Target.Target = CopyTargetLocally(request, WhitelistPatterns);
                }
                catch (InsufficientDiskSpaceException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    throw new ExtractionException(String.Format("Failed to copy target '{0}' to local temp directory: {1}", request.Target, ex.Message), ex);
                }
            }

            // Unpack files.
            try
            {
                request.RunContext.RootLogDirectory = UnpackLogset();
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

        /// <summary>
        /// Removes all files and subdirectories from a particular run in the "temporary" unpack location.
        /// </summary>
        public static void CleanUpRun(string runId)
        {
            var tempRunDirectory = Path.Combine(GetUnpackTempDirectory(), runId);
            Log.DebugFormat("Deleting temp directory {0}..", tempRunDirectory);

            try
            {
                DirectoryHelper.DeleteDirectory(tempRunDirectory);
            }
            catch (Exception ex)
            {
                // Log & swallow any exceptions -- cleanup is a nice-to-have feature and is not vital.
                Log.ErrorFormat("Failed to clean up logset left over from previous run: {0}", ex.Message);
            }
        }

        /// <summary>
        /// Cleans up all contents of the temp unpack directory.
        /// </summary>
        public static void CleanUpAll()
        {
            var tempDirectory = GetUnpackTempDirectory();

            try
            {
                DirectoryHelper.DeleteDirectory(tempDirectory);
            }
            catch (Exception ex)
            {
                // Log & swallow any exceptions -- cleanup is a nice-to-have feature and is not vital.
                Log.ErrorFormat("Failed to clean up logsets left over from previous run: {0}", ex.Message);
            }
        }

        #endregion Public Methods

        #region Protected Methods

        /// <summary>
        /// Extracts a log archive to a local temporary directory.  Collects timing data & sets the root log location.
        /// </summary>
        /// <returns>Location where archive was unpacked.</returns>
        protected string UnpackLogset()
        {
            string rootUnpackDirectory = Path.Combine(GetUnpackTempDirectory(), request.RunId);

            ICollection<string> archivesToUnpack = GetArchivesToUnpack(rootUnpackDirectory);

            if (!archivesToUnpack.Any())
            {
                request.Target.ProcessedSize = DiskSpaceHelper.GetDirectorySize(rootUnpackDirectory);
                return rootUnpackDirectory;
            }

            var unpackTimer = request.RunContext.CreateTimer("Unpack Logset");
            try
            {
                UnpackArchives(archivesToUnpack, rootUnpackDirectory);
            }
            finally
            {
                unpackTimer.Stop();
            }

            request.Target.ProcessedSize = DiskSpaceHelper.GetDirectorySize(rootUnpackDirectory);
            Log.InfoFormat("Finished extracting required files from logset! Unpacked {0} out of {1}. [{2}]", request.Target.ProcessedSize.Value.ToPrettySize(), request.Target.UncompressedSize.ToPrettySize(), unpackTimer.Elapsed.Print());

            return rootUnpackDirectory;
        }

        /// <summary>
        /// Returns a list of archives that need to be unpacked.
        /// </summary>
        protected ICollection<string> GetArchivesToUnpack(string unpackDirectory)
        {
            List<string> archivesToUnpack = new List<string>();
            if (request.Target.IsDirectory)
            {
                IEnumerable<string> nestedArchivePaths = Directory.GetFiles(unpackDirectory).Where(PathHelper.IsArchive);
                archivesToUnpack.AddRange(nestedArchivePaths);
            }
            else if (request.Target.IsFile)
            {
                archivesToUnpack.Add(request.Target);
            }

            return archivesToUnpack;
        }

        /// <summary>
        /// Unpacks a set of archives into a given directory.
        /// </summary>
        protected void UnpackArchives(IEnumerable<string> archivesToUnpack, string unpackDirectory)
        {
            foreach (string archiveToUnpack in archivesToUnpack)
            {
                if (request.Target.IsDirectory)
                {
                    // When the target is a nested archive, we need to unpack it to a subdirectory.
                    unpackDirectory = Path.Combine(unpackDirectory, Path.GetFileNameWithoutExtension(archiveToUnpack));
                }

                // Extract archive.
                var unzipStrategy = new UnzipStrategy(WhitelistPatterns, unzipNestedArchives: true);

                var unzipper = new LogsetUnzipper(unzipStrategy, request);
                UnzipResult result = unzipper.Unzip(archiveToUnpack, unpackDirectory, deleteOnFinish: request.Target.IsDirectory);

                // Update target size to include the size of the zip contents (but not the zip itself if it's a nested zip in a directory).
                request.Target.UncompressedSize += result.FullUncompressedSize;
                if (request.Target.IsDirectory)
                {
                    request.Target.UncompressedSize -= result.CompressedSize;
                }
            };
        }

        /// <summary>
        /// Copies either an archive or a folder to the local temp directory.
        /// </summary>
        /// <param name="request">The Logshark request containing a target.</param>
        /// <returns>Path where target was copied.</returns>
        protected static string CopyTargetLocally(LogsharkRequest request, ISet<Regex> whitelistPatterns)
        {
            string destination = Path.Combine(GetUnpackTempDirectory(), request.RunId);

            var copyTimer = request.RunContext.CreateTimer("Copy Logset Locally");
            Log.Info("Copying target to local temp directory..");

            ValidateSufficientDiskSpaceToCopyTargetLocally(request, whitelistPatterns);

            // Copy target.
            if (request.Target.IsDirectory)
            {
                // Create all of the directories.
                foreach (string dirPath in Directory.GetDirectories(request.Target, "*", SearchOption.AllDirectories))
                {
                    Directory.CreateDirectory(dirPath.Replace(request.Target, destination));
                }

                // Copy all the files that match the whitelist pattern.
                var requiredFiles = GetRequiredFilesInDirectory(request, whitelistPatterns);
                foreach (string file in requiredFiles)
                {
                    File.Copy(file, file.Replace(request.Target, destination), true);
                }
            }
            else
            {
                if (!Directory.Exists(destination))
                {
                    Directory.CreateDirectory(destination);
                }
                destination = Path.Combine(destination, Path.GetFileName(request.Target));
                File.Copy(request.Target, destination, overwrite: true);
            }

            copyTimer.Stop();
            Log.InfoFormat("Finished copying logset. [{0}]", copyTimer.Elapsed.Print());

            return destination;
        }

        private static void ValidateSufficientDiskSpaceToCopyTargetLocally(LogsharkRequest request, ISet<Regex> whitelistPatterns)
        {
            long availableDiskSpace = DiskSpaceHelper.GetAvailableFreeSpace(GetUnpackTempDirectory());

            long requiredDiskSpace = 0;
            if (request.Target.IsDirectory)
            {
                foreach (string requiredFile in GetRequiredFilesInDirectory(request, whitelistPatterns))
                {
                    requiredDiskSpace += DiskSpaceHelper.GetSize(requiredFile);
                }
            }
            else if (request.Target.IsFile)
            {
                requiredDiskSpace = DiskSpaceHelper.GetSize(request.Target);
            }

            if (requiredDiskSpace > availableDiskSpace)
            {
                throw new InsufficientDiskSpaceException(String.Format("Failed to copy target '{0}' to local temp directory: Not enough free disk space available! ({1} available, {2} required)",
                                                                        request.Target, availableDiskSpace.ToPrettySize(), requiredDiskSpace.ToPrettySize()));
            }
        }

        /// <summary>
        /// Retrieves the list of files in the target directory which are required to process this request.
        /// </summary>
        protected static IEnumerable<string> GetRequiredFilesInDirectory(LogsharkRequest request, ISet<Regex> whitelistPatterns)
        {
            IEnumerable<string> allFiles = Directory.GetFiles(request.Target, "*", SearchOption.AllDirectories);

            return allFiles.Where(file => FileIsWhitelisted(file, whitelistPatterns));
        }

        protected static bool FileIsWhitelisted(string file, ISet<Regex> whitelistPatterns)
        {
            foreach (var whitelist in whitelistPatterns)
            {
                if (whitelist.IsMatch(Path.GetFileName(file)))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Retrieve the application temp directory.
        /// </summary>
        /// <returns>Path to temp directory.</returns>
        protected static string GetUnpackTempDirectory()
        {
            return Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Temp");
        }

        #endregion Protected Methods
    }
}