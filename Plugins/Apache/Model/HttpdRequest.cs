using MongoDB.Bson.Serialization.Attributes;
using System;

namespace Logshark.Plugins.Apache.Model
{
    [BsonIgnoreExtraElements]
    public class HttpdRequest
    {
        [BsonElement("content_length")]
        public long? ContentLength { get; set; }

        [BsonElement("port")]
        public int? Port { get; set; }

        [BsonElement("resource")]
        public string RequestBody { get; set; }

        [BsonElement("request_id")]
        public string RequestId { get; set; }

        [BsonElement("request_ip")]
        public string RequestIp { get; set; }

        [BsonElement("request_method")]
        public string RequestMethod { get; set; }

        [BsonElement("request_time")]
        public long? RequestTimeMS { get; set; }

        [BsonElement("requester")]
        public string Requester { get; set; }

        [BsonElement("status_code")]
        public int? StatusCode { get; set; }

        [BsonElement("ts")]
        public DateTime Timestamp { get; set; }

        [BsonElement("ts_offset")]
        public string TimestampOffset { get; set; }

        [BsonElement("xforwarded_for")]
        public string XForwardedFor { get; set; }

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
