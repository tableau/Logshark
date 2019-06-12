using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Tableau.ExtractApi.DataAttributes;

namespace Logshark.Plugins.Art.Model
{
    [BsonIgnoreExtraElements]
    public class LogEventWithArt
    {
        [BsonElement("v")]
        [BsonIgnoreIfNull]
        [ExtractIgnore]
        public BsonDocument Message { get; set; }
        
        [BsonId]
        [ExtractIgnore]
        public string MongoId { get; set; }
        
        [BsonElement("req")]
        [BsonIgnoreIfNull]
        public string RequestId { get; set; }
        
        [BsonElement("pid")]
        public int ProcessId { get; set; }
        
        [BsonElement("ts")]
        public DateTime Timestamp { get; set; }
        
        [BsonElement("tid")]
        public string ThreadId { get; set; }
        
        [BsonElement("sess")]
        [BsonIgnoreIfNull]
        public string SessionId { get; set; }
        
        [BsonElement("site")]
        [BsonIgnoreIfNull]
        public string Site { get; set; }

        [BsonElement("user")]
        [BsonIgnoreIfNull]
        public string User { get; set; }
        
        [BsonElement("a")]
        [BsonIgnoreIfNull]
        [ExtractIgnore]
        public ArtData ArtData { get; set; }

        [BsonElement("ctx")]
        [BsonIgnoreIfNull]
        public ContextMetrics ContextMetrics { get; set; }
        
        #region Metadata Fields

        [BsonElement("worker")]
        public string Worker { get; set; }

        [BsonElement("file_path")]
        public string FilePath { get; set; }

        [BsonElement("file")]
        public string FileName { get; set; }

        [BsonElement("line")]
        public int Line { get; set; }

        #endregion Metadata Fields
    }
}