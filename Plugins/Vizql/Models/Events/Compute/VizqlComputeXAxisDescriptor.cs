using Logshark.PluginLib.Helpers;
using MongoDB.Bson;

namespace Logshark.Plugins.Vizql.Models.Events.Compute
{
    public class VizqlComputeXAxisDescriptor : VizqlEvent
    {
        public double Elapsed { get; set; }

        public VizqlComputeXAxisDescriptor() { }

        public VizqlComputeXAxisDescriptor(BsonDocument document)
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
