using Logshark.PluginLib.Extensions;
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
            SetEventMetadata(document);

            BsonDocument values = BsonDocumentHelper.GetValuesStruct(document);
            InConstructionCount = values.GetNullableInt("in-construction-count");
            ClosedProtocolsCount = values.GetNullableInt("closed-protocols-count");
            ProtocolGroupId = values.GetNullableInt("group-id");
            ConnectionLimit = values.GetInt("connection-limit");
            Attributes = values.GetString("attributes");
        }
    }
}