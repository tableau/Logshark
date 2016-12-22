using Logshark.PluginLib.Helpers;
using MongoDB.Bson;
using ServiceStack.DataAnnotations;
using System;

namespace Logshark.Plugins.ClusterController.Models
{
    public class ZookeeperError : ClusterControllerEvent
    {
        [Index]
        public string Severity { get; set; }

        public string Message { get; set; }

        [Index]
        public string Class { get; set; }

        public ZookeeperError()
        {
        }

        public ZookeeperError(BsonDocument document, Guid logsetHash)
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