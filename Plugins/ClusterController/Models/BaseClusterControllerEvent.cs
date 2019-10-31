using Logshark.PluginLib.Extensions;
using MongoDB.Bson;
using System;

namespace Logshark.Plugins.ClusterController.Models
{
    public abstract class BaseClusterControllerEvent
    {
        public DateTime Timestamp { get; set; }

        public string Worker { get; set; }
        public string FilePath { get; set; }
        public string File { get; set; }
        public int LineNumber { get; set; }

        protected BaseClusterControllerEvent()
        {
        }

        protected BaseClusterControllerEvent(BsonDocument document)
        {
            Timestamp = document.GetDateTime("ts");
            Worker = document.GetString("worker");
            FilePath = document.GetString("file_path");
            File = document.GetString("file");
            LineNumber = document.GetInt("line");
        }
    }
}