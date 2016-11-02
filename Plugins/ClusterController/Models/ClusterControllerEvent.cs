using Logshark.PluginLib.Helpers;
using MongoDB.Bson;
using ServiceStack.DataAnnotations;
using System;

namespace Logshark.Plugins.ClusterController.Models
{
    public class ClusterControllerEvent
    {
        [PrimaryKey]
        [AutoIncrement]
        public int Id { get; set; }

        public Guid LogsetHash { get; set; }

        [Index(Unique = true)]
        public Guid EventHash { get; set; }

        [Index]
        public DateTime Timestamp { get; set; }

        [Index]
        public int Worker { get; set; }

        public string Filename { get; set; }
        public int LineNumber { get; set; }

        public ClusterControllerEvent()
        {
        }

        public ClusterControllerEvent(BsonDocument document, Guid logsetHash)
        {
            LogsetHash = logsetHash;
            Timestamp = BsonDocumentHelper.GetDateTime("ts", document);
            Worker = BsonDocumentHelper.GetInt("worker", document);
            Filename = String.Format(@"{0}\{1}", BsonDocumentHelper.GetString("file_path", document), BsonDocumentHelper.GetString("file", document));
            LineNumber = BsonDocumentHelper.GetInt("line", document);
        }
    }
}