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
            ValidateArguments("end-sql-temp-table-tuples-create", document);
            SetEventMetadata(document);
            BsonDocument values = BsonDocumentHelper.GetValuesStruct(document);
            Elapsed = BsonDocumentHelper.GetDouble("elapsed", values);
            ElapsedCreate = BsonDocumentHelper.GetDouble("elapsed-create", values);
            ElapsedInsert = BsonDocumentHelper.GetDouble("elapsed-insert", values);
            ProtocolId = BsonDocumentHelper.GetInt("protocol-id", values);
            TableName = BsonDocumentHelper.GetString("tablename", values);
        }

        public override double? GetElapsedTimeInSeconds()
        {
            return Elapsed;
        }
    }
}
