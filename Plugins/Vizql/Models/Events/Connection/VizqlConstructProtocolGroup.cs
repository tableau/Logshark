using Logshark.PluginLib.Helpers;
using MongoDB.Bson;

namespace Logshark.Plugins.Vizql.Models.Events.Connection
{
    public class VizqlConstructProtocolGroup : VizqlEvent
    {
        public int? InConstructionCount { get; set; }
        public int? ClosedProtocolsCount { get; set; }
        public int? ProtocolGroupId { get; set; }
        public int ConnectionLimit { get; set; }
        public int ProtocolsCount { get; set; }
        public string Attributes { get; set; }

        public VizqlConstructProtocolGroup() { }

        public VizqlConstructProtocolGroup(BsonDocument document)
        {
            ValidateArguments("construct-protocol-group", document);
            SetEventMetadata(document);

            BsonDocument values = BsonDocumentHelper.GetValuesStruct(document);
            InConstructionCount = BsonDocumentHelper.GetNullableInt("in-construction-count", values);
            ClosedProtocolsCount = BsonDocumentHelper.GetNullableInt("closed-protocols-count", values);
            ProtocolGroupId = BsonDocumentHelper.GetNullableInt("group-id", values);
            ConnectionLimit = BsonDocumentHelper.GetInt("connection-limit", values);
            Attributes = BsonDocumentHelper.GetString("attributes", values);
        }
    }
}
 