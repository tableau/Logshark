using Logshark.PluginLib.Helpers;
using MongoDB.Bson;
using ServiceStack.DataAnnotations;

namespace Logshark.Plugins.Vizql.Models.Events.Query
{
    public class VizqlQpBatchSummaryJob
    {
        [AutoIncrement]
        [PrimaryKey]
        public int Id { get; set; }

        [Index]
        public string QpBatchSummaryEventGuid { get; set; }
        public int QueryId { get; set; }
        public int? ProtocolId { get; set; }
        public double? Elapsed { get; set; }
        public string OwnerComponent { get; set; }
        public string OwnerDashboard { get; set; }
        public string OwnerWorksheet { get; set; }
        public string QueryAbstract { get; set; }
        public string QueryCompiled { get; set; }

        public VizqlQpBatchSummaryJob() { }

        public VizqlQpBatchSummaryJob(string qpBatchSummaryEventGuid, BsonDocument document)
        {
            QpBatchSummaryEventGuid = qpBatchSummaryEventGuid;
            QueryId = BsonDocumentHelper.GetInt("query-id", document);
            ProtocolId = BsonDocumentHelper.GetNullableInt("protocol-id", document);
            Elapsed = BsonDocumentHelper.GetNullableDouble("elapsed", document);
            OwnerComponent = BsonDocumentHelper.GetString("owner-component", document);
            OwnerDashboard = BsonDocumentHelper.GetString("owner-dashboard", document);
            OwnerWorksheet = BsonDocumentHelper.GetString("owner-worksheet", document);
            QueryAbstract = BsonDocumentHelper.TruncateString(BsonDocumentHelper.GetString("query-abstract", document), 1024);
            QueryCompiled = BsonDocumentHelper.TruncateString(BsonDocumentHelper.GetString("query-compiled", document), 1024);
        }
    }
}
