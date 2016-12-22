using Logshark.PluginLib.Helpers;
using MongoDB.Bson;

namespace Logshark.Plugins.Vizql.Models.Events.Render
{
    class VizqlEndDataInterpreter : VizqlEvent
    {
        public double? Elapsed { get; set; }

        public VizqlEndDataInterpreter(BsonDocument document)
        {
            ValidateArguments("end-data-interpreter", document);
            SetEventMetadata(document);
            Elapsed = BsonDocumentHelper.GetNullableDouble("elapsed", document);
        }

        public override double? GetElapsedTimeInSeconds()
        {
            return Elapsed;
        }
    }
}
