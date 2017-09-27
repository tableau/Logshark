using log4net;
using LogParsers.Base;
using LogParsers.Base.Helpers;
using Logshark.Common.Helpers;
using Logshark.Core.Controller.Parsing.Partitioning;
using Logshark.Core.Exceptions;
using Logshark.Core.Helpers;
using Logshark.RequestModel;
using Logshark.RequestModel.RunContext;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Logshark.Core.Controller.Parsing
{
    /// <summary>
    /// Processes a root log directory and queues up supported logs for processing.
    /// Also partitions files which exceed the configured size threshold.
    /// </summary>
    internal class LogsetPreprocessor
    {
        protected LogsharkRequest request;
        protected IParserFactory parserFactory;
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public LogsetPreprocessor(LogsharkRequest request, IParserFactory parserFactory)
        {
            this.request = request;
            this.parserFactory = parserFactory;
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

            // Gather a list of log files required to run the requested plugins for that type.
            IEnumerable<LogFileContext> requiredLogs = LoadRequiredLogs();

            // Split large files into chunks for faster parallel processing.
            var partitioner = new ConcurrentFilePartitioner(request, parserFactory);
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
            var supportedFiles = GetSupportedFiles(request.RunContext.RootLogDirectory);

            // Filter supported files to keep only what we need to populate the required collections.
            foreach (var supportedFile in supportedFiles)
            {
                var parser = parserFactory.GetParser(supportedFile.FullName);
                string collectionName = parser.CollectionSchema.CollectionName.ToLowerInvariant();

                if (LogsetDependencyHelper.IsCollectionRequiredForRequest(collectionName, request))
                {
                    logsToProcess.Add(new LogFileContext(supportedFile.FullName, request.RunContext.RootLogDirectory));
                }
            }

            return logsToProcess;
        }

        public IList<FileInfo> GetSupportedFiles(string rootLogDirectory)
        {
            IList<FileInfo> supportedFiles = new List<FileInfo>();
            foreach (FileInfo file in DirectoryHelper.GetAllFiles(rootLogDirectory))
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
    }
}