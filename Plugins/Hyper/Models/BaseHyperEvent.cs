using MongoDB.Bson.Serialization.Attributes;
using System;
using Tableau.ExtractApi.DataAttributes;

namespace Logshark.Plugins.Hyper.Models
{
    [BsonIgnoreExtraElements]
    public abstract class BaseHyperEvent
    {
        [BsonId]
        [ExtractIgnore]
        public string MongoId { get; set; }

        [BsonElement("ts")]
        public DateTime Timestamp { get; set; }

        [BsonElement("pid")]
        public int ProcessId { get; set; }

        [BsonElement("tid")]
        public string ThreadId { get; set; }

        [BsonElement("sev")]
        public string Severity { get; set; }

        [BsonElement("req")]
        [BsonIgnoreIfNull]
        public string RequestId { get; set; }

        [BsonElement("sess")]
        [BsonIgnoreIfNull]
        public string SessionId { get; set; }

        [BsonElement("site")]
        [BsonIgnoreIfNull]
        public string Site { get; set; }

        [BsonElement("user")]
        [BsonIgnoreIfNull]
        public string User { get; set; }

        [BsonElement("k")]
        public string Key { get; set; }

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