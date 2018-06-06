using log4net;
using Logshark.ConnectionModel.Mongo;
using Logshark.Core.Exceptions;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Logshark.Core.Controller.Parsing.Mongo.Metadata
{
    /// <summary>
    /// Handles writing metadata about a logset into MongoDB.
    /// </summary>
    internal class MongoLogProcessingMetadataWriter
    {
        // Name of the master metadata database that will store metadata about all runs.
        protected const string MongoMetadataDatabaseName = "metadata";

        // Name of the metadata collection that will keep any metadata about the run.
        protected const string MongoMetadataCollectionName = "metadata";

        // List of any indexes that should be created for the metadata collection(s).
        protected static readonly IEnumerable<IndexKeysDefinition<LogProcessingMetadata>> MetadataCollectionIndices =
            new List<IndexKeysDefinition<LogProcessingMetadata>>
            {
                new IndexKeysDefinitionBuilder<LogProcessingMetadata>().Ascending(metadata => metadata.ProcessingTimestamp)
            };

        protected readonly MongoConnectionInfo mongoConnectionInfo;

        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public MongoLogProcessingMetadataWriter(MongoConnectionInfo mongoConnectionInfo)
        {
            this.mongoConnectionInfo = mongoConnectionInfo;
        }

        #region Public Methods

        /// <summary>
        /// Returns an object with all of the metadata about the processing of a logset.
        /// </summary>
        /// <returns>LogProcessingMetadata object pulled from Mongo.</returns>
        public LogProcessingMetadata Read(string databaseName)
        {
            IMongoCollection<LogProcessingMetadata> metadataCollection = mongoConnectionInfo.GetDatabase(databaseName).GetCollection<LogProcessingMetadata>(MongoMetadataCollectionName);
            return GetMetadata(metadataCollection, databaseName);
        }

        /// <summary>
        /// Write the logset processing metadata to a given database.
        /// </summary>
        /// <param name="metadata"></param>
        /// <param name="databaseName"></param>
        /// <returns></returns>
        public bool Write(LogProcessingMetadata metadata, string databaseName)
        {
            Log.DebugFormat("Writing Logshark metadata to Mongo database '{0}'..", databaseName);

            try
            {
                return GetOrCreateMetadataCollection(databaseName).ReplaceOne(GetMetadataQuery(databaseName), metadata, new UpdateOptions { IsUpsert = true }).IsAcknowledged;
            }
            catch (Exception ex)
            {
                throw new ProcessingException(String.Format("Failed to write logset processing metadata to MongoDB: {0}", ex.Message), ex);
            }
        }

        public bool WriteField(string propertyName, object propertyValue, string logsetHash)
        {
            var update = Builders<LogProcessingMetadata>.Update.Set(propertyName, BsonValue.Create(propertyValue));
            UpdateOptions updateOptions = new UpdateOptions { IsUpsert = true };

            return GetOrCreateMetadataCollection(logsetHash).UpdateOne(GetMetadataQuery(logsetHash), update, updateOptions).IsAcknowledged;
        }

        public bool WriteMasterMetadataRecord(LogProcessingMetadata metadata)
        {
            Log.DebugFormat("Writing Logshark metadata to Mongo database '{0}'..", MongoMetadataDatabaseName);

            try
            {
                return GetOrCreateMetadataCollection(MongoMetadataDatabaseName).ReplaceOne(GetMetadataQuery(metadata.Id), metadata, new UpdateOptions { IsUpsert = true }).IsAcknowledged;
            }
            catch (Exception ex)
            {
                throw new ProcessingException(String.Format("Failed to write logset processing metadata to MongoDB: {0}", ex.Message), ex);
            }
        }

        public bool DeleteMasterMetadataRecord(string databaseName)
        {
            Log.Debug("Deleting metadata record from master metadata database..");
            return GetMetadataCollection(MongoMetadataDatabaseName).DeleteOne(GetMetadataQuery(databaseName)).IsAcknowledged;
        }

        public void Dispose()
        {
        }

        #endregion Public Methods

        #region Protected Methods

        protected IMongoCollection<LogProcessingMetadata> GetOrCreateMetadataCollection(string databaseName)
        {
            IMongoCollection<LogProcessingMetadata> collection = GetMetadataCollection(databaseName);

            if (!collection.Indexes.List().Any())
            {
                Log.DebugFormat("Creating '{0}' collection in Mongo database '{1}'..", collection.CollectionNamespace.CollectionName, databaseName);

                var indexOptions = new CreateIndexOptions { Sparse = false };
                foreach (var index in MetadataCollectionIndices)
                {
                    collection.Indexes.CreateOne(index, indexOptions);
                }
            }

            return collection;
        }

        protected IMongoCollection<LogProcessingMetadata> GetMetadataCollection(string databaseName)
        {
            IMongoDatabase database = mongoConnectionInfo.GetDatabase(databaseName);
            return database.GetCollection<LogProcessingMetadata>(MongoMetadataCollectionName);
        }

        protected LogProcessingMetadata GetMetadata(string databaseName)
        {
            return GetMetadata(GetMetadataCollection(databaseName), databaseName);
        }

        protected LogProcessingMetadata GetMetadata(IMongoCollection<LogProcessingMetadata> collection, string databaseName)
        {
            return collection.FindSync(GetMetadataQuery(databaseName)).FirstOrDefault();
        }

        protected FilterDefinition<LogProcessingMetadata> GetMetadataQuery(string id)
        {
            return Builders<LogProcessingMetadata>.Filter.Eq(document => document.Id, id);
        }

        #endregion Protected Methods
    }
}