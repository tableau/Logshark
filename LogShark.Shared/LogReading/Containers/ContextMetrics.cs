using Newtonsoft.Json;

namespace LogShark.Shared.LogReading.Containers
{
    public class ContextMetrics
    {
        [JsonProperty(PropertyName = "client-type")]
        public string ClientType { get; set; }
        
        [JsonProperty(PropertyName = "client-procid")]
        public string ClientProcessId { get; set; }
        
        [JsonProperty(PropertyName = "procid")]
        private string ClientProcessIdAlternative
        {
            set => ClientProcessId = value;
        }
        
        [JsonProperty(PropertyName = "req")]
        public string ClientRequestId { get; set; }

        [JsonProperty(PropertyName = "client-request-id")]
        public string ClientRequestIdAlternative1
        {
            set => ClientRequestId = value;
        }

        [JsonProperty(PropertyName = "requestID")]
        private string ClientRequestIdAlternative2
        {
            set => ClientRequestId = value;
        }
        
        [JsonProperty(PropertyName = "sess")]
        public string ClientSessionId { get; set; }
        
        [JsonProperty(PropertyName = "client-session-id")]
        public string ClientSessionIdAlternative1
        {
            set => ClientSessionId = value;
        }

        [JsonProperty(PropertyName = "sessionid")]
        private string ClientSessionIdAlternative2
        {
            set => ClientSessionId = value;
        }

        [JsonProperty(PropertyName = "client-tid")]
        public string ClientThreadId { get; set; }

        [JsonProperty(PropertyName = "tid")]
        private string ClientThreadIdAlternative
        {
            set => ClientThreadId = value;
        }
        
        [JsonProperty(PropertyName = "client-user")]
        public string ClientUsername { get; set; }

        [JsonProperty(PropertyName = "username")]
        private string ClientUsernameAlternative
        {
            set => ClientUsername = value;
        }
        
        [JsonProperty(PropertyName = "vw")]
        public string View { get; set; }

        [JsonProperty(PropertyName = "wb")]
        public string Workbook { get; set; }
    }
}
