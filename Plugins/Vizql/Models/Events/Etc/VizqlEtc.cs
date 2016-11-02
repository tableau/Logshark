using Logshark.PluginLib.Helpers;
using MongoDB.Bson;
using ServiceStack.DataAnnotations;

namespace Logshark.Plugins.Vizql.Models.Events.Etc
{
    public class VizqlEtc : VizqlEvent
    {
        [Index]
        public new string KeyType { get; set; }
        public string Value { get; set; }

        public VizqlEtc() { }

        public VizqlEtc(BsonDocument document)
        {
            SetEventMetadata(document);
            KeyType = BsonDocumentHelper.GetString("k", document);
            try
            {
                BsonDocument values = BsonDocumentHelper.GetValuesStruct(document);
                Value = values.ToJson();
            }
            //If the values payload is actually a string instead of a struct we will throw, catch it and use the string here.
            catch
            {
                Value = BsonDocumentHelper.GetString("v", document);
            }
        }
    }
}
