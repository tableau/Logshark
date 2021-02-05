using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LogShark.Shared.LogReading.Containers
{
    public class NativeJsonLogsBaseEvent
    {
        [JsonProperty(PropertyName = "a")]
        public JToken ArtData { get; set; }
        
        [JsonProperty(PropertyName = "ctx")]
        public ContextMetrics ContextMetrics { get; set; }
        
        [JsonProperty(PropertyName = "req")]
        public string RequestId { get; set; }
        
        [JsonProperty(PropertyName = "k")]
        public string EventType { get; set; }

        [JsonProperty(PropertyName = "v")]
        public JToken EventPayload { get; set; }
        
        [JsonProperty(PropertyName ="pid")]
        public int ProcessId { get; set; }
        
        [JsonProperty(PropertyName = "sev")]
        public string Severity { get; set; }
        
        [JsonProperty(PropertyName = "site")]
        public string Site { get; set; }
        
        [JsonProperty(PropertyName = "tid")]
        public string ThreadId { get; set; }
        
        [JsonProperty(PropertyName = "ts")]
        public DateTime Timestamp { get; set; }
        
        [JsonProperty(PropertyName = "user")]
        public string Username { get; set; }

        [JsonProperty(PropertyName = "sess")]
        public string SessionId { get; set; }
    }
}