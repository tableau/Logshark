using Logshark.PluginLib.Extensions;
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
            SetEventMetadata(document);

            BsonDocument values = BsonDocumentHelper.GetValuesStruct(document);

            Elapsed = values.GetNullableDouble("created-elapsed");
            ProtocolId = values.GetNullableInt("id");
            ProtocolGroupId = values.GetNullableInt("group_id");

            BsonDocument attributes = BsonDocumentHelper.GetBsonDocument("attributes", values);
            if (attributes != null)
            {
                Class = attributes.GetString("class");
                DatabaseName = attributes.GetString("dbname");
                DatabaseServer = attributes.GetString("server");
                Attributes = attributes.ToString();
            }
        }

        public override double? GetElapsedTimeInSeconds()
        {
            return Elapsed;
        }
    }
}