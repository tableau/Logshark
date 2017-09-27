using log4net;
using Logshark.Core.Exceptions;
using Logshark.RequestModel;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Reflection;

namespace Logshark.Core.Controller.Metadata.Logset.Mongo
{
    /// <summary>
    /// Handles writing metadata about a logset into MongoDB.
    /// </summary>
    internal class LogsetMetadataWriter
    {
        protected readonly LogsharkRequest logsharkRequest;
        protected readonly IMongoDatabase logsetDatabase;
        protected readonly IMongoCollection<BsonDocument> logsetMetadataCollection;
        protected readonly IMongoCollection<BsonDocument> masterMetadataCollection;
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public LogsetMetadataWriter(LogsharkRequest logsharkRequest)
        {
            this.logsharkRequest = logsharkRequest;
            logsetDatabase = logsharkRequest.Configuration.MongoConnectionInfo.GetDatabase(logsharkRequest.RunContext.MongoDatabaseName);
            logsetMetadataCollection = GetOrCreateMetadataCollection();
            masterMetadataCollection = logsharkRequest.Configuration.MongoConnectionInfo.GetDatabase(CoreConstants.MONGO_METADATA_DATABASE_NAME).GetCollection<BsonDocument>(CoreConstants.MONGO_METADATA_COLLECTION_NAME);
        }

        #region Public Methods

        public void WritePreProcessingMetadata()
        {
            Log.Debug("Writing Logshark metadata to Mongo database..");
            var metadataTimer = logsharkRequest.RunContext.CreateTimer("Write Metadata", "Pre-processing");

            var metadata = new LogsetMetadata(logsharkRequest);

            try
            {
                logsetMetadataCollection.ReplaceOne(GetMetadataDocumentQuery(), metadata.ToBsonDocument(), new UpdateOptions { IsUpsert = true });
            }
            catch (Exception ex)
            {
                throw new ProcessingException(String.Format("Failed to write pre-processing logset metadata to MongoDB: {0}", ex.Message), ex);
            }
            finally
            {
                metadataTimer.Stop();
            }
        }

        public void WritePostProcessingMetadata(bool processedSuccessfully)
        {
            var metadataTimer = logsharkRequest.RunContext.CreateTimer("Write Metadata", "Post-processing");

            try
            {
                logsetMetadataCollection.UpdateOne(GetMetadataDocumentQuery(), GetPostProcessingUpdate(processedSuccessfully));
            }
            catch (Exception ex)
            {
                throw new ProcessingException(String.Format("Failed to update logset metadata in MongoDB: {0}", ex.Message), ex);
            }
            finally
            {
                metadataTimer.Stop();
            }
        }

        public void WriteProperty(string propertyName, object propertyValue)
        {
            var update = Builders<BsonDocument>.Update.Set(propertyName, BsonValue.Create(propertyValue));
            UpdateOptions updateOptions = new UpdateOptions { IsUpsert = true };

            logsetMetadataCollection.UpdateOne(GetMetadataDocumentQuery(), update, updateOptions);
        }

        public void RemoveProperty(string propertyName)
        {
            var update = Builders<BsonDocument>.Update.Unset(propertyName);

            logsetMetadataCollection.UpdateOne(GetMetadataDocumentQuery(), update);
        }

        public void WriteMasterMetadataRecord()
        {
            Log.Debug("Writing metadata to master metadata database..");

            // Copy the contents of the main metadata document in the logset's metadata collection.
            BsonDocument metadataDocument = GetMetadataDocument(logsetMetadataCollection);
            if (metadataDocument == null)
            {
                Log.ErrorFormat("No metadata document found in database '{0}'!  Cannot write master metadata record.", logsetDatabase);
                return;
            }

            // Save out the modified metadata document to the "master" metadata db.
            try
            {
                masterMetadataCollection.ReplaceOne(item => item["_id"] == metadataDocument["_id"], metadataDocument, new UpdateOptions { IsUpsert = true });
            }
            catch (Exception ex)
            {
                throw new ProcessingException(String.Format("Failed to write master metadata: {0}", ex.Message), ex);
            }
        }

        public void DeleteMasterMetadataRecord()
        {
            Log.Debug("Deleting metadata record from master metadata database..");

            BsonDocument metadataDocument = GetMetadataDocument(masterMetadataCollection);
            if (metadataDocument == null)
            {
                Log.Debug("No master metadata record found to delete!");
                return;
            }

            masterMetadataCollection.DeleteOne(metadataDocument);
        }

        #endregion Public Methods

        #region Protected Methods

        protected IMongoCollection<BsonDocument> GetOrCreateMetadataCollection()
        {
            if (logsetDatabase.GetCollection<BsonDocument>(CoreConstants.MONGO_METADATA_COLLECTION_NAME).Indexes.List().ToList().Count == 0)
            {
                return logsetDatabase.GetCollection<BsonDocument>(CoreConstants.MONGO_METADATA_COLLECTION_NAME);
            }

            Log.DebugFormat("Creating '{0}' collection in Mongo database '{1}'..", CoreConstants.MONGO_METADATA_COLLECTION_NAME, logsharkRequest.RunContext.MongoDatabaseName);
            var collection = logsetDatabase.GetCollection<BsonDocument>(CoreConstants.MONGO_METADATA_COLLECTION_NAME);

            foreach (var index in CoreConstants.MONGO_METADATA_COLLECTION_INDEXES)
            {
                var indexKeysBuilder = new IndexKeysDefinitionBuilder<BsonDocument>();
                CreateIndexOptions indexOptions = new CreateIndexOptions { Sparse = false };
                collection.Indexes.CreateOne(indexKeysBuilder.Ascending(index), indexOptions);
            }

            return collection;
        }

        protected FilterDefinition<BsonDocument> GetMetadataDocumentQuery()
        {
            return Builders<BsonDocument>.Filter.Eq("_id", logsharkRequest.RunContext.LogsetHash);
        }

        protected BsonDocument GetMetadataDocument(IMongoCollection<BsonDocument> collection)
        {
            return collection.FindSync(GetMetadataDocumentQuery()).FirstOrDefault();
        }

        protected UpdateDefinition<BsonDocument> GetPostProcessingUpdate(bool processedSuccessfully)
        {
            var processingTime = Math.Round((DateTime.UtcNow - logsharkRequest.RequestCreationDate).TotalSeconds, 3);

            var update = Builders<BsonDocument>.Update.Set("processing_complete", processedSuccessfully)
                                                      .Set("processing_time", processingTime)
                                                      .Set("failed_file_parses", logsharkRequest.RunContext.FailedFileParses)
                                                      .Set("collections_parsed", logsharkRequest.RunContext.CollectionsGenerated)
                                                      .Set("timing_data", logsharkRequest.RunContext.TimingData);

            return update;
        }

        #endregion Protected Methods
    }
}