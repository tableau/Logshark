using Logshark.PluginLib.Helpers;
using MongoDB.Bson;

namespace Logshark.Plugins.Vizql.Models.Events.Render
{
    public class VizqlEndPartitionInterpreter : VizqlEvent
    {
        public double Elapsed { get; set; }

        public VizqlEndPartitionInterpreter(BsonDocument document)
        {
            SetEventMetadata(document);

            Elapsed = BsonDocumentHelper.GetDouble("elapsed", BsonDocumentHelper.GetValuesStruct(document));
        }

        public override double? GetElapsedTimeInSeconds()
        {
            return Elapsed;
        }
    }
}