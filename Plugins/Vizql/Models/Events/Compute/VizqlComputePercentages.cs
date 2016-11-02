using Logshark.PluginLib.Helpers;
using MongoDB.Bson;

namespace Logshark.Plugins.Vizql.Models.Events.Compute
{
    public class VizqlComputePercentages : VizqlEvent
    {
        public double Elapsed { get; set; }

        public VizqlComputePercentages() { }

        public VizqlComputePercentages(BsonDocument document)
        {
            ValidateArguments("compute-percentages", document);
            SetEventMetadata(document);
            Elapsed = BsonDocumentHelper.GetDouble("elapsed", BsonDocumentHelper.GetValuesStruct(document));
        }

        public override double? GetElapsedTimeInSeconds()
        {
            return Elapsed;
        }
    }
}
