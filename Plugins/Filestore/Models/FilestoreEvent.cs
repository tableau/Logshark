using Logshark.PluginLib.Helpers;
using MongoDB.Bson;
using ServiceStack.DataAnnotations;
using System;

namespace Logshark.Plugins.Filestore.Models
{
    internal class FilestoreEvent
    {
        [PrimaryKey]
        [AutoIncrement]
        public int Id { get; set; }

        public Guid LogsetHash { get; set; }

        [Index(Unique = true)]
        public Guid EventHash { get; set; }

        [Index]
        public DateTime Timestamp { get; set; }

        public string TimestampOffset { get; set; }

        [Index]
        public string Severity { get; set; }

        public string Message { get; set; }

        [Index]
        public string Class { get; set; }

        [Index]
        public int Worker { get; set; }

        public FilestoreEvent()
        {
        }

        public FilestoreEvent(BsonDocument logLine, Guid logsetHash)
        {
            LogsetHash = logsetHash;
            Timestamp = BsonDocumentHelper.GetDateTime("ts", logLine);
            TimestampOffset = BsonDocumentHelper.GetString("ts_offset", logLine);
            Severity = BsonDocumentHelper.GetString("sev", logLine);
            Message = BsonDocumentHelper.GetString("message", logLine);
            Class = BsonDocumentHelper.GetString("class", logLine);
            Worker = BsonDocumentHelper.GetInt("worker", logLine);
            EventHash = GetEventHash(logLine);
        }

        protected Guid GetEventHash(BsonDocument logLine)
        {
            string file = BsonDocumentHelper.GetString("file", logLine);
            int line = BsonDocumentHelper.GetInt("line", logLine);
            return HashHelper.GenerateHashGuid(Timestamp, TimestampOffset, Message, Worker, file, line);
        }
    }
}