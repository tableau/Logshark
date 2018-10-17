using Logshark.PluginLib.Helpers;
using MongoDB.Bson;

namespace Logshark.Plugins.Vizql.Models.Events.Connection
{
    public class VizqlDsConnect : VizqlEvent
    {
        public string Class { get; set; }
        public string Name { get; set; }
        public string Server { get; set; }
        public int? Port { get; set; }
        public string Database { get; set; }
        public string Tablename { get; set; }
        public string Username { get; set; }

        public VizqlDsConnect()
        {
        }

        public VizqlDsConnect(BsonDocument document)
        {
            SetEventMetadata(document);
            BsonDocument values = BsonDocumentHelper.GetValuesStruct(document);
            Name = BsonDocumentHelper.GetString("name", values);

            BsonDocument attr = BsonDocumentHelper.GetBsonDocument("attr", values);
            Class = BsonDocumentHelper.GetString("class", attr);
            Server = BsonDocumentHelper.GetString("server", attr);
            Port = BsonDocumentHelper.GetNullableInt("port", attr);
            Database = BsonDocumentHelper.GetString("dbname", attr);
            Tablename = BsonDocumentHelper.GetString("tablename", attr);
            Username = BsonDocumentHelper.GetString("username", attr);
        }
    }
}