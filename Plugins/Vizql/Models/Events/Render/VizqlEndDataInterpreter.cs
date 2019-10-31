using Logshark.PluginLib.Extensions;
using MongoDB.Bson;

namespace Logshark.Plugins.Vizql.Models.Events.Render
{
    public class VizqlEndDataInterpreter : VizqlEvent
    {
        public double? Elapsed { get; set; }

        public VizqlEndDataInterpreter(BsonDocument document)
        {
            SetEventMetadata(document);
            Elapsed = document.GetNullableDouble("elapsed");
        }

        public override double? GetElapsedTimeInSeconds()
        {
            return Elapsed;
        }
    }
}