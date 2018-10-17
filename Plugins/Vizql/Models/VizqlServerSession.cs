using Logshark.PluginLib.Extensions;
using MongoDB.Bson;
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
        public string Worker { get; private set; }

        public DateTime? CreationTimestamp { get; set; }
        public DateTime? DestructionTimestamp { get; set; }

        public VizqlServerSession() { }
        
        public VizqlServerSession(BsonDocument firstEvent, BsonDocument lastEvent, string workbookName, string processName, string bootstrapRequestId)
        {
            VizqlSessionId = firstEvent.GetString("sess");
            BootstrapApacheRequestId = bootstrapRequestId;
            Username = firstEvent.GetString("user");
            Site = firstEvent.GetString("site");
            CreationTimestamp = firstEvent.GetDateTime("ts");
            DestructionTimestamp = lastEvent.GetDateTime("ts");
            File = firstEvent.GetString("file");
            Worker = firstEvent.GetString("worker");
            Workbook = workbookName;
            ProcessName = processName;

            CreateEventCollections();
        }
    }
}