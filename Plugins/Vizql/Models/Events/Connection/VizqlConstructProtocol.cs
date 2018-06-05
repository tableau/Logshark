using Logshark.PluginLib.Helpers;
using MongoDB.Bson;

namespace Logshark.Plugins.Vizql.Models.Events.Connection
{
    public class VizqlConstructProtocol : VizqlEvent
    {
        public string Class { get; set; }
        public string DatabaseName { get; set; }
        public string DatabaseServer { get; set; }
        public double? Elapsed { get; set; }
        public int? ProtocolId { get; set; }
        public int? ProtocolGroupId { get; set; }
        public string Attributes { get; set; }

        public VizqlConstructProtocol() { }

        public VizqlConstructProtocol(BsonDocument document)
        {
            ValidateArguments("construct-protocol", document);
            SetEventMetadata(document);

            BsonDocument values = BsonDocumentHelper.GetValuesStruct(document);

            Elapsed = BsonDocumentHelper.GetNullableDouble("created-elapsed", values);
            ProtocolId = BsonDocumentHelper.GetNullableInt("id", values);
            ProtocolGroupId = BsonDocumentHelper.GetNullableInt("group_id", values);

            BsonDocument attributes = BsonDocumentHelper.GetBsonDocument("attributes", values);
            if (attributes != null)
            {
                Class = BsonDocumentHelper.GetString("class", attributes);
                DatabaseName = BsonDocumentHelper.GetString("dbname", attributes);
                DatabaseServer = BsonDocumentHelper.GetString("server", attributes);
                Attributes = attributes.ToString();
            }
        }

        public override double? GetElapsedTimeInSeconds()
        {
            return Elapsed;
        }
    }
}
