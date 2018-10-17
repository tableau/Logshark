using Logshark.PluginLib.Helpers;
using MongoDB.Bson;

namespace Logshark.Plugins.Vizql.Models.Events.Render
{
    public class VizqlEndPreparePrimaryMappingTable : VizqlEvent
    {
        public double Elapsed { get; set; }

        public VizqlEndPreparePrimaryMappingTable() { }

        public VizqlEndPreparePrimaryMappingTable(BsonDocument document)
        {
            SetEventMetadata(document);
            Elapsed = BsonDocumentHelper.GetDouble("elapsed", BsonDocumentHelper.GetValuesStruct(document));
        }

        public override double? GetElapsedTimeInSeconds()
        {
            return Elapsed;
        }
    }
}