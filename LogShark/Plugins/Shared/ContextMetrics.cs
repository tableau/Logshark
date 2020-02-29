using Newtonsoft.Json;

namespace LogShark.Plugins.Shared
{
    public class ContextMetrics
    {
        [JsonProperty(PropertyName = "vw")]
        public string View { get; set; }

        [JsonProperty(PropertyName = "wb")]
        public string Workbook { get; set; }
    }
}
