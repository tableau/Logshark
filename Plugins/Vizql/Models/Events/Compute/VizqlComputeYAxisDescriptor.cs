using Logshark.PluginLib.Helpers;
using MongoDB.Bson;

namespace Logshark.Plugins.Vizql.Models.Events.Compute
{
    public class VizqlComputeYAxisDescriptor : VizqlEvent
    {
        public double Elapsed { get; set; }

        public VizqlComputeYAxisDescriptor() { }

        public VizqlComputeYAxisDescriptor(BsonDocument document)
        {
            ValidateArguments("compute-y-axis-descriptor", document);
            SetEventMetadata(document);
            Elapsed = BsonDocumentHelper.GetDouble("elapsed", BsonDocumentHelper.GetValuesStruct(document));
        }

        public override double? GetElapsedTimeInSeconds()
        {
            return Elapsed;
        }
    }
}
