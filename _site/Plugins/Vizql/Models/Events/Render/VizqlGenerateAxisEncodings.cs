using Logshark.PluginLib.Helpers;
using MongoDB.Bson;

namespace Logshark.Plugins.Vizql.Models.Events.Render
{
    public class VizqlGenerateAxisEncodings : VizqlEvent
    {
        public double Elapsed { get; set; }

        public VizqlGenerateAxisEncodings() { }

        public VizqlGenerateAxisEncodings(BsonDocument document)
        {
            ValidateArguments("generate-axis-encodings", document);
            SetEventMetadata(document);
            Elapsed = BsonDocumentHelper.GetDouble("elapsed", BsonDocumentHelper.GetValuesStruct(document));
        }

        public override double? GetElapsedTimeInSeconds()
        {
            return Elapsed;
        }
    }
}
