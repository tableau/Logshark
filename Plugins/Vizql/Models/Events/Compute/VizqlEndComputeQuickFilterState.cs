using Logshark.PluginLib.Helpers;
using MongoDB.Bson;

namespace Logshark.Plugins.Vizql.Models.Events.Compute
{
    public class VizqlEndComputeQuickFilterState : VizqlEvent
    {
        public double Elapsed { get; set; }
        public string View { get; set; }
        public string Sheet { get; set; }

        public VizqlEndComputeQuickFilterState() { }

        public VizqlEndComputeQuickFilterState(BsonDocument document)
        {
            ValidateArguments("end-compute-quick-filter-state", document);
            SetEventMetadata(document);
            BsonDocument values = BsonDocumentHelper.GetValuesStruct(document);
            Elapsed = BsonDocumentHelper.GetDouble("elapsed", values);
            Sheet = BsonDocumentHelper.GetString("sheet", values);
            View = BsonDocumentHelper.GetString("view", values);
        }

        public override double? GetElapsedTimeInSeconds()
        {
            return Elapsed;
        }
    }
}
