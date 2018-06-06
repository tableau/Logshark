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

namespace Logshark.Core.Controller.Initialization.Archive
{
    internal class LogsetCopier
    {
        protected readonly ISet<Regex> fileWhitelistPatterns;

        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public LogsetCopier(ISet<Regex> fileWhitelistPatterns)
        {
            this.fileWhitelistPatterns = fileWhitelistPatterns;
        }

        #region Public Methods

        public string CopyLogset(string target, string destination)
        {
            if (!PathHelper.IsPathToExistingResource(target))
            {
                throw new ArgumentException(String.Format("Failed to copy target '{0}': path does not exist!", target));
            }

            try
            {
                using (var copyTimer = new LogsharkTimer("Copy Logset Locally", GlobalEventTimingData.Add))
                {
                    string copyPath = CopyTarget(target, destination);

                    Log.InfoFormat("Finished copying logset. [{0}]", copyTimer.Elapsed.Print());
                    return copyPath;
                }
            }
            catch (InsufficientDiskSpaceException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new LogsetCopyException(String.Format("Failed to copy target '{0}' to local temp directory: {1}", target, ex.Message), ex);
            }
        }

        #endregion Public Methods

        #region Protected Methods

        protected string CopyTarget(string target, string destination)
        {
            Log.InfoFormat("Copying target logset '{0}' to '{1}'..", target, destination);

            if (PathHelper.IsDirectory(target))
            {
                return CopyDirectory(target, destination);
            }

            return CopyFile(target, destination);
        }

        protected string CopyDirectory(string target, string destination)
        {
            ValidateSufficientDiskSpaceToCopyTarget(target, destination);

            // Create all of the directories.
            foreach (string dirPath in Directory.GetDirectories(target, "*", SearchOption.AllDirectories))
            {
                Directory.CreateDirectory(dirPath.Replace(target, destination));
            }

            // Copy all the files that match the whitelist pattern.
            var requiredFiles = GetWhitelistedFilesInDirectory(target);
            foreach (string file in requiredFiles)
            {
                File.Copy(file, file.Replace(target, destination), true);
            }

            return destination;
        }

        protected string CopyFile(string target, string destination)
        {
            ValidateSufficientDiskSpaceToCopyTarget(target, destination);

            if (!Directory.Exists(destination))
            {
                Directory.CreateDirectory(destination);
            }

            string destinationFile = Path.Combine(destination, Path.GetFileName(target));
            File.Copy(target, destinationFile, overwrite: true);

            return destinationFile;
        }

        protected void ValidateSufficientDiskSpaceToCopyTarget(string target, string destination)
        {
            long availableDiskSpace = DiskSpaceHelper.GetAvailableFreeSpace(destination);
            long requiredDiskSpace = GetRequiredDiskSpace(target);

            if (requiredDiskSpace > availableDiskSpace)
            {
                throw new InsufficientDiskSpaceException(String.Format("Failed to copy target '{0}' to local temp directory: Not enough free disk space available! ({1} available, {2} required)",
                                                                        target, availableDiskSpace.ToPrettySize(), requiredDiskSpace.ToPrettySize()));
            }
        }

        protected long GetRequiredDiskSpace(string target)
        {
            if (PathHelper.IsDirectory(target))
            {
                return GetWhitelistedFilesInDirectory(target).Sum(file => DiskSpaceHelper.GetSize(file));
            }

            return DiskSpaceHelper.GetSize(target);
        }

        /// <summary>
        /// Retrieves the list of files in the given directory which match the set of whitelisted file patterns.
        /// </summary>
        protected IEnumerable<string> GetWhitelistedFilesInDirectory(string targetDirectory)
        {
            return Directory.GetFiles(targetDirectory, "*", SearchOption.AllDirectories)
                            .Where(IsFileWhitelisted);
        }

        protected bool IsFileWhitelisted(string file)
        {
            return fileWhitelistPatterns.Any(whitelistPattern => whitelistPattern.IsMatch(Path.GetFileName(file)));
        }

        #endregion Protected Methods
    }
}