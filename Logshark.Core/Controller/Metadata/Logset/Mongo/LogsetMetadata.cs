using log4net;
using Logshark.RequestModel;
using Logshark.RequestModel.Timers;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Logshark.Core.Controller.Metadata.Logset.Mongo
{
    /// <summary>
    /// Represents a metadata model for a processed Logshark logset.
    /// </summary>
    [BsonIgnoreExtraElements]
    internal class LogsetMetadata
    {
        protected LogsharkRequest request;
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        [BsonId]
        public string Id { get; set; }

        [BsonRequired]
        [BsonElement("logset_target")]
        public string Target { get; set; }

        [BsonRequired]
        [BsonElement("logset_size")]
        public long TargetUncompressedSize { get; set; }

        [BsonElement("logset_compressed_size")]
        public long? TargetCompressedSize { get; set; }

        [BsonElement("logset_processed_size")]
        public long? TargetProcessedSize { get; set; }

        [BsonElement("logset_type")]
        public string LogsetType { get; set; }

        [BsonRequired]
        [BsonElement("processing_start")]
        public DateTime ProcessingTimestamp { get; set; }

        [BsonRequired]
        [BsonElement("processing_complete")]
        public bool ProcessedSuccessfully { get; set; }

        [BsonElement("processing_heartbeat")]
        public DateTime ProcessingHeartbeat { get; set; }

        [BsonRequired]
        [BsonElement("processed_by_user")]
        public string User { get; set; }

        [BsonRequired]
        [BsonElement("processed_by_machine")]
        public string Machine { get; set; }

        [BsonRequired]
        [BsonElement("processed_by_logshark_version")]
        public string LogsharkVersion { get; set; }

        [BsonElement("collections_parsed")]
        [BsonIgnoreIfNull]
        public IEnumerable<string> CollectionsParsed { get; set; }

        [BsonElement("processing_time")]
        [BsonIgnoreIfNull]
        public double? ProcessingTime { get; set; }

        [BsonElement("failed_file_parses")]
        [BsonIgnoreIfNull]
        public IEnumerable<string> FailedFileParses { get; set; }

        [BsonElement("timing_data")]
        [BsonIgnoreIfNull]
        public IEnumerable<TimingData> TimingData { get; set; }

        [BsonIgnore]
        public IDictionary<string, object> RequestMetadata { get; set; }

        public LogsetMetadata(LogsharkRequest request)
        {
            this.request = request;
            Id = request.RunContext.LogsetHash;
            Target = request.Target.OriginalTarget;
            TargetCompressedSize = request.Target.CompressedSize;
            TargetUncompressedSize = request.Target.UncompressedSize;
            TargetProcessedSize = request.Target.ProcessedSize;
            LogsetType = request.RunContext.LogsetType;
            ProcessedSuccessfully = false;
            ProcessingTimestamp = request.RequestCreationDate;
            User = Environment.UserName;
            Machine = Environment.MachineName;
            LogsharkVersion = typeof(LogsharkController).Assembly.GetName().Version.ToString();
            CollectionsParsed = request.RunContext.CollectionsGenerated;
            RequestMetadata = request.Metadata;
        }

        public BsonDocument ToBsonDocument()
        {
            var document = this.ToBsonDocument<LogsetMetadata>();

            // Add each item from RequestMetadata to the document as a root level key/value pair.
            if (RequestMetadata != null)
            {
                foreach (var metadatum in RequestMetadata)
                {
                    // Try to add it, swallow any cast exceptions if the user provided an object that isn't castable as a BsonValue.
                    try
                    {
                        document.Add(metadatum.Key, BsonValue.Create(metadatum.Value));
                    }
                    catch (Exception ex)
                    {
                        Log.ErrorFormat("Cannot add duplicate custom metadata key {0}: {1}", metadatum.Key, ex.Message);
                    }
                }
            }

            return document;
        }

        public bool IsHeartbeatExpired()
        {
            TimeSpan timeSinceLastHeartbeat = DateTime.UtcNow - ProcessingHeartbeat;
            if (timeSinceLastHeartbeat.TotalSeconds >= CoreConstants.MONGO_PROCESSING_HEARTBEAT_EXPIRATION_TIME)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}