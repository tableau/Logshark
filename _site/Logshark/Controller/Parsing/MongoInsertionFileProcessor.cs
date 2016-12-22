using System;
using log4net;
using LogParsers;
using LogParsers.Helpers;
using Logshark.Extensions;
using Logshark.Helpers;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace Logshark.Controller.Parsing
{
    internal class MongoInsertionFileProcessor
    {
        private readonly LogFileContext logFile;
        private readonly IParser parser;
        private readonly IMongoDatabase mongoDatabase;
        private readonly IList<Thread> inFlightInsertions;
        private readonly bool processDebugLogs;
        private ICollection<BsonDocument> insertionQueue;
        private long insertionQueueByteCount;

        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public MongoInsertionFileProcessor(LogFileContext logFile, LogsharkRequest request)
        {
            this.logFile = logFile;
            var parserFactory = new ParserFactory(request.RunContext.RootLogDirectory);
            parser = parserFactory.GetParser(logFile);
            mongoDatabase = request.Configuration.MongoConnectionInfo.GetDatabase(request.RunContext.MongoDatabaseName);
            processDebugLogs = request.ProcessDebug;
            inFlightInsertions = new List<Thread>();
            insertionQueue = new List<BsonDocument>();
        }

        /// <summary>
        /// Parse the log file associated with this processor.
        /// </summary>
        /// <returns>True if at least one document was successfully parsed.</returns>
        public bool ProcessFile()
        {
            bool processedSuccessfully = false;

            using (var fs = new FileStream(logFile.FilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var reader = new StreamReader(fs))
            {
                while (!parser.FinishedParsing)
                {
                    // Parse a document.
                    JObject jObject = parser.ParseLogDocument(reader);
                    if (jObject != null)
                    {
                        BsonDocument bson = MongoJsonHelper.GetBsonDocument(jObject);
                        long bsonDocumentSizeBytes = bson.ToBson().LongLength;

                        if (!IsPersistableLogEntry(bson))
                        {
                            continue;
                        }

                        // Check if we need to flush prior to inserting this document.
                        if (insertionQueue.Count > 0 && insertionQueueByteCount + bsonDocumentSizeBytes > LogsharkConstants.MONGO_INSERTION_BATCH_SIZE)
                        {
                            processedSuccessfully = true;
                            FlushInsertionQueue();
                        }

                        // Add document to insertion queue.
                        insertionQueue.Add(bson);
                        insertionQueueByteCount += bsonDocumentSizeBytes;
                    }
                }
            }

            // Flush any outstanding documents.
            if (insertionQueue.Count > 0)
            {
                processedSuccessfully = true;
                FlushInsertionQueue();
            }

            WaitForInsertionThreadsToFinish(LogsharkConstants.MONGO_INSERTION_THREAD_POLL_INTERVAL, LogsharkConstants.MONGO_INSERTION_THREAD_TIMEOUT);

            return processedSuccessfully;
        }

        private bool IsPersistableLogEntry(BsonDocument document)
        {
            long bsonDocumentSizeBytes = document.ToBson().LongLength;
            string severity = document.GetValue("sev", "").AsString;

            // Validate that a document of this size can even be inserted.  TODO handle this situation better for documents which exceed the max size.
            if (bsonDocumentSizeBytes > LogsharkConstants.MONGO_INSERTION_BATCH_MAX_ALLOWED_SIZE)
            {
                Log.WarnFormat("Processed a BsonDocument from {0} of size '{1}', which exceeds the maximum MongoDB allowed size of '{2}'!  Skipping insertion of this document..",
                                logFile.FileName, bsonDocumentSizeBytes.ToPrettySize(), LogsharkConstants.MONGO_INSERTION_BATCH_MAX_ALLOWED_SIZE.ToPrettySize());
                return false;
            }

            // Are we working with a debug log entry?
            if (severity.Equals("debug", StringComparison.OrdinalIgnoreCase))
            {
                // If the processDebug flag is enabled or this collection is on the debug whitelist. It's persistable.
                return (processDebugLogs || LogsharkConstants.DEBUG_PROCESSING_COLLECTION_WHITELIST.Contains(parser.CollectionSchema.CollectionName));
            }

            // All other cases are persistable.
            return true;
        }

        /// <summary>
        /// Flushes the document insertion queue to Mongo.
        /// </summary>
        private void FlushInsertionQueue()
        {
            inFlightInsertions.Add(MongoBulkInsertionHelper.CreateInsertionThread(mongoDatabase, insertionQueue, parser.CollectionSchema.CollectionName, LogsharkConstants.MONGO_MAX_INSERTION_RETRIES));
            insertionQueue = new List<BsonDocument>();
            insertionQueueByteCount = 0;
        }

        /// <summary>
        /// Waits for any in-flight insertions to finish, polling according to a sleep interval.
        /// </summary>
        /// <param name="sleepInterval">The time to sleep between polling cycles.</param>
        /// <param name="timeout">The grace period that a thread has to finish.</param>
        private void WaitForInsertionThreadsToFinish(int sleepInterval, int timeout)
        {
            int elapsedTime = 0;
            while (elapsedTime < timeout)
            {
                if (!HasLiveThreads())
                {
                    return;
                }

                // If we have a thread that is still alive, sleep and try again.
                Thread.Sleep(sleepInterval);
                elapsedTime += sleepInterval;
            }
        }

        /// <summary>
        /// Indicates whether there are any in-flight insertions that have a thread state of Alive.
        /// </summary>
        /// <returns>True if any of the insertion threads are Alive.</returns>
        private bool HasLiveThreads()
        {
            return inFlightInsertions.Any(thread => thread.IsAlive);
        }
    }
}