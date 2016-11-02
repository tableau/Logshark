using Logshark.PluginLib.Helpers;
using MongoDB.Bson;

namespace Logshark.Plugins.Vizql.Models.Events.Compute
{
    public class VizqlComputeYSetInterp : VizqlEvent
    {
        public double Elapsed { get; set; }

        public VizqlComputeYSetInterp() { }

        public VizqlComputeYSetInterp(BsonDocument document)
        {
            ValidateArguments("compute-y-set-interp", document);
            SetEventMetadata(document);
            Elapsed = BsonDocumentHelper.GetDouble("elapsed", BsonDocumentHelper.GetValuesStruct(document));
        }

        public override double? GetElapsedTimeInSeconds()
        {
            return Elapsed;
        }
    }
}
