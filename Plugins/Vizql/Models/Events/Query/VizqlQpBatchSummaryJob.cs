using Logshark.PluginLib.Extensions;
using MongoDB.Bson;
using System;

namespace Logshark.Plugins.Vizql.Models.Events.Query
{
    public class VizqlQpBatchSummaryJob
    {
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

            QueryId = document.GetInt("query-id");
            ProtocolId = document.GetNullableInt("protocol-id");
            Elapsed = document.GetNullableDouble("elapsed");
            OwnerComponent = document.GetString("owner-component");
            OwnerDashboard = document.GetString("owner-dashboard");
            OwnerWorksheet = document.GetString("owner-worksheet");
            QueryAbstract = document.GetString("query-abstract");
            QueryCompiled = document.GetString("query-compiled");
            CacheHit = document.GetString("cache-hit");
            FusionParent = document.GetNullableInt("fusion-parent");
            Exception = document.GetString("exception");
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