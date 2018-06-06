using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace Logshark.ArtifactProcessors.TableauServerLogProcessor.Parsers.Helpers.Netstat
{
    internal class UnixDomainSocket
    {
        [JsonProperty("protocol")]
        public string Protocol { get; set; }

        [JsonProperty("reference_count")]
        public int? ReferenceCount { get; set; }

        [JsonProperty("flags")]
        public ICollection<string> Flags { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("state", NullValueHandling = NullValueHandling.Ignore)]
        public string State { get; set; }

        [JsonProperty("inode")]
        public int? INode { get; set; }

        [JsonProperty("pid", NullValueHandling = NullValueHandling.Ignore)]
        public int? ProcessId { get; set; }

        [JsonProperty("program_name")]
        public string ProgramName { get; set; }

        [JsonProperty("path", NullValueHandling = NullValueHandling.Ignore)]
        public string Path { get; set; }

        [JsonProperty("line")]
        public int Line { get; set; }

        public UnixDomainSocket()
        {
            Flags = new List<string>();
        }

        public JToken ToJToken()
        {
            return JObject.FromObject(this);
        }
    }
}