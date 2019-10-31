using Logshark.PluginLib.Extensions;
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
            SetEventMetadata(document);

            BsonDocument values = BsonDocumentHelper.GetValuesStruct(document);
            KeyHash = values.GetString("key-hash");
            Outcome = values.GetString("outcome");
            KeySizeB = values.GetInt("key-size-b");
            ValueSizeB = values.GetInt("value-size-b");
            Cns = values.GetString("cns");
            ElapsedMs = values.GetNullableInt("elapsed-ms");
            LoadTimeMs = values.GetInt("load-time-ms");
            LowerBoundMs = values.GetInt("lower-bound-ms");
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
