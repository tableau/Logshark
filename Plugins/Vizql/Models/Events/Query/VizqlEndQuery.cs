using Logshark.PluginLib.Helpers;
using MongoDB.Bson;

namespace Logshark.Plugins.Vizql.Models.Events.Query
{
    public class VizqlEndQuery : VizqlEvent
    {
        public string Query { get; set; }
        public long? ProtocolId { get; set; }
        public int Cols { get; set; }
        public int Rows { get; set; }
        public long? QueryHash { get; set; }
        public double Elapsed { get; set; }

        public VizqlEndQuery() { }

        public VizqlEndQuery(BsonDocument document)
        {
            ValidateArguments("end-query", document);

            SetEventMetadata(document);
            BsonDocument values = BsonDocumentHelper.GetValuesStruct(document);

            Query = BsonDocumentHelper.TruncateString(BsonDocumentHelper.GetString("query", values), 1024);
            ProtocolId = BsonDocumentHelper.GetNullableLong("protocol-id", values);
            Cols = BsonDocumentHelper.GetInt("cols", values);
            Rows = BsonDocumentHelper.GetInt("rows", values);
            QueryHash = BsonDocumentHelper.GetNullableLong("query-hash", values);
            Elapsed = BsonDocumentHelper.GetDouble("elapsed", values);
        }

        public override double? GetElapsedTimeInSeconds()
        {
            return Elapsed;
        }
    }
}
