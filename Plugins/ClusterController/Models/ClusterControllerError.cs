using Logshark.PluginLib.Extensions;
using MongoDB.Bson;

namespace Logshark.Plugins.ClusterController.Models
{
    public sealed class ClusterControllerError : BaseClusterControllerEvent
    {
        public string Severity { get; set; }

        public string Message { get; set; }

        public string Class { get; set; }

        public ClusterControllerError()
        {
        }

        public ClusterControllerError(BsonDocument document) : base(document)
        {
            Severity = document.GetString("sev");
            Message = document.GetString("message");
            Class = document.GetString("class");
        }
    }
}