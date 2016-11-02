using System;
using System.Reflection;
using log4net;
using MongoDB.Driver;

namespace Logshark.Controller.Metadata.Logset.Mongo
{
    /// <summary>
    /// Handles reading the metadata about a logset from MongoDB.
    /// </summary>
    internal class LogsetMetadataReader
    {
        public IMongoCollection<LogsetMetadata> MetadataCollection { get; private set; }
        public string MetadataDocumentId { get; private set; }

        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public LogsetMetadataReader(LogsharkRequest logsharkRequest)
        {
            IMongoDatabase mongoDatabase = logsharkRequest.Configuration.MongoConnectionInfo.GetDatabase(logsharkRequest.RunContext.MongoDatabaseName);
            MetadataCollection = mongoDatabase.GetCollection<LogsetMetadata>(LogsharkConstants.MONGO_METADATA_COLLECTION_NAME);
            MetadataDocumentId = logsharkRequest.RunContext.LogsetHash;
        }

        /// <summary>
        /// Returns an object with all of the metadata about the processing of a logset.
        /// </summary>
        /// <returns>LogsetMetadata object pulled from Mongo.</returns>
        public LogsetMetadata GetMetadata()
        {
            var filter = Builders<LogsetMetadata>.Filter.Eq("_id", MetadataDocumentId);
            return MetadataCollection.Find(filter).FirstOrDefault();
        }

        /// <summary>
        /// Retrieves the logset type for a given request.
        /// </summary>
        /// <returns>Logset type of remote logset.</returns>
        public LogsetType GetLogsetType()
        {
            try
            {
                // If we have a hash match we need to know what kind of logset this is..
                LogsetMetadata metadata = GetMetadata();
                LogsetType logsetType;
                bool retrievedLogsetType = Enum.TryParse(metadata.LogsetType, true, out logsetType);
                if (retrievedLogsetType)
                {
                    return logsetType;
                }
            }
            catch (Exception ex)
            {
                Log.ErrorFormat("Error retrieving logset type: {0}", ex);
            }

            return LogsetType.Unknown;
        }

        /// <summary>
        /// Retrieves the logset size for a given request, in bytes.
        /// </summary>
        /// <returns>Uncompressed logset size of remote logset.</returns>
        public long GetLogsetUncompressedSize()
        {
            try
            {
                // If we have a hash match we need to know what kind of logset this is..
                LogsetMetadata metadata = GetMetadata();
                return metadata.TargetUncompressedSize;
            }
            catch (Exception ex)
            {
                Log.ErrorFormat("Error retrieving uncompressed logset size: {0}", ex);
                return 0;
            }
        }

        /// <summary>
        /// Retrieves the compressed logset size for a given request, in bytes.
        /// </summary>
        /// <returns>Compressed logset size of remote logset.</returns>
        public long? GetLogsetCompressedSize()
        {
            try
            {
                // If we have a hash match we need to know what kind of logset this is..
                LogsetMetadata metadata = GetMetadata();
                return metadata.TargetCompressedSize;
            }
            catch (Exception ex)
            {
                Log.ErrorFormat("Error retrieving compressed logset size: {0}", ex);
                return null;
            }
        }

        /// <summary>
        /// Retrieves the processed logset size for a given request, in bytes.
        /// </summary>
        /// <returns>Processed logset size of remote logset.</returns>
        public long? GetLogsetProcessedSize()
        {
            try
            {
                // If we have a hash match we need to know what kind of logset this is..
                LogsetMetadata metadata = GetMetadata();
                return metadata.TargetProcessedSize;
            }
            catch (Exception ex)
            {
                Log.ErrorFormat("Error retrieving logset processed size: {0}", ex);
                return null;
            }
        }

        /// <summary>
        /// Indicates whether logset metadata exists.
        /// </summary>
        /// <returns>True if logset has metadata.</returns>
        public bool HasMetadata()
        {
            return GetMetadata() != null;
        }

        /// <summary>
        /// Set state on a request as to what kind of logset we are working with.
        /// </summary>
        /// <param name="request">The request to set the LogsetType parameter on.</param>
        public static void SetLogsetType(LogsharkRequest request)
        {
            var metadataReader = new LogsetMetadataReader(request);
            LogsetType logsetType = metadataReader.GetLogsetType();
            request.RunContext.LogsetType = logsetType;
        }

        /// <summary>
        /// Set state on a request as to the size of logset we are working with.
        /// </summary>
        /// <param name="request">The request to set the logset size on.</param>
        public static void SetLogsetSize(LogsharkRequest request)
        {
            var metadataReader = new LogsetMetadataReader(request);
            request.Target.UncompressedSize = metadataReader.GetLogsetUncompressedSize();
            request.Target.CompressedSize = metadataReader.GetLogsetCompressedSize();
            request.Target.ProcessedSize = metadataReader.GetLogsetProcessedSize();
        }
    }
}
