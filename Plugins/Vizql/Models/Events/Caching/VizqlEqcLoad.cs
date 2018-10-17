using Logshark.PluginLib.Extensions;
using Logshark.PluginLib.Helpers;
using MongoDB.Bson;

namespace Logshark.Plugins.Vizql.Models.Events.Caching
{
    public class VizqlEqcLoad : VizqlEvent
    {
        public string KeyHash { get; private set; }
        public string Outcome { get; private set; }
        public string QueryKind { get; private set; }
        public int? ElapsedMs { get; private set; }

        public VizqlEqcLoad() { }

        public VizqlEqcLoad(BsonDocument document)
        {
            SetEventMetadata(document);

            BsonDocument values = BsonDocumentHelper.GetValuesStruct(document);

            KeyHash = values.GetString("key-hash");
            Outcome = values.GetString("outcome");

            if (document.Contains("query-kind"))
            {
                QueryKind = values.GetString("query-kind");
            }
            else if (document.Contains("kind"))
            {
                QueryKind = values.GetString("kind");
            }
            ElapsedMs = values.GetNullableInt("elapsed-ms");
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
