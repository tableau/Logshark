using Logshark.PluginLib.Helpers;
using MongoDB.Bson;

namespace Logshark.Plugins.Vizql.Models.Events.Render
{
    public class VizqlEndVisualInterpreter : VizqlEvent
    {
        public double Elapsed { get; set; }

        public VizqlEndVisualInterpreter() { }

        public VizqlEndVisualInterpreter(BsonDocument document)
        {
            ValidateArguments("end-visual-interpreter", document);
            SetEventMetadata(document);
            Elapsed = BsonDocumentHelper.GetDouble("elapsed", BsonDocumentHelper.GetValuesStruct(document));
        }

        public override double? GetElapsedTimeInSeconds()
        {
            return Elapsed;
        }
    }
}
