using Logshark.PluginLib.Helpers;
using MongoDB.Bson;
using ServiceStack.DataAnnotations;
using System;

namespace Logshark.Plugins.Apache.Model
{
    public class HttpdRequest
    {
        [PrimaryKey]
        [AutoIncrement]
        public int Id { get; set; }

        public Guid LogsetHash { get; set; }

        [Index(Unique = true)]
        public Guid EventHash { get; set; }

        [Index]
        public string RequestId { get; set; }

        public string XForwardedFor { get; set; }
        public string RequestIp { get; set; }

        [Index]
        public DateTime Timestamp { get; set; }

        public string TimestampOffset { get; set; }
        public int? Port { get; set; }
        public string RequestMethod { get; set; }
        public string RequestBody { get; set; }

        [Index]
        public int? StatusCode { get; set; }

        public int? RequestTimeMS { get; set; }

        [Index]
        public int? Worker { get; set; }

        [Index]
        public string File { get; set; }

        public int LineNumber { get; set; }

        public HttpdRequest() { }

        public HttpdRequest(BsonDocument logLine, Guid logsetHash)
        {
            LogsetHash = logsetHash;
            RequestId = BsonDocumentHelper.GetString("request_id", logLine);
            XForwardedFor = BsonDocumentHelper.GetString("xforwarded_for", logLine);
            RequestIp = BsonDocumentHelper.GetString("request_ip", logLine);
            Timestamp = BsonDocumentHelper.GetDateTime("ts", logLine);
            TimestampOffset = BsonDocumentHelper.GetString("ts_offset", logLine);
            Port = BsonDocumentHelper.GetNullableInt("port", logLine);
            RequestMethod = BsonDocumentHelper.GetString("request_method", logLine);
            RequestBody = BsonDocumentHelper.GetString("resource", logLine);
            StatusCode = BsonDocumentHelper.GetNullableInt("status_code", logLine);
            RequestTimeMS = BsonDocumentHelper.GetNullableInt("request_time", logLine);
            Worker = BsonDocumentHelper.GetNullableInt("worker", logLine);
            File = String.Format(@"{0}\{1}", BsonDocumentHelper.GetString("file_path", logLine), BsonDocumentHelper.GetString("file", logLine));
            LineNumber = BsonDocumentHelper.GetInt("line", logLine);
            EventHash = GetEventHash(logLine);
        }

        protected Guid GetEventHash(BsonDocument logLine)
        {
            return HashHelper.GenerateHashGuid(RequestId, Timestamp, TimestampOffset, File);
        }
    }
}
