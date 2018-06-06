using log4net;
using LogParsers.Base;
using LogParsers.Base.Helpers;
using LogParsers.Base.Parsers;
using Logshark.Common.Extensions;
using Logshark.Common.TaskSchedulers;
using Logshark.Core.Helpers.Timers;
using Logshark.RequestModel.Config;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Logshark.Core.Controller.Parsing.Preprocessing
{
    internal class ConcurrentFilePartitioner
    {
        protected readonly IParserFactory parserFactory;
        protected readonly LogsharkTuningOptions tuningOptions;

        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public ConcurrentFilePartitioner(IParserFactory parserFactory, LogsharkTuningOptions tuningOptions)
        {
            this.parserFactory = parserFactory;
            this.tuningOptions = tuningOptions;
        }

        /// <summary>
        /// Splits all logfiles exceeding the configured size threshold into smaller chunks.
        /// </summary>
        /// <param name="files">The collection of files eligible for partitioning.</param>
        /// <returns>Collection of all files in logset following the partitioning process.</returns>
        public IEnumerable<LogFileContext> PartitionLargeFiles(IEnumerable<LogFileContext> files)
        {
            long maxBytes = tuningOptions.FilePartitionerThresholdMb * 1024 * 1024;

            // Build list of files to chunk by searching for files exceeding the max size and consulting the parser factory to see if it's a single-line log type.
            var processedFiles = new ConcurrentBag<LogFileContext>();
            var filesToPartition = new List<LogFileContext>();
            foreach (var file in files)
            {
                if (IsPartitionableFile(file, maxBytes))
                {
                    filesToPartition.Add(file);
                }
                else
                {
                    // Nothing to do; use it as-is.
                    processedFiles.Add(file);
                }
            }

            if (!filesToPartition.Any())
            {
                Log.InfoFormat("No log files were found that are larger than {0}MB; skipping partitioning phase.", tuningOptions.FilePartitionerThresholdMb);
                return processedFiles;
            }

            Log.InfoFormat("Partitioning {0} log {1} larger than {2}MB to speed up processing.  This may take some time..",
                            filesToPartition.Count, "file".Pluralize(filesToPartition.Count), tuningOptions.FilePartitionerThresholdMb);

            // Set up task scheduler.
            TaskFactory factory = GetFilePartitioningTaskFactory();

            // Spin up partitioning tasks in parallel.
            Task[] taskArray = new Task[filesToPartition.Count];
            for (var i = 0; i < filesToPartition.Count; i++)
            {
                var fileToChunk = filesToPartition[i];
                taskArray[i] = factory.StartNew(() =>
                {
                    var partitions = PartitionFile(fileToChunk, maxBytes);
                    processedFiles.AddRange(partitions);
                });
            }

            // Wait on any in-flight tasks.
            Task.WaitAll(taskArray);

            return processedFiles;
        }

        /// <summary>
        /// Indicates whether a given file qualifies for partitioning.
        /// </summary>
        protected virtual bool IsPartitionableFile(LogFileContext file, long maxFileSizeBytes)
        {
            if (file.FileSize <= maxFileSizeBytes)
            {
                return false;
            }

            IParser parser = parserFactory.GetParser(file.FilePath);

            return parser != null && !parser.IsMultiLineLogType;
        }

        /// <summary>
        /// Creates a task factory to handle all file partitioning tasks.
        /// </summary>
        /// <returns></returns>
        protected virtual TaskFactory GetFilePartitioningTaskFactory()
        {
            Log.InfoFormat("Setting file partitioning concurrency limit to {0} concurrent files.", tuningOptions.FilePartitionerConcurrencyLimit);
            LimitedConcurrencyLevelTaskScheduler taskScheduler = new LimitedConcurrencyLevelTaskScheduler(tuningOptions.FilePartitionerConcurrencyLimit);

            return new TaskFactory(taskScheduler);
        }

        /// <summary>
        /// Partitions a single file into multiple pieces.
        /// </summary>
        protected virtual IEnumerable<LogFileContext> PartitionFile(LogFileContext fileToPartition, long maxFileSizeBytes)
        {
            using (var partitionFileTimer = new LogsharkTimer("Partition File", String.Format("{0}/{1}", fileToPartition.FileLocationRelativeToRoot, fileToPartition.FileName), GlobalEventTimingData.Add))
            {
                Log.InfoFormat("Partitioning file {0}.. ({1})", fileToPartition.FileName, fileToPartition.FileSize.ToPrettySize());

                var partitioner = new FilePartitioner(fileToPartition, maxFileSizeBytes);
                IList<LogFileContext> partitions = partitioner.PartitionFile();

                Log.InfoFormat("Finished partitioning file {0} ({1}) [{2}]", fileToPartition.FileName, fileToPartition.FileSize.ToPrettySize(), partitionFileTimer.Elapsed.Print());

                return partitions;
            }
        }
    }
}