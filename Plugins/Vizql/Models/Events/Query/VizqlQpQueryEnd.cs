using Logshark.PluginLib.Extensions;
using Logshark.PluginLib.Helpers;
using MongoDB.Bson;

namespace Logshark.Plugins.Vizql.Models.Events.Query
{
    public class VizqlQpQueryEnd : VizqlEvent
    {
        public int QueryId { get; set; }
        public string OwnerDashboard { get; set; }
        public string OwnerWorksheet { get; set; }
        public string OwnerComponent { get; set; }
        public int? ProtocolId { get; set; }
        public string CacheHit { get; set; }
        public double? Elapsed { get; set; }

        public VizqlQpQueryEnd() { }

        public VizqlQpQueryEnd(BsonDocument document)
        {
            SetEventMetadata(document);
            BsonDocument values = BsonDocumentHelper.GetValuesStruct(document);

            QueryId = values.GetInt("query-id");
            OwnerDashboard = values.GetString("owner-dashboard");
            OwnerComponent = values.GetString("owner-component");
            OwnerWorksheet = values.GetString("owner-worksheet");
            CacheHit = values.GetString("cache-hit");
            ProtocolId = values.GetNullableInt("protocol-id");
            Elapsed = values.GetNullableDouble("elapsed");
        }

        public override double? GetElapsedTimeInSeconds()
        {
            return Elapsed;
        }
    }
}