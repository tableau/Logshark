using Logshark.PluginLib.Helpers;
using MongoDB.Bson;

namespace Logshark.Plugins.Vizql.Models.Events.Query
{
    public class VizqlSetCollation : VizqlEvent
    {
        public string Column { get; set; }
        public string Collation { get; set; }

        public VizqlSetCollation()
        {
        }

        public VizqlSetCollation(BsonDocument document)
        {
            ValidateArguments("set-collation", document);
            SetEventMetadata(document);
            BsonDocument values = BsonDocumentHelper.GetValuesStruct(document);

            Column = BsonDocumentHelper.GetString("column", values);
            Collation = BsonDocumentHelper.GetString("collation", values);
        }
    }
}