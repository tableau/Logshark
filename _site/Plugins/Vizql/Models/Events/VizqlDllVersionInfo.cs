using Logshark.PluginLib.Helpers;
using MongoDB.Bson;

namespace Logshark.Plugins.Vizql.Models.Events
{
    public class VizqlDllVersionInfo : VizqlEvent
    {
        public string Filename { get; set; }
        public string ProductName { get; set; }
        public string FileVersion { get; set; }
        public string ProductVersion { get; set; }

        public VizqlDllVersionInfo()
        {
        }

        public VizqlDllVersionInfo(BsonDocument document)
        {
            ValidateArguments("dll-version-info", document);
            SetEventMetadata(document);

            BsonDocument values = BsonDocumentHelper.GetValuesStruct(document);
            Filename = BsonDocumentHelper.GetString("filename", values);
            ProductName = BsonDocumentHelper.GetString("product-name", values);
            FileVersion = BsonDocumentHelper.GetString("file-version", values);
            ProductVersion = BsonDocumentHelper.GetString("product-version", values);
        }
    }
}