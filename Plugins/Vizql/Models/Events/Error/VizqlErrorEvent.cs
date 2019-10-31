using Logshark.PluginLib.Extensions;
using MongoDB.Bson;

namespace Logshark.Plugins.Vizql.Models.Events.Error
{
    public class VizqlErrorEvent : VizqlEvent
    {
        public string Message { get; set; }
        public string Severity { get; set; }

        public VizqlErrorEvent() { }

        public VizqlErrorEvent(BsonDocument document, string message)
        {
            SetEventMetadata(document);

            Message = message;
            Severity = document.GetValue("sev").AsString;
        }

        public VizqlErrorEvent(BsonDocument document)
            : this(document, document.GetString("v"))
        {
        }
    }
}