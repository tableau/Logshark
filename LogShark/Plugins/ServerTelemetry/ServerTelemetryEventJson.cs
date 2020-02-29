using System;
using Newtonsoft.Json;

namespace LogShark.Plugins.ServerTelemetry
{
    public class ServerTelemetryEventJson
    {
        [JsonProperty(PropertyName = "ts")]
        public DateTime ActionTimeStamp { get; set; }

        [JsonProperty(PropertyName = "pid")]
        public int? ProcessId { get; set; }

        [JsonProperty(PropertyName = "tid")]
        public string ThreadId { get; set; }
        
        [JsonProperty(PropertyName = "sid")]
        public string SessionId { get; set; }
        
        [JsonProperty(PropertyName = "v")]
        public ServerTelemetryEventMessageJson Message { get; set; }
        
        public int? Worker { get; set; }

        public int? Process { get; set; }
    }
}