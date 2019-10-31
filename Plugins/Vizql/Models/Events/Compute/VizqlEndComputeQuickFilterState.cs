using Logshark.PluginLib.Extensions;
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
            SetEventMetadata(document);
            BsonDocument values = BsonDocumentHelper.GetValuesStruct(document);
            Elapsed = values.GetDouble("elapsed");
            Sheet = values.GetString("sheet");
            View = values.GetString("view");
        }

        public override double? GetElapsedTimeInSeconds()
        {
            return Elapsed;
        }
    }
}
