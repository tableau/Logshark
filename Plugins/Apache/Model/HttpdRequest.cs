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

        [Index(Unique = true)]
        public Guid EventHash { get; set; }

        #region Apache Data Fields

        public long? ContentLength { get; set; }

        public int? Port { get; set; }

        public string RequestBody { get; set; }

        [Index]
        public string RequestId { get; set; }

        public string RequestIp { get; set; }

        public string RequestMethod { get; set; }

        public long? RequestTimeMS { get; set; }

        [Index]
        public string Requester { get; set; }

        [Index]
        public int? StatusCode { get; set; }

        [Index]
        public DateTime Timestamp { get; set; }

        public string TimestampOffset { get; set; }

        public string XForwardedFor { get; set; }

        #endregion Apache Data Fields

        #region Metadata Fields

        [Index]
        public string File { get; set; }

        public int? LineNumber { get; set; }

        public Guid LogsetHash { get; set; }

        [Index]
        public int? Worker { get; set; }

        #endregion Metadata Fields

        public HttpdRequest() { }

        public HttpdRequest(BsonDocument logLine, Guid logsetHash)
        {
            // Initialize Apache Data fields
            ContentLength = BsonDocumentHelper.GetNullableLong("content_length", logLine);
            Port = BsonDocumentHelper.GetNullableInt("port", logLine);
            RequestBody = BsonDocumentHelper.GetString("resource", logLine);
            RequestId = BsonDocumentHelper.GetString("request_id", logLine);
            RequestIp = BsonDocumentHelper.GetString("request_ip", logLine);
            RequestMethod = BsonDocumentHelper.GetString("request_method", logLine);
            RequestTimeMS = BsonDocumentHelper.GetNullableLong("request_time", logLine);
            Requester = BsonDocumentHelper.GetString("requester", logLine);
            StatusCode = BsonDocumentHelper.GetNullableInt("status_code", logLine);
            Timestamp = BsonDocumentHelper.GetDateTime("ts", logLine);
            TimestampOffset = BsonDocumentHelper.GetString("ts_offset", logLine);
            XForwardedFor = BsonDocumentHelper.GetString("xforwarded_for", logLine);

            // Initialize Metadata fields
            File = String.Format(@"{0}\{1}", BsonDocumentHelper.GetString("file_path", logLine), BsonDocumentHelper.GetString("file", logLine));
            LineNumber = BsonDocumentHelper.GetNullableInt("line", logLine);
            LogsetHash = logsetHash;
            Worker = BsonDocumentHelper.GetNullableInt("worker", logLine);

            // Generate unique event hash
            EventHash = HashHelper.GenerateHashGuid(RequestId, Timestamp, TimestampOffset, File);
        }
    }
}
