using Logshark.PluginLib.Extensions;
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
            SetEventMetadata(document);

            BsonDocument values = BsonDocumentHelper.GetValuesStruct(document);
            Filename = values.GetString("filename");
            ProductName = values.GetString("product-name");
            FileVersion = values.GetString("file-version");
            ProductVersion = values.GetString("product-version");
        }
    }
}