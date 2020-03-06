using Newtonsoft.Json;

namespace LogShark.Plugins.Art.Model
{
    public class CpuMetrics
    {
        /// <summary>
        /// Cpu thread time for this activity excluding descendent (sponsored) activities.
        /// </summary>
        [JsonProperty(PropertyName = "e")]
        public long CpuTimeThisActivityMilliseconds { get; set; }
        
        /// <summary>
        /// Cpu thread time for this activity including all sponsored activities.
        /// </summary>
        [JsonProperty(PropertyName = "i")]
        public long CpuTimeThisActivityPlusSponsoredMilliseconds { get; set; }
    }
}