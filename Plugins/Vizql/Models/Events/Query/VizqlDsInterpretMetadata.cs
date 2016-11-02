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

        public VizqlDsInterpretMetadata()
        {
        }

        public VizqlDsInterpretMetadata(BsonDocument document)
        {
            ValidateArguments("ds-interpret-metadata", document);
            SetEventMetadata(document);
            BsonDocument values = BsonDocumentHelper.GetValuesStruct(document);

            HasNull = BsonDocumentHelper.GetNullableBool("has-null", values);
            Precision = BsonDocumentHelper.GetNullableInt("precision", values);
            Scale = BsonDocumentHelper.GetNullableInt("scale", values);
            RemoteType = BsonDocumentHelper.GetString("remote-type", values);
            Name = BsonDocumentHelper.GetString("name", values);
            Collation = BsonDocumentHelper.GetString("collation", values);
        }
    }
}