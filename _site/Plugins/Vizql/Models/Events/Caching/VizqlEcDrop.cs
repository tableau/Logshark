using Logshark.PluginLib.Helpers;
using MongoDB.Bson;

namespace Logshark.Plugins.Vizql.Models.Events.Caching
{
    public class VizqlEcDrop : VizqlEvent
    {
        public string KeyHash { get; private set; }
        public string Outcome { get; private set; }
        public int KeySizeB { get; private set; }
        public string Cns { get; private set; }
        public int? ElapsedMs { get; private set; }

        public VizqlEcDrop() { }

        public VizqlEcDrop(BsonDocument document)
        {
            ValidateArguments("ec-drop", document);
            SetEventMetadata(document);

            BsonDocument values = BsonDocumentHelper.GetValuesStruct(document);

            KeyHash = BsonDocumentHelper.GetString("key-hash", values);
            Outcome = BsonDocumentHelper.GetString("outcome", values);
            KeySizeB = BsonDocumentHelper.GetInt("key-size-b", values);
            Cns = BsonDocumentHelper.GetString("cns", values);
            ElapsedMs = BsonDocumentHelper.GetNullableInt("elapsed-ms", values);
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
