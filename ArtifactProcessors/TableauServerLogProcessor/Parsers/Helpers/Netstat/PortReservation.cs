using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Logshark.ArtifactProcessors.TableauServerLogProcessor.Parsers.Helpers.Netstat
{
    /// <summary>
    /// Models a transport-layer port reservation.
    /// </summary>
    internal class PortReservation
    {
        [JsonProperty("protocol")]
        public string Protocol { get; set; }

        [JsonProperty("component", NullValueHandling = NullValueHandling.Ignore)]
        public string Component { get; set; }

        [JsonProperty("process")]
        public string Process { get; set; }

        [JsonProperty("pid", NullValueHandling = NullValueHandling.Ignore)]
        public int? ProcessId { get; set; }

        [JsonProperty("local_address")]
        public string LocalAddress { get; set; }

        [JsonProperty("local_port")]
        public int? LocalPort { get; set; }

        [JsonProperty("foreign_address")]
        public string ForeignAddress { get; set; }

        [JsonProperty("foreign_port")]
        public int? ForeignPort { get; set; }

        [JsonProperty("tcp_state")]
        public string TcpState { get; set; }

        [JsonProperty("recv_q", NullValueHandling = NullValueHandling.Ignore)]
        public int? RecvQ { get; set; }

        [JsonProperty("send_q", NullValueHandling = NullValueHandling.Ignore)]
        public int? SendQ { get; set; }

        [JsonProperty("line")]
        public int Line { get; set; }

        public JToken ToJToken()
        {
            return JObject.FromObject(this);
        }
    }
}