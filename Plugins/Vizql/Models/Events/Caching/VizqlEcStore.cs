using Logshark.PluginLib.Helpers;
using MongoDB.Bson;

namespace Logshark.Plugins.Vizql.Models.Events.Caching
{
    public class VizqlEcStore : VizqlEvent
    {
        public string KeyHash { get; private set; }
        public string Outcome { get; private set; }
        public int KeySizeB { get; private set; }
        public int ValueSizeB { get; private set; }
        public string Cns { get; private set; }
        public int? ElapsedMs { get; private set; }
        public int LoadTimeMs { get; private set; }
        public int LowerBoundMs { get; private set; }

        public VizqlEcStore() { }

        public VizqlEcStore(BsonDocument document)
        {
            ValidateArguments("ec-store", document);
            SetEventMetadata(document);

            BsonDocument values = BsonDocumentHelper.GetValuesStruct(document);
            KeyHash = BsonDocumentHelper.GetString("key-hash", values);
            Outcome = BsonDocumentHelper.GetString("outcome", values);
            KeySizeB = BsonDocumentHelper.GetInt("key-size-b", values);
            ValueSizeB = BsonDocumentHelper.GetInt("value-size-b", values);
            Cns = BsonDocumentHelper.GetString("cns", values);
            ElapsedMs = BsonDocumentHelper.GetNullableInt("elapsed-ms", values);
            LoadTimeMs = BsonDocumentHelper.GetInt("load-time-ms", values);
            LowerBoundMs = BsonDocumentHelper.GetInt("lower-bound-ms", values);
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
