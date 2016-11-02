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
            ValidateArguments("qp-query-end", document);
            SetEventMetadata(document);

            BsonDocument values = BsonDocumentHelper.GetValuesStruct(document);
            QueryId = BsonDocumentHelper.GetInt("query-id", values);
            OwnerDashboard = BsonDocumentHelper.GetString("owner-dashboard", values);
            OwnerComponent = BsonDocumentHelper.GetString("owner-component", values);
            OwnerWorksheet = BsonDocumentHelper.GetString("owner-worksheet", values);
            CacheHit = BsonDocumentHelper.GetString("cache-hit", values);
            ProtocolId = BsonDocumentHelper.GetNullableInt("protocol-id", values);
            Elapsed = BsonDocumentHelper.GetNullableDouble("elapsed", values);
        }

        public override double? GetElapsedTimeInSeconds()
        {
            return Elapsed;
        }

    }
}
