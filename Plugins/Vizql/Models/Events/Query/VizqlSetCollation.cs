using Logshark.PluginLib.Extensions;
using Logshark.PluginLib.Helpers;
using MongoDB.Bson;

namespace Logshark.Plugins.Vizql.Models.Events.Query
{
    public class VizqlSetCollation : VizqlEvent
    {
        public string Column { get; set; }
        public string Collation { get; set; }

        public VizqlSetCollation() { }

        public VizqlSetCollation(BsonDocument document)
        {
            SetEventMetadata(document);
            BsonDocument values = BsonDocumentHelper.GetValuesStruct(document);

            Column = values.GetString("column");
            Collation = values.GetString("collation");
        }
    }
}