using Logshark.PluginLib.Extensions;
using Logshark.PluginLib.Helpers;
using MongoDB.Bson;

namespace Logshark.Plugins.Vizql.Models.Events.Query
{
    public class VizqlDsInterpretMetadata : VizqlEvent
    {
        public bool? HasNull { get; set; }
        public int? Precision { get; set; }
        public int? Scale { get; set; }
        public string RemoteType { get; set; }
        public string Name { get; set; }
        public string Collation { get; set; }

        public VizqlDsInterpretMetadata() { }

        public VizqlDsInterpretMetadata(BsonDocument document)
        {
            SetEventMetadata(document);
            BsonDocument values = BsonDocumentHelper.GetValuesStruct(document);

            HasNull = values.GetNullableBool("has-null");
            Precision = values.GetNullableInt("precision");
            Scale = values.GetNullableInt("scale");
            RemoteType = values.GetString("remote-type");
            Name = values.GetString("name");
            Collation = values.GetString("collation");
        }
    }
}