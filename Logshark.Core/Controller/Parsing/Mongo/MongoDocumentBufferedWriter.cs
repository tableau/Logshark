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
        private const int InsertionBatchSizeBytes = 4194304;

        // Max number of bytes MongoDB allows in a single batch insertion.
        private const int InsertionMaxAllowedBatchSizeBytes = 16777216;

        // Max number of retries for failing insertions.
        private const int InsertionMaxRetries = 3;

        // Amount of time to sleep between thread activity checks, in ms.
        private const int InsertionThreadPollInterval = 100;

        // Max amount of time to wait for MongoDB insertion threads to finish their work, in ms.
        private const int InsertionThreadTimeout = 30000;

        // MongoDB-compatible JSON serialization settings.
        private static readonly JsonSerializerSettings JsonSerializerSettings = new JsonSerializerSettings
        {
            Formatting = Formatting.None,
            Converters = new List<JsonConverter>
            {
                new MongoCompatibleJsonConverter(typeof(JObject))
            }
        };

        private readonly IMongoCollection<BsonDocument> _collection;
        private readonly IList<Thread> _inFlightInsertions;
        private ICollection<BsonDocument> _insertionQueue;
        private long _insertionQueueByteCount;

        private bool _disposed;

        public MongoDocumentBufferedWriter(IMongoDatabase database, string collectionName)
        {
            _collection = database.GetCollection<BsonDocument>(collectionName);

            _inFlightInsertions = new List<Thread>();
            _insertionQueue = new List<BsonDocument>();
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
                var errorMessage = $"Cannot convert JSON object to BSON: {ex.Message}!  Skipping insertion of this document..";
                return new DocumentWriteResult(DocumentWriteResultType.Failure, errorMessage);
            }

            long bsonSizeBytes;
            try
            {
                // This seems wasteful, but currently there is no better way to do this. We need this value to validate the document will fit within MongoDB's
                // document size limit, and the only way to get it to is serialize the whole document as a BSON byte array.
                bsonSizeBytes = document.ToBson().LongLength; 
            }
            catch (BsonSerializationException ex)
            {
                if (ex.Message.Contains("Maximum serialization depth exceeded"))
                {
                    var errorMessage = $"Processed BsonDocument exceeds the maximum MongoDB allowed size of '{InsertionMaxAllowedBatchSizeBytes.ToPrettySize()}'!  Skipping insertion of this document..";
                    return new DocumentWriteResult(DocumentWriteResultType.SuccessWithWarning, errorMessage);    
                }
                else
                {
                    var errorMessage = $"Processed a BsonDocument that could not be serialized due to reason '{ex.Message}'  Skipping insertion of this document..";
                    return new DocumentWriteResult(DocumentWriteResultType.Failure, errorMessage);
                }
            }

            // Check if we need to flush prior to inserting this document.
            if (_insertionQueue.Count > 0 && _insertionQueueByteCount + bsonSizeBytes > InsertionBatchSizeBytes)
            {
                Flush();
            }

            // Add document to insertion queue.
            _insertionQueue.Add(bson);
            _insertionQueueByteCount += bsonSizeBytes;

            return new DocumentWriteResult(DocumentWriteResultType.Success);
        }

        /// <summary>
        /// Shuts down the writer and waits for any in-flight insertions to finish, polling according to a sleep interval.
        /// </summary>
        public void Shutdown()
        {
            Flush();

            var elapsedTime = 0;
            while (HasLiveThreads() && elapsedTime < InsertionThreadTimeout)
            {
                // If we have a thread that is still alive, sleep and try again.
                Thread.Sleep(InsertionThreadPollInterval);
                elapsedTime += InsertionThreadPollInterval;
            }

            foreach (var inFlightInsertion in _inFlightInsertions)
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
        private static BsonDocument GetBsonDocument(JObject jObject)
        {
            var json = JsonConvert.SerializeObject(jObject, JsonSerializerSettings);
            return BsonSerializer.Deserialize<BsonDocument>(json);
        }

        /// <summary>
        /// Flushes the document insertion queue to MongoDB.
        /// </summary>
        private void Flush()
        {
            if (_insertionQueue.Any())
            {
                _inFlightInsertions.Add(CreateInsertionThread(_insertionQueue));
            }

            _insertionQueue = new List<BsonDocument>();
            _insertionQueueByteCount = 0;
        }

        private Thread CreateInsertionThread(ICollection<BsonDocument> documents)
        {
            var insertionThread = new Thread(() => MongoBulkInsertionTask.InsertDocuments(documents, _collection, InsertionMaxRetries));
            insertionThread.Start();
            return insertionThread;
        }

        /// <summary>
        /// Indicates whether there are any in-flight insertions that have a thread state of Alive.
        /// </summary>
        /// <returns>True if any of the insertion threads are Alive.</returns>
        private bool HasLiveThreads()
        {
            return _inFlightInsertions.Any(thread => thread.IsAlive);
        }

        #endregion Protected Methods

        #region IDisposable Support

        private void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    Shutdown();
                }

                _disposed = true;
            }
        }

        #endregion IDisposable Support
    }
}