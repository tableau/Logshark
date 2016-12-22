using log4net;
using LogParsers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using LogParsers.Helpers;
using Logshark.Extensions;
using Logshark.TaskSchedulers;

namespace Logshark.Controller.Parsing.Partitioning
{
    internal class ConcurrentFilePartitioner
    {
        protected LogsharkRequest request;
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public ConcurrentFilePartitioner(LogsharkRequest request)
        {
            this.request = request;
        }

        /// <summary>
        /// Splits all logfiles exceeding the specified size threshold into smaller chunks.
        /// </summary>
        /// <param name="files">The collection of files eligible for partitioning.</param>
        /// <returns>Collection of all files in logset following the partition step.</returns>
        public IEnumerable<LogFileContext> PartitionLargeFiles(IEnumerable<LogFileContext> files)
        {
            long maxBytes = request.Configuration.TuningOptions.FilePartitionerThresholdMb * 1024 * 1024;

            // Build list of files to chunk by searching for files exceeding the max size and consulting the parser
            // factory to see if it's a single-line parsable log type.  We avoid doing chunking on logs where a single document
            // spans multiple log lines.
            var processedFiles = new ConcurrentBag<LogFileContext>();
            IList<LogFileContext> filesToPartition = new List<LogFileContext>();
            var parserFactory = new ParserFactory(request.RunContext.RootLogDirectory);
            foreach (var file in files)
            {
                if (file.FileSize > maxBytes)
                {
                    // If there's no parser available for it, don't bother chunking it.
                    var parser = parserFactory.GetParser(file.FilePath);
                    if (parser == null)
                    {
                        continue;
                    }

                    if (!parser.IsMultiLineLogType)
                    {
                        filesToPartition.Add(file);
                    }
                    else
                    {
                        // This is a large multi-line log type; we can't do anything with it so we just consider it good as-is.
                        processedFiles.Add(file);
                    }
                }
                else
                {
                    processedFiles.Add(file);
                }
            }

            if (filesToPartition.Count == 0)
            {
                Log.InfoFormat("No log files were found that are larger than {0}MB; skipping partitioning phase.", request.Configuration.TuningOptions.FilePartitionerThresholdMb);
                return processedFiles;
            }

            Log.InfoFormat("Partitioning {0} log {1} larger than {2}MB to speed up processing.  This may take some time..",
                            filesToPartition.Count, "file".Pluralize(filesToPartition.Count), request.Configuration.TuningOptions.FilePartitionerThresholdMb);

            // Set up task scheduler.
            var factory = GetFilePartitioningTaskFactory();

            // Spin up partitioning tasks in parallel.
            Task[] taskArray = new Task[filesToPartition.Count];
            for (var i = 0; i < filesToPartition.Count; i++)
            {
                var fileToChunk = filesToPartition[i];
                taskArray[i] = factory.StartNew(() =>
                {
                    Log.InfoFormat("Partitioning file {0}.. ({1})", fileToChunk.FileName, fileToChunk.FileSize.ToPrettySize());
                    var partitionFileTimer = request.RunContext.CreateTimer("Partition File", String.Format("{0}/{1}", fileToChunk.FileLocationRelativeToRoot, fileToChunk.FileName));
                    var partitioner = new FilePartitioner(request, fileToChunk, maxBytes);
                    IList<LogFileContext> partitions = partitioner.PartitionFile();
                    foreach (var partition in partitions)
                    {
                        processedFiles.Add(partition);
                    }
                    partitionFileTimer.Stop();
                    Log.InfoFormat("Finished partitioning file {0} ({1}) [{2}]", fileToChunk.FileName, fileToChunk.FileSize.ToPrettySize(), partitionFileTimer.Elapsed.Print());
                });
            }

            // Wait on any in-flight tasks.
            Task.WaitAll(taskArray);

            return processedFiles;
        }

        /// <summary>
        /// Creates a task factory to handle all file partitioning tasks.
        /// </summary>
        /// <returns></returns>
        private TaskFactory GetFilePartitioningTaskFactory()
        {
            Log.InfoFormat("Setting file partitioning concurrency limit to {0} concurrent files.", request.Configuration.TuningOptions.FilePartitionerConcurrencyLimit);
            LimitedConcurrencyLevelTaskScheduler lcts = new LimitedConcurrencyLevelTaskScheduler(request.Configuration.TuningOptions.FilePartitionerConcurrencyLimit);
            return new TaskFactory(lcts);
        }
    }
}