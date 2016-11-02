using Logshark.PluginLib.Helpers;
using MongoDB.Bson;

namespace Logshark.Plugins.Vizql.Models.Events.Caching
{
    public class VizqlEqcLoad : VizqlEvent
    {
        public string KeyHash { get; private set; }
        public string Outcome { get; private set; }
        public string QueryKind { get; private set; }
        public int ElapsedMs { get; private set; }

        public VizqlEqcLoad() { }

        public VizqlEqcLoad(BsonDocument document)
        {
            ValidateArguments("eqc-load", document);
            SetEventMetadata(document);

            BsonDocument values = BsonDocumentHelper.GetValuesStruct(document);

            KeyHash = BsonDocumentHelper.GetString("key-hash", values);
            Outcome = BsonDocumentHelper.GetString("outcome", values);
            QueryKind = BsonDocumentHelper.GetString("query-kind", values);
            ElapsedMs = BsonDocumentHelper.GetInt("elapsed-ms", values);
        }

        public override double? GetElapsedTimeInSeconds()
        {
            return (double)ElapsedMs/1000;
        }
    }
}
