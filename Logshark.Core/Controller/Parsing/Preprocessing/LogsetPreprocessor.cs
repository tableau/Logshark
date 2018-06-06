using log4net;
using LogParsers.Base;
using Logshark.ArtifactProcessorModel;
using Logshark.Common.Helpers;
using Logshark.RequestModel.Config;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Logshark.Core.Controller.Parsing.Preprocessing
{
    /// <summary>
    /// Processes a root log directory and queues up supported logs for processing.
    /// Also partitions files which exceed the configured size threshold.
    /// </summary>
    internal class LogsetPreprocessor
    {
        protected readonly LogsharkTuningOptions tuningOptions;

        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public LogsetPreprocessor(LogsharkTuningOptions tuningOptions)
        {
            this.tuningOptions = tuningOptions;
        }

        /// <summary>
        /// Preprocesses the logset associated with the given artifact processor.
        /// Partitions any partitionable files that exceed the configured size threshold.
        /// </summary>
        /// <returns>Queue of preprocessed log files.</returns>
        public Queue<LogFileContext> Preprocess(string rootLogDirectory, IArtifactProcessor artifactProcessor, ISet<string> collectionsRequested)
        {
            // Sanity check.
            if (!Directory.Exists(rootLogDirectory))
            {
                throw new ArgumentException(String.Format("{0} is not a valid directory.", rootLogDirectory));
            }

            IParserFactory parserFactory = artifactProcessor.GetParserFactory(rootLogDirectory);

            // Gather a list of log files required to run the requested plugins for that type.
            IEnumerable<LogFileContext> requiredLogs = LoadRequiredLogs(rootLogDirectory, artifactProcessor, parserFactory, collectionsRequested);

            // Split large files into chunks for faster parallel processing.
            var partitioner = new ConcurrentFilePartitioner(parserFactory, tuningOptions);
            var filesToProcess = partitioner.PartitionLargeFiles(requiredLogs);

            return new Queue<LogFileContext>(filesToProcess.OrderByDescending(file => file.FileSize));
        }

        /// <summary>
        /// Loads all of the logs found in the root log directory which are supported by the given artifact processor.
        /// </summary>
        /// <returns>Log contexts for all logs required for request.</returns>
        protected IEnumerable<LogFileContext> LoadRequiredLogs(string rootLogDirectory, IArtifactProcessor artifactProcessor, IParserFactory parserFactory, ISet<string> collectionsRequested)
        {
            var logsToProcess = new List<LogFileContext>();

            // Filter down to only supported files.
            IEnumerable<FileInfo> supportedFiles = GetSupportedFiles(rootLogDirectory, parserFactory);

            // Filter supported files to keep only what we need to populate the required collections.
            foreach (var supportedFile in supportedFiles)
            {
                var parser = parserFactory.GetParser(supportedFile.FullName);
                string collectionName = parser.CollectionSchema.CollectionName;

                if (collectionsRequested.Contains(collectionName, StringComparer.InvariantCultureIgnoreCase))
                {
                    logsToProcess.Add(new LogFileContext(supportedFile.FullName, rootLogDirectory, artifactProcessor.GetAdditionalFileMetadata));
                }
            }

            return logsToProcess;
        }

        protected IEnumerable<FileInfo> GetSupportedFiles(string rootLogDirectory, IParserFactory parserFactory)
        {
            var supportedFiles = new List<FileInfo>();

            foreach (FileInfo file in DirectoryHelper.GetAllFiles(rootLogDirectory))
            {
                try
                {
                    if (parserFactory.IsSupported(file.FullName))
                    {
                        supportedFiles.Add(file);
                    }
                }
                // Just swallow any downstream exceptions for the sake of stability; we can't guarantee the artifact processor author was vigilant.
                catch (Exception ex)
                {
                    Log.ErrorFormat("Unable to determine if file '{0}' is supported: {1}", file.FullName, ex.Message);
                }
            }

            return supportedFiles;
        }
    }
}