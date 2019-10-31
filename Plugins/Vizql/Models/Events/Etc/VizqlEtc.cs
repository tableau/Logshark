using Logshark.PluginLib.Extensions;
using Logshark.PluginLib.Helpers;
using MongoDB.Bson;

namespace Logshark.Plugins.Vizql.Models.Events.Etc
{
    public class VizqlEtc : VizqlEvent
    {
        public new string KeyType { get; set; }
        public string Value { get; set; }

        public VizqlEtc() { }

        public VizqlEtc(BsonDocument document)
        {
            SetEventMetadata(document);
            KeyType = document.GetString("k");
            try
            {
                BsonDocument values = BsonDocumentHelper.GetValuesStruct(document);
                Value = values.ToJson();
            }
            // If the values payload is actually a string instead of a struct we will throw, catch it and use the string here.
            catch
            {
                Value = document.GetString("v");
            }
        }
    }
}