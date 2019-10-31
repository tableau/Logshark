using Logshark.PluginLib.Extensions;
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
            SetEventMetadata(document);
            BsonDocument values = BsonDocumentHelper.GetValuesStruct(document);

            Error = values.GetString("error");
            Cached = values.GetBool("cached");
            Success = values.GetBool("success");
            CacheHit = values.GetBool("cachehit");
            Elapsed = values.GetDouble("elapsed");
            Query = values.GetString("query");
        }

        public override double? GetElapsedTimeInSeconds()
        {
            return Elapsed;
        }
    }
}