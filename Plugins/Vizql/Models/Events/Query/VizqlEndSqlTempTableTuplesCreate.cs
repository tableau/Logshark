using Logshark.PluginLib.Extensions;
using Logshark.PluginLib.Helpers;
using MongoDB.Bson;

namespace Logshark.Plugins.Vizql.Models.Events.Query
{
    public class VizqlEndSqlTempTableTuplesCreate : VizqlEvent
    {
        public double Elapsed { get; set; }
        public double ElapsedCreate { get; set; }
        public double ElapsedInsert { get; set; }
        public int ProtocolId { get; set; }
        public string TableName { get; set; }

        public VizqlEndSqlTempTableTuplesCreate() { }

        public VizqlEndSqlTempTableTuplesCreate(BsonDocument document)
        {
            SetEventMetadata(document);
            BsonDocument values = BsonDocumentHelper.GetValuesStruct(document);
            Elapsed = values.GetDouble("elapsed");
            ElapsedCreate = values.GetDouble("elapsed-create");
            ElapsedInsert = values.GetDouble("elapsed-insert");
            ProtocolId = values.GetInt("protocol-id");
            TableName = values.GetString("tablename");
        }

        public override double? GetElapsedTimeInSeconds()
        {
            return Elapsed;
        }
    }
}