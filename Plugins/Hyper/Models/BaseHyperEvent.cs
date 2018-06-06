using Logshark.PluginLib.Helpers;
using MongoDB.Bson.Serialization.Attributes;
using ServiceStack.DataAnnotations;
using System;

namespace Logshark.Plugins.Hyper.Models
{
    [BsonIgnoreExtraElements]
    public abstract class BaseHyperEvent
    {
        private Guid? eventHash;

        #region Identity Fields & Constraints

        [PrimaryKey]
        [AutoIncrement]
        public int Id { get; set; }

        [Index(Unique = true)]
        public Guid EventHash
        {
            get
            {
                if (eventHash == null)
                {
                    eventHash = HashHelper.GenerateHashGuid(Timestamp, Worker, FileName, Line);
                }

                return eventHash.Value;
            }
            set
            {
                eventHash = value;
            }
        }

        [BsonId]
        [Ignore]
        public string MongoId { get; set; }

        #endregion Identity Fields & Constraints

        [BsonElement("ts")]
        [Index]
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
        [Index]
        public string Key { get; set; }

        #region Metadata Fields

        [BsonIgnoreIfNull]
        public Guid LogsetHash { get; set; }

        [BsonElement("worker")]
        [Index]
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