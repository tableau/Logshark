using Logshark.PluginLib.Helpers;
using MongoDB.Bson;
using ServiceStack.DataAnnotations;
using System;

namespace Logshark.Plugins.Vizql.Models
{
    public class VizqlServerSession : VizqlSession
    {
        public string ProcessName { get; private set; }
        public string Username { get; private set; }
        public string BootstrapApacheRequestId { get; private set; }
        public string Workbook { get; private set; }
        public string Site { get; private set; }
        public string Hostname { get; set; }
        public string File { get; private set; }
        public int? Worker { get; private set; }

        [Index]
        public DateTime? CreationTimestamp { get; set; }

        [Index]
        public DateTime? DestructionTimestamp { get; set; }

        public VizqlServerSession(BsonDocument firstEvent, BsonDocument lastEvent, string workbookName, string processName, string bootstrapRequestId, Guid logsetHash)
        {
            VizqlSessionId = BsonDocumentHelper.GetString("sess", firstEvent);
            LogsetHash = logsetHash;
            BootstrapApacheRequestId = bootstrapRequestId;
            Username = BsonDocumentHelper.GetString("user", firstEvent);
            Site = BsonDocumentHelper.GetString("site", firstEvent);
            CreationTimestamp = BsonDocumentHelper.GetDateTime("ts", firstEvent);
            DestructionTimestamp = BsonDocumentHelper.GetDateTime("ts", lastEvent);
            File = BsonDocumentHelper.GetString("file", firstEvent);
            Worker = BsonDocumentHelper.GetInt("worker", firstEvent);
            Workbook = workbookName;
            ProcessName = processName;

            CreateEventCollections();
        }

        //Required for ORMLite.
        public VizqlServerSession() { }
    }
}