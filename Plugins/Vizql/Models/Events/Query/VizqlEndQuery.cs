using System;
using Logshark.PluginLib.Helpers;
using MongoDB.Bson;

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
            ValidateArguments("end-query", document);

            SetEventMetadata(document);
            BsonDocument values = BsonDocumentHelper.GetValuesStruct(document);

            Query = BsonDocumentHelper.GetString("query", values);
            ProtocolId = BsonDocumentHelper.GetNullableLong("protocol-id", values);
            Cols = BsonDocumentHelper.GetInt("cols", values);
            Rows = BsonDocumentHelper.GetInt("rows", values);
            QueryHash = BsonDocumentHelper.GetNullableLong("query-hash", values);
            Elapsed = BsonDocumentHelper.GetDouble("elapsed", values);
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
