using System;
using Logshark.PluginLib.Helpers;
using MongoDB.Bson;
using ServiceStack.DataAnnotations;

namespace Logshark.Plugins.Vizportal.Models
{
    public class VizportalEvent
    {
        [PrimaryKey]
        [AutoIncrement]
        public int Id { get; set; }

        public Guid LogsetHash { get; set; }

        [Index(Unique = true)]
        public Guid EventHash { get; set; }

        [Index]
        public string RequestId { get; set; }

        [Index]
        public DateTime Timestamp { get; set; }

        public string Worker { get; set; }
        public string User { get; set; }

        [Index]
        public string SessionId { get; set; }

        public string Site { get; set; }

        [Index]
        public string Severity { get; set; }

        public string Class { get; set; }
        public string Message { get; set; }

        public VizportalEvent()
        {
        }

        public VizportalEvent(BsonDocument logLine, Guid logsetHash)
        {
            LogsetHash = logsetHash;
            RequestId = BsonDocumentHelper.GetString("req", logLine);
            Timestamp = BsonDocumentHelper.GetDateTime("ts", logLine);
            Worker = BsonDocumentHelper.GetString("worker", logLine);
            User = BsonDocumentHelper.GetString("user", logLine);
            SessionId = BsonDocumentHelper.GetString("sess", logLine);
            Site = BsonDocumentHelper.GetString("site", logLine);
            Severity = BsonDocumentHelper.GetString("sev", logLine);
            Class = BsonDocumentHelper.GetString("class", logLine);
            Message = BsonDocumentHelper.GetString("message", logLine);
            EventHash = GetEventHash(logLine);
        }

        protected Guid GetEventHash(BsonDocument logLine)
        {
            return HashHelper.GenerateHashGuid(Site, User, RequestId, Message, Timestamp, Worker, SessionId);
        }
    }
}
