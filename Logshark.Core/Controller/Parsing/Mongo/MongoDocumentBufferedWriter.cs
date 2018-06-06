using Logshark.Common.Extensions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Logshark.Core.Controller.Parsing.Mongo
{
    internal class MongoDocumentBufferedWriter : IDocumentWriter, IDisposable
    {
        // Max number of bytes that we will submit in a single insertion batch.
        protected const int InsertionBatchSizeBytes = 4194304;

        // Max number of bytes MongoDB allows in a single batch insertion.
        protected const int InsertionMaxAllowedBatchSizeBytes = 16777216;

        // Max number of retries for failing insertions.
        protected const int InsertionMaxRetries = 3;

        // Amount of time to sleep between thread activity checks, in ms.
        protected const int InsertionThreadPollInterval = 100;

        // Max amount of time to wait for MongoDB insertion threads to finish their work, in ms.
        protected const int InsertionThreadTimeout = 30000;

        // MongoDB InsertMany document API options.
        protected static readonly InsertManyOptions InsertionOptions = new InsertManyOptions { BypassDocumentValidation = true, IsOrdered = false };

        // MongoDB-compatible JSON serialization settings.
        protected static readonly JsonSerializerSettings JsonSerializerSettings = new JsonSerializerSettings
        {
            Formatting = Formatting.None,
            Converters = new List<JsonConverter>
            {
                new MongoCompatibleJsonConverter(typeof(JObject))
            }
        };

        protected readonly IMongoCollection<BsonDocument> collection;
        protected readonly IList<Thread> inFlightInsertions;
        protected ICollection<BsonDocument> insertionQueue;
        protected long insertionQueueByteCount;

        private bool disposed; // To detect redundant calls

        public MongoDocumentBufferedWriter(IMongoDatabase database, string collectionName)
        {
            collection = database.GetCollection<BsonDocument>(collectionName);

            inFlightInsertions = new List<Thread>();
            insertionQueue = new List<BsonDocument>();
        }

        public void Dispose()
        {
            Dispose(true);
        }

        public DocumentWriteResult Write(JObject document)
        {
            BsonDocument bson;
            try
            {
                bson = GetBsonDocument(document);
            }
            catch (Exception ex)
            {
                string errorMessage = String.Format("Cannot convert JSON object to BSON: {0}!  Skipping insertion of this document..", ex.Message);
                return new DocumentWriteResult(false, errorMessage);
            }
            
            long bsonSizeBytes = document.ToBson().LongLength;
            if (bsonSizeBytes > InsertionMaxAllowedBatchSizeBytes)
            {
                string errorMessage = String.Format("Processed a BsonDocument of size '{0}', which exceeds the maximum MongoDB allowed size of '{1}'!  Skipping insertion of this document..",
                                                    bsonSizeBytes.ToPrettySize(), InsertionMaxAllowedBatchSizeBytes.ToPrettySize());
                return new DocumentWriteResult(false, errorMessage);
            }

            // Check if we need to flush prior to inserting this document.
            if (insertionQueue.Count > 0 && insertionQueueByteCount + bsonSizeBytes > InsertionBatchSizeBytes)
            {
                Flush();
            }

            // Add document to insertion queue.
            insertionQueue.Add(bson);
            insertionQueueByteCount += bsonSizeBytes;

            return new DocumentWriteResult(isSuccessful: true);
        }

        /// <summary>
        /// Shuts down the writer and waits for any in-flight insertions to finish, polling according to a sleep interval.
        /// </summary>
        public void Shutdown()
        {
            Flush();

            int elapsedTime = 0;
            while (HasLiveThreads() && elapsedTime < InsertionThreadTimeout)
            {
                // If we have a thread that is still alive, sleep and try again.
                Thread.Sleep(InsertionThreadPollInterval);
                elapsedTime += InsertionThreadPollInterval;
            }

            foreach (var inFlightInsertion in inFlightInsertions)
            {
                inFlightInsertion.Abort();
            }
        }

        #region Protected Methods

        /// <summary>
        /// Converts a JObject to a BsonDocument using a MongoDB-friendly JSON converter.
        /// </summary>
        /// <param name="jObject">The JSON object to convert.</param>
        /// <returns>The JObject as a BsonDocument</returns>
        protected static BsonDocument GetBsonDocument(JObject jObject)
        {
            string json = JsonConvert.SerializeObject(jObject, JsonSerializerSettings);
            return BsonSerializer.Deserialize<BsonDocument>(json);
        }

        /// <summary>
        /// Flushes the document insertion queue to MongoDB.
        /// </summary>
        protected int Flush()
        {
            if (insertionQueue.Any())
            {
                inFlightInsertions.Add(CreateInsertionThread(insertionQueue));
            }

            int documentsFlushed = insertionQueue.Count;

            insertionQueue = new List<BsonDocument>();
            insertionQueueByteCount = 0;

            return documentsFlushed;
        }

        protected Thread CreateInsertionThread(ICollection<BsonDocument> documents)
        {
            Thread insertionThread = new Thread(() => MongoBulkInsertionTask.InsertDocuments(documents, collection, InsertionMaxRetries));
            insertionThread.Start();
            return insertionThread;
        }

        /// <summary>
        /// Indicates whether there are any in-flight insertions that have a thread state of Alive.
        /// </summary>
        /// <returns>True if any of the insertion threads are Alive.</returns>
        protected bool HasLiveThreads()
        {
            return inFlightInsertions.Any(thread => thread.IsAlive);
        }

        #endregion Protected Methods

        #region IDisposable Support

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    Shutdown();
                }

                disposed = true;
            }
        }

        #endregion IDisposable Support
    }
}