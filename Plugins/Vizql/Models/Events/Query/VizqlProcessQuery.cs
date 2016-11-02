using Logshark.PluginLib.Helpers;
using MongoDB.Bson;

namespace Logshark.Plugins.Vizql.Models.Events.Query
{
    public class VizqlProcessQuery : VizqlEvent
    {
        public string Error { get; set; }
        public bool Cached { get; set; }
        public bool Success { get; set; }
        public bool CacheHit { get; set; }
        public double Elapsed { get; set; }
        public string Query { get; set; }

        public VizqlProcessQuery()
        {
        }

        public VizqlProcessQuery(BsonDocument document)
        {
            ValidateArguments("process_query", document);
            SetEventMetadata(document);
            BsonDocument values = BsonDocumentHelper.GetValuesStruct(document);

            Error = BsonDocumentHelper.GetString("error", values);
            Cached = BsonDocumentHelper.GetBool("cached", values);
            Success = BsonDocumentHelper.GetBool("success", values);
            CacheHit = BsonDocumentHelper.GetBool("cachehit", values);
            Elapsed = BsonDocumentHelper.GetDouble("elapsed", values);
            Query = BsonDocumentHelper.GetString("query", values);
        }

        public override double? GetElapsedTimeInSeconds()
        {
            return Elapsed;
        }
    }
}