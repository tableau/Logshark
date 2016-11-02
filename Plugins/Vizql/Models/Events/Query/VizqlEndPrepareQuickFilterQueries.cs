using Logshark.PluginLib.Helpers;
using MongoDB.Bson;

namespace Logshark.Plugins.Vizql.Models.Events.Query
{
    public class VizqlEndPrepareQuickFilterQueries : VizqlEvent
    {
        public double Elapsed { get; set; }
        public string Sheet { get; set; }
        public string View { get; set; }

        public VizqlEndPrepareQuickFilterQueries() { }

        public VizqlEndPrepareQuickFilterQueries(BsonDocument document)
        {
            ValidateArguments("end-prepare-quick-filter-queries", document);
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
