using MongoDB.Bson;
using Logshark.PluginLib.Helpers;

namespace Logshark.Plugins.Vizql.Models.Events
{
    public class VizqlEndBootstrapSession : VizqlEvent
    {
        public double Elapsed { get; set; }

        public VizqlEndBootstrapSession() { }

        public VizqlEndBootstrapSession(BsonDocument document)
        {
            ValidateArguments("end-bootstrap-session", document);
            SetEventMetadata(document);
            Elapsed = BsonDocumentHelper.GetDouble("elapsed", BsonDocumentHelper.GetValuesStruct(document));
        }

        public override double? GetElapsedTimeInSeconds()
        {
            return Elapsed;
        }
    }
}
