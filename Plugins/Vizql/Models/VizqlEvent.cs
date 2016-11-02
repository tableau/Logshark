using Logshark.PluginLib.Helpers;
using MongoDB.Bson;
using ServiceStack.DataAnnotations;
using System;

namespace Logshark.Plugins.Vizql.Models
{
    public abstract class VizqlEvent
    {
        [AutoIncrement]
        [PrimaryKey]
        public int Id { get; set; }

        [Index]
        public string VizqlSessionId { get; set; }

        public string KeyType { get; set; }

        public string ThreadId { get; set; }

        public int ProcessId { get; set; }

        [Index]
        public string ApacheRequestId { get; set; }

        [Index]
        public DateTime EventTimestamp { get; set; }

        [Index(Unique = true)]
        public Guid? EventHash { get; set; }

        public void SetEventMetadata(BsonDocument document)
        {
            VizqlSessionId = BsonDocumentHelper.GetString("sess", document);
            ThreadId = BsonDocumentHelper.GetString("tid", document);
            ApacheRequestId = BsonDocumentHelper.GetString("req", document);
            EventTimestamp = BsonDocumentHelper.GetDateTime("ts", document);
            ProcessId = BsonDocumentHelper.GetInt("pid", document);
            KeyType = BsonDocumentHelper.GetKeyType(document);
            EventHash = HashHelper.GenerateHashGuid(document.ToString());
        }

        protected void ValidateArguments(string expectedKeyType, BsonDocument document)
        {
            if (BsonDocumentHelper.GetKeyType(document) != expectedKeyType)
            {
                throw new ArgumentException("Logline key not of type " + expectedKeyType + "!");
            }
        }

        public virtual double? GetElapsedTimeInSeconds()
        {
            return null;
        }
    }
}