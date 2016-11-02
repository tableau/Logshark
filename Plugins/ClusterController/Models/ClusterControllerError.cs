using Logshark.PluginLib.Helpers;
using MongoDB.Bson;
using ServiceStack.DataAnnotations;
using System;

namespace Logshark.Plugins.ClusterController.Models
{
    public class ClusterControllerError : ClusterControllerEvent
    {
        [Index]
        public string Severity { get; set; }

        public string Message { get; set; }

        [Index]
        public string Class { get; set; }

        public ClusterControllerError()
        {
        }

        public ClusterControllerError(BsonDocument document, Guid logsetHash)
            : base(document, logsetHash)
        {
            Severity = BsonDocumentHelper.GetString("sev", document);
            Message = BsonDocumentHelper.GetString("message", document);
            Class = BsonDocumentHelper.GetString("class", document);
            EventHash = GetEventHash();
        }

        protected Guid GetEventHash()
        {
            return HashHelper.GenerateHashGuid(Timestamp, Message, Worker, Filename, LineNumber);
        }
    }
}