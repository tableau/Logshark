using Logshark.PluginLib.Extensions;
using MongoDB.Bson;

namespace Logshark.Plugins.Vizql.Models.Events.Etc
{
    public class VizqlMessage : VizqlEvent
    {
        public string Message { get; set; }
        public int Line { get; set; }
        public string Sev { get; set; }

        public VizqlMessage() { }

        public VizqlMessage(BsonDocument document)
        {
            SetEventMetadata(document);

            Line = document.GetInt("line");
            Message = document.GetString("v");
            Sev = document.GetString("sev");
        }
    }
}