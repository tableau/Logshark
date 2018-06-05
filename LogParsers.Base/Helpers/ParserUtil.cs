using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LogParsers.Base.Helpers
{
    public static class ParserUtil
    {
        /// <summary>
        /// Returns a list of all parent directories to the log file, not including directories that are outside the scope of the logset.
        /// </summary>
        /// <param name="absoluteFilePath">An absolute path to a log file.</param>
        /// <param name="rootLogLocation">An absolute path to a root log location.</param>
        /// <returns>List of all directories between the unpack location and the log file.</returns>
        public static IList<string> GetParentLogDirs(string absoluteFilePath, string rootLogLocation)
        {
            var relativePath = absoluteFilePath.Substring(rootLogLocation.Length);
            var segments = relativePath.Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);
            var parentLogDirs = from segment in segments
                                where !segment.Equals(Path.GetFileName(absoluteFilePath))
                                select segment;

            return parentLogDirs.ToList();
        }

        /// <summary>
        /// New up a CollectionSchema object for a given collection name & index list.
        /// </summary>
        /// <param name="collectionName">The name of the collection.</param>
        /// <param name="indexNames">A list of indexes that are associated with this collection.</param>
        /// <returns>CollectionSchema object</returns>
        public static CollectionSchema CreateCollectionSchema(string collectionName, IList<string> indexNames)
        {
            CollectionSchema collectionSchema = new CollectionSchema(collectionName);
            foreach (var indexName in indexNames)
            {
                collectionSchema.AddIndex(indexName);
            }

            return collectionSchema;
        }
    }
}