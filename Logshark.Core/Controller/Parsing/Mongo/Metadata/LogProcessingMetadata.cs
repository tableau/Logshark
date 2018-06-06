using Logshark.Core.Helpers.Timers;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;

namespace Logshark.Core.Controller.Parsing.Mongo.Metadata
{
    /// <summary>
    /// Represents a metadata model for a processed Logshark logset.
    /// </summary>
    [BsonIgnoreExtraElements]
    internal class LogProcessingMetadata
    {
        [BsonId]
        public string Id { get; set; }

        [BsonRequired]
        [BsonElement("logset_target")]
        public string Target { get; set; }

        [BsonRequired]
        [BsonElement("logset_size")]
        public long? TargetSize { get; set; }

        [BsonElement("processed_size")]
        public long? ProcessedSize { get; set; }

        [BsonElement("logset_type")]
        public string LogsetType { get; set; }

        [BsonRequired]
        [BsonElement("processing_start")]
        public DateTime ProcessingTimestamp { get; set; }

        [BsonRequired]
        [BsonElement("processing_complete")]
        public bool ProcessedSuccessfully { get; set; }

        [BsonElement("processing_heartbeat")]
        [BsonIgnoreIfDefault]
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

        [BsonElement("processed_by_artifact_processor_type")]
        public string ArtifactProcessorType { get; set; }

        [BsonElement("processed_by_artifact_processor_version")]
        public Version ArtifactProcessorVersion { get; set; }

        [BsonElement("collections_parsed")]
        [BsonIgnoreIfNull]
        public SortedSet<string> CollectionsParsed { get; set; }

        [BsonElement("processing_time")]
        [BsonIgnoreIfNull]
        public double? ProcessingTime { get; set; }

        [BsonElement("failed_file_parses")]
        [BsonIgnoreIfNull]
        public IEnumerable<string> FailedFileParses { get; set; }

        public LogProcessingMetadata(LogsetParsingRequest request)
        {
            Id = request.LogsetHash;
            Target = request.Target;
            TargetSize = request.Target.Size;
            LogsetType = request.ArtifactProcessor.ArtifactType;
            ProcessedSuccessfully = false;
            ProcessingTimestamp = request.CreationTimestamp;
            ProcessingTime = GlobalEventTimingData.GetElapsedTime("Parsed Files", request.LogsetHash);
            User = Environment.UserName;
            Machine = Environment.MachineName;
            LogsharkVersion = typeof(LogsharkRequestProcessor).Assembly.GetName().Version.ToString();
            ArtifactProcessorType = request.ArtifactProcessor.GetType().Name;
            ArtifactProcessorVersion = request.ArtifactProcessor.GetType().Assembly.GetName().Version;
            CollectionsParsed = new SortedSet<string>(request.CollectionsToParse);
        }
    }
}