using System;
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

        public string VizqlSessionId { get; set; }
        public int QueryId { get; set; }
        public int? ProtocolId { get; set; }
        public double? Elapsed { get; set; }
        public string OwnerComponent { get; set; }
        public string OwnerDashboard { get; set; }
        public string OwnerWorksheet { get; set; }
        public string QueryAbstract { get; set; }
        public string QueryCompiled { get; set; }
        public string CacheHit { get; set; }
        public string Exception { get; set; }
        public int? FusionParent { get; set; }

        public VizqlQpBatchSummaryJob()
        {
        }

        public VizqlQpBatchSummaryJob(string qpBatchSummaryEventGuid, string vizqlSessionId, BsonDocument document)
        {
            QpBatchSummaryEventGuid = qpBatchSummaryEventGuid;
            VizqlSessionId = vizqlSessionId;

            QueryId = BsonDocumentHelper.GetInt("query-id", document);
            ProtocolId = BsonDocumentHelper.GetNullableInt("protocol-id", document);
            Elapsed = BsonDocumentHelper.GetNullableDouble("elapsed", document);
            OwnerComponent = BsonDocumentHelper.GetString("owner-component", document);
            OwnerDashboard = BsonDocumentHelper.GetString("owner-dashboard", document);
            OwnerWorksheet = BsonDocumentHelper.GetString("owner-worksheet", document);
            QueryAbstract = BsonDocumentHelper.GetString("query-abstract", document);
            QueryCompiled = BsonDocumentHelper.GetString("query-compiled", document);
            CacheHit = BsonDocumentHelper.GetString("cache-hit", document);
            FusionParent = BsonDocumentHelper.GetNullableInt("fusion-parent", document);
            Exception = BsonDocumentHelper.GetString("exception", document);
        }

        /// <summary>
        /// The queries in the logs can be absolutely massive (> 100MB) so we may wish to truncate these to avoid memory or database bloat.
        /// </summary>
        public VizqlQpBatchSummaryJob WithTruncatedQueryText(int maxQueryLength)
        {
            if (maxQueryLength >= 0)
            {
                if (!String.IsNullOrEmpty(QueryAbstract) && QueryAbstract.Length > maxQueryLength)
                {
                    QueryAbstract = QueryAbstract.Substring(0, maxQueryLength);
                }
                if (!String.IsNullOrEmpty(QueryCompiled) && QueryCompiled.Length > maxQueryLength)
                {
                    QueryCompiled = QueryCompiled.Substring(0, maxQueryLength);
                }
            }

            return this;
        }
    }
}