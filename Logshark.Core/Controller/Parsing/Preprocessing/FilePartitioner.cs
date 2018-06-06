using LogParsers.Base;
using Logshark.Common.Extensions;
using System;
using System.Collections.Generic;
using System.IO;

namespace Logshark.Core.Controller.Parsing.Preprocessing
{
    /// <summary>
    /// Handles partitioning of a single logfile into smaller chunks for faster parallel processing.
    /// </summary>
    internal class FilePartitioner
    {
        protected readonly LogFileContext file;
        protected readonly long partitionSizeBytes;
        protected bool finishedPartitioning;
        protected bool encounteredEndOfFile;
        protected int linesWritten;

        public FilePartitioner(LogFileContext file, long partitionSizeBytes)
        {
            this.file = file;
            this.partitionSizeBytes = partitionSizeBytes;
        }

        /// <summary>
        /// Splits a single logfile into a number of partitions.
        /// </summary>
        /// <returns>List containing all logfiles which were produced from the original source file.</returns>
        public IList<LogFileContext> PartitionFile()
        {
            if (finishedPartitioning)
            {
                throw new InvalidOperationException("Cannot invoke partitioning more than once on a single file!");
            }

            IList<LogFileContext> partitions = new List<LogFileContext>();

            // Sanity check to see if this file can & should be partitioned.
            if (file.FileSize <= partitionSizeBytes)
            {
                partitions.Add(file);
                return partitions;
            }

            // Generate chunks.
            using (var lineIterator = File.ReadLines(file.FilePath).GetEnumerator())
            {
                while (!encounteredEndOfFile)
                {
                    int partitionIndex = partitions.Count + 1;
                    LogFileContext partition = WritePartition(lineIterator, partitionIndex);
                    partitions.Add(partition);
                }
            }

            // Destroy the original file now that we're finished with it.
            File.Delete(file.FilePath);

            finishedPartitioning = true;
            return partitions;
        }

        /// <summary>
        /// Creates a single partition up to the size partitionSizeBytes.
        /// </summary>
        /// <param name="lineIterator">An open line iterator on the source file.</param>
        /// <param name="partitionIndex">The relative index of this partition.  Used to determine filename.</param>
        /// <returns>LogFileContext for the newly generated partition.</returns>
        protected virtual LogFileContext WritePartition(IEnumerator<string> lineIterator, int partitionIndex)
        {
            int lineOffset = linesWritten;
            var partitionName = GeneratePartitionName(file.FilePath, partitionIndex);

            // Pass along the artifact metadata from the original file that spawned this partition.
            Func<LogFileContext, IDictionary<string, object>> metadataRetrievalCallback = _ => file.ArtifactSpecificFileMetadata;

            using (var writer = File.CreateText(partitionName))
            {
                long bytesWritten = 0;
                while (bytesWritten <= partitionSizeBytes)
                {
                    // Move to next line, or terminate if there is no next line.
                    if (!lineIterator.MoveNext())
                    {
                        encounteredEndOfFile = true;
                        return new LogFileContext(partitionName, file.RootLogDirectory, metadataRetrievalCallback: metadataRetrievalCallback, logicalFileName: file.FileName, lineOffset: lineOffset);
                    }

                    writer.WriteLine(lineIterator.Current);
                    linesWritten++;
                    bytesWritten += lineIterator.Current.Length * sizeof(char);
                }
            }

            return new LogFileContext(partitionName, file.RootLogDirectory, metadataRetrievalCallback: metadataRetrievalCallback, logicalFileName: file.FileName, lineOffset: lineOffset);
        }

        /// <summary>
        /// Generates a new unique file name for a partition.
        /// </summary>
        /// <param name="sourceFile">The source file used to create a partition.</param>
        /// <param name="partitionIndex">The index of the partition being created.</param>
        /// <returns>New unique file name for the partition.</returns>
        protected virtual string GeneratePartitionName(string sourceFile, int partitionIndex)
        {
            string extension = Path.GetExtension(sourceFile) ?? ".part";
            string suffix = String.Format("-part{0}{1}", partitionIndex, extension);

            return sourceFile.ReplaceLastOccurrence(extension, suffix, StringComparison.InvariantCultureIgnoreCase);
        }
    }
}