using Newtonsoft.Json;

namespace LogShark.Plugins.ServerTelemetry
{
    public class ServerTelemetryEventMessageJson
    {
        [JsonProperty(PropertyName = "device-pixel-ratio")]
        public double? DevicePixelRatio { get; set; }

        [JsonProperty(PropertyName = "dsd-device-type")]
        public string DsdDeviceType { get; set; }

        [JsonProperty(PropertyName = "sid")]
        public string SessionId { get; set; }
        
        [JsonProperty(PropertyName = "sitename")]
        public string SiteName { get; set; }
        
        [JsonProperty(PropertyName = "workbook-name")]
        public string WorkbookName { get; set; }
        
        [JsonProperty(PropertyName = "user-agent")]
        public string UserAgent { get; set; }
        
        [JsonProperty(PropertyName = "username")]
        public string UserName { get; set; }
        
        [JsonProperty(PropertyName = "request-info")]
        public ServerTelemetryEventRequestInfoJson RequestInfo { get; set; }
    }
}