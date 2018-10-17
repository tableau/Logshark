using MongoDB.Bson.Serialization.Attributes;
using System;

namespace Logshark.Plugins.Vizportal.Models
{
    [BsonIgnoreExtraElements]
    public class VizportalEvent
    {
        [BsonElement("req")]
        public string RequestId { get; set; }

        [BsonElement("ts")]
        public DateTime Timestamp { get; set; }

        [BsonElement("user")]
        public string User { get; set; }

        [BsonElement("sess")]
        public string SessionId { get; set; }

        [BsonElement("site")]
        public string Site { get; set; }

        [BsonElement("sev")]
        public string Severity { get; set; }

        [BsonElement("class")]
        public string Class { get; set; }

        [BsonElement("message")]
        public string Message { get; set; }

        [BsonElement("worker")]
        public string Worker { get; set; }

        [BsonElement("file_path")]
        public string FilePath { get; set; }

        [BsonElement("file")]
        public string File { get; set; }

        [BsonElement("line")]
        public int LineNumber { get; set; }
    }
}