using Logshark.PluginLib.Extensions;
using Logshark.PluginLib.Helpers;
using MongoDB.Bson;
using System;

namespace Logshark.Plugins.Vizql.Models.Events.Query
{
    public class VizqlEndQuery : VizqlEvent
    {
        public string Query { get; set; }
        public long? ProtocolId { get; set; }
        public int Cols { get; set; }
        public int Rows { get; set; }
        public long? QueryHash { get; set; }
        public double Elapsed { get; set; }

        public VizqlEndQuery() { }

        public VizqlEndQuery(BsonDocument document)
        {
            SetEventMetadata(document);
            BsonDocument values = BsonDocumentHelper.GetValuesStruct(document);

            Query = values.GetString("query-trunc") ?? values.GetString("query");
            ProtocolId = values.GetNullableLong("protocol-id");
            Cols = values.GetInt("cols");
            Rows = values.GetInt("rows");
            QueryHash = values.GetNullableLong("query-hash");
            Elapsed = values.GetDouble("elapsed");
        }

        /// <summary>
        /// The queries in the logs can be absolutely massive (> 100MB) so we may wish to truncate these to avoid memory or database bloat.
        /// </summary>
        public VizqlEndQuery WithTruncatedQueryText(int maxQueryLength)
        {
            if (maxQueryLength >= 0 && !String.IsNullOrEmpty(Query) && Query.Length > maxQueryLength)
            {
                Query = Query.Substring(0, maxQueryLength);
            }

            return this;
        }

        public override double? GetElapsedTimeInSeconds()
        {
            return Elapsed;
        }
    }
}