using log4net;
using LogParsers;
using Logshark.Controller.Parsing.Partitioning;
using Logshark.Exceptions;
using Logshark.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using LogParsers.Helpers;

namespace Logshark.Controller.Parsing
{
    /// <summary>
    /// Processes a root log directory and queues up supported logs for processing.
    /// Also partitions files which exceed the configured size threshold.
    /// </summary>
    internal class LogsetPreprocessor
    {
        protected LogsharkRequest request;
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public LogsetPreprocessor(LogsharkRequest request)
        {
            this.request = request;
        }

        /// <summary>
        /// Preprocesses the logset associated with the member LogsharkRequest.
        /// Partitions any partitionable files that exceed the configured size threshold.
        /// </summary>
        /// <returns>Queue of preprocessed log files.</returns>
        public Queue<LogFileContext> Preprocess()
        {
            // Sanity check.
            if (!Directory.Exists(request.RunContext.RootLogDirectory))
            {
                throw new ArgumentException(String.Format("{0} is not a valid directory.", request.RunContext.RootLogDirectory));
            }

            // Set the logset type and validate that further work should be done.
            SetLogsetType(request);

            if (request.RunContext.LogsetType == LogsetType.Desktop)
            {
                MoveDesktopLogsToSubdirectory();
            }

            // Gather a list of log files required to run the requested plugins for that type.
            IEnumerable<LogFileContext> requiredLogs = LoadRequiredLogs();

            // Split large files into chunks for faster parallel processing.
            var partitioner = new ConcurrentFilePartitioner(request);
            var filesToProcess = partitioner.PartitionLargeFiles(requiredLogs);

            return new Queue<LogFileContext>(filesToProcess.OrderByDescending(file => file.FileSize));
        }

        /// <summary>
        /// Loads of all the logs required for this request.
        /// </summary>
        /// <returns>Log contexts for all logs required for request.</returns>
        public IEnumerable<LogFileContext> LoadRequiredLogs()
        {
            var logsToProcess = new List<LogFileContext>();

            // Filter down to only supported files.
            var supportedFiles = DirectoryHelper.GetSupportedFiles(request.RunContext.RootLogDirectory);

            // Filter supported files to keep only what we need to populate the required collections.
            foreach (var supportedFile in supportedFiles)
            {
                var parserFactory = new ParserFactory(request.RunContext.RootLogDirectory);
                var parser = parserFactory.GetParser(supportedFile.FullName);
                string collectionName = parser.CollectionSchema.CollectionName.ToLowerInvariant();

                if (LogsetDependencyHelper.IsCollectionRequiredForRequest(collectionName, request))
                {
                    logsToProcess.Add(new LogFileContext(supportedFile.FullName, request.RunContext.RootLogDirectory));
                }
            }

            return logsToProcess;
        }

        protected void SetLogsetType(LogsharkRequest request)
        {
            request.RunContext.LogsetType = LogsetTypeHelper.GetLogsetType(request.RunContext.RootLogDirectory);

            if (request.RunContext.LogsetType == LogsetType.Unknown)
            {
                request.RunContext.IsValidLogset = false;
                throw new InvalidLogsetException("Target does not appear to be a valid Tableau logset!");
            }

            request.RunContext.IsValidLogset = true;
            Log.InfoFormat("Target is a Tableau {0} logset.", request.RunContext.LogsetType);
        }

        protected void MoveDesktopLogsToSubdirectory()
        {
            // Construct list of all subdirectories.
            IEnumerable<string> allDirectories = Directory.GetDirectories(request.RunContext.RootLogDirectory, "*", SearchOption.TopDirectoryOnly).ToList();

            // Create new desktop subdirectory.
            string desktopDirectoryLocation = Path.Combine(request.RunContext.RootLogDirectory, "desktop");
            if (!Directory.Exists(desktopDirectoryLocation))
            {
                Directory.CreateDirectory(desktopDirectoryLocation);
            }

            // Move all of the subdirectories.
            foreach (string directory in allDirectories)
            {
                Directory.Move(directory, directory.Replace(request.RunContext.RootLogDirectory, desktopDirectoryLocation));
            }

            // Move all root files into the new location.
            IEnumerable<string> allFiles = Directory.GetFiles(request.RunContext.RootLogDirectory, "*", SearchOption.TopDirectoryOnly);
            foreach (string file in allFiles)
            {
                string destinationPath = file.Replace(request.RunContext.RootLogDirectory, desktopDirectoryLocation);
                File.Move(file, destinationPath);
            }
        }
    }
}