using Logshark.PluginLib.Helpers;
using MongoDB.Bson;

namespace Logshark.Plugins.Vizql.Models.Events.Caching
{
    public class VizqlEqcStore : VizqlEvent
    {
        public string KeyHash { get; private set; }
        public string Outcome { get; private set; }
        public int? KeySizeB { get; private set; }
        public int? ValueSizeB { get; private set; }
        public string QueryKind { get; private set; }
        public int? ElapsedMs { get; private set; }
        public int? QueryLatencyMs { get; private set; }
        public int? ColumnCount { get; private set; }
        public int? RowCount { get; private set; }

        public VizqlEqcStore() { }

        public VizqlEqcStore(BsonDocument document)
        {
            ValidateArguments("eqc-store", document);
            SetEventMetadata(document);

            BsonDocument values = BsonDocumentHelper.GetValuesStruct(document);

            KeyHash = BsonDocumentHelper.GetString("key-hash", values);
            Outcome = BsonDocumentHelper.GetString("outcome", values);
            KeySizeB = BsonDocumentHelper.GetNullableInt("key-size-b", values);
            ValueSizeB = BsonDocumentHelper.GetNullableInt("value-size-b", values);
            QueryKind = BsonDocumentHelper.GetString("query-kind", values);
            ElapsedMs = BsonDocumentHelper.GetNullableInt("elapsed-ms", values);
            QueryLatencyMs = BsonDocumentHelper.GetNullableInt("query-latency-ms", values);
            ColumnCount = BsonDocumentHelper.GetNullableInt("column-count", values);
            RowCount = BsonDocumentHelper.GetNullableInt("row-count", values);
        }

        public override double? GetElapsedTimeInSeconds()
        {
            if (ElapsedMs.HasValue)
            {
                return (double)ElapsedMs/1000;
            }
            else
            {
                return null;
            }
        }
    }
}
