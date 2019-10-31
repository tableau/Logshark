using Logshark.PluginLib.Extensions;
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
            SetEventMetadata(document);

            BsonDocument values = BsonDocumentHelper.GetValuesStruct(document);

            KeyHash = values.GetString("key-hash");
            Outcome = values.GetString("outcome");
            KeySizeB = values.GetNullableInt("key-size-b");
            ValueSizeB = values.GetNullableInt("value-size-b");
            QueryKind = values.GetString("query-kind");
            ElapsedMs = values.GetNullableInt("elapsed-ms");
            QueryLatencyMs = values.GetNullableInt("query-latency-ms");
            ColumnCount = values.GetNullableInt("column-count");
            RowCount = values.GetNullableInt("row-count");
        }

        public override double? GetElapsedTimeInSeconds()
        {
            if (!ElapsedMs.HasValue)
            {
                return null;
            }

            return (double) ElapsedMs / 1000;
        }
    }
}
