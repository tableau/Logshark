using MongoDB.Bson.Serialization.Attributes;
using System;

namespace Logshark.Plugins.Filestore.Models
{
    [BsonIgnoreExtraElements]
    internal class FilestoreEvent
    {
        [BsonElement("ts")]
        public DateTime Timestamp { get; set; }

        [BsonElement("sev")]
        public string Severity { get; set; }

        [BsonElement("message")]
        public string Message { get; set; }

        [BsonElement("class")]
        public string Class { get; set; }

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