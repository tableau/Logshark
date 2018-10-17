using Logshark.PluginLib.Extensions;
using Logshark.PluginLib.Helpers;
using MongoDB.Bson;
using System;
using Tableau.ExtractApi.DataAttributes;

namespace Logshark.Plugins.Vizql.Models
{
    public abstract class VizqlEvent
    {
        public string VizqlSessionId { get; set; }
        public string KeyType { get; set; }
        public string ThreadId { get; set; }
        public int ProcessId { get; set; }
        public string ApacheRequestId { get; set; }
        public DateTime EventTimestamp { get; set; }
        public string Worker { get; set; }
        public string FilePath { get; set; }
        public string File { get; set; }
        public int LineNumber { get; set; }

        [ExtractIgnore]
        public BsonValue ValuePayload { get; set; }

        public void SetEventMetadata(BsonDocument document)
        {
            VizqlSessionId = document.GetString("sess");
            ThreadId = document.GetString("tid");
            ApacheRequestId = document.GetString("req");
            EventTimestamp = document.GetDateTime("ts");
            ProcessId = document.GetInt("pid");
            KeyType = BsonDocumentHelper.GetKeyType(document);
            Worker = document.GetString("worker");
            FilePath = document.GetString("file_path");
            File = document.GetString("file");
            LineNumber = document.GetInt("line");

            if (document.Contains("v"))
            {
                ValuePayload = document["v"];
            }       
        }

        public virtual double? GetElapsedTimeInSeconds()
        {
            return null;
        }
    }
}