using Logshark.PluginLib.Helpers;
using MongoDB.Bson;

namespace Logshark.Plugins.Vizql.Models.Events.Etc
{
    public class VizqlMessage : VizqlEvent
    {
        public string Message { get; set; }
        public int Line { get; set; }
        public string Sev { get; set; }

        public VizqlMessage()
        {
        }

        public VizqlMessage(BsonDocument document)
        {
            ValidateArguments("msg", document);
            SetEventMetadata(document);

            Line = BsonDocumentHelper.GetInt("line", document);
            Message = BsonDocumentHelper.GetString("v", document);
            Sev = BsonDocumentHelper.GetString("sev", document);
        }
    }
}