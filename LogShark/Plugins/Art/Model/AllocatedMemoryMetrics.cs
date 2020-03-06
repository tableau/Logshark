using Newtonsoft.Json;

namespace LogShark.Plugins.Art.Model
{
    public class AllocatedMemoryMetrics
    {
        /// <summary>
        /// Bytes allocated for this activity excluding all sponsored activities
        /// </summary>
        [JsonProperty(PropertyName = "e")]
        public object BytesThisActivity { get; set; }

        /// <summary>
        /// Bytes allocated for this activity including descendent (sponsored) activities
        /// </summary>
        [JsonProperty(PropertyName = "i")]
        public object BytesThisActivityPlusSponsored { get; set; }

        /// <summary>
        /// Max (as in high water mark) bytes allocated at some point, for this activity
        /// </summary>
        [JsonProperty(PropertyName = "peak")]
        public object MaxThisActivity { get; set; }

        /// <summary>
        /// Number of times allocations occurred for this activity excluding all sponsored activities
        /// </summary>
        [JsonProperty(PropertyName = "ne")]
        public long NumberOfAllocationsThisActivity { get; set; }

        /// <summary>
        /// Number of times allocations occurred for this activity including descendent (sponsored) activities
        /// </summary>
        [JsonProperty(PropertyName = "ni")]
        public long NumberOfAllocationsThisActivityPlusSponsored { get; set; }
    }
}