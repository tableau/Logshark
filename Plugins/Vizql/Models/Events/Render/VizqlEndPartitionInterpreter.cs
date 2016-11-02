using Logshark.PluginLib.Helpers;
using MongoDB.Bson;

namespace Logshark.Plugins.Vizql.Models.Events.Render
{
    class VizqlEndPartitionInterpreter : VizqlEvent
    {
        public double Elapsed { get; set; }

        public VizqlEndPartitionInterpreter(BsonDocument document)
        {
            ValidateArguments("end-partition-interpreter", document);
            SetEventMetadata(document);

            Elapsed = BsonDocumentHelper.GetDouble("elapsed", BsonDocumentHelper.GetValuesStruct(document));
        }

        public override double? GetElapsedTimeInSeconds()
        {
            return Elapsed;
        }
    }
}
