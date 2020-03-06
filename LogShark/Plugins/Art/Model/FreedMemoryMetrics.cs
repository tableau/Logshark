using Newtonsoft.Json;

namespace LogShark.Plugins.Art.Model
{
    public class FreedMemoryMetrics
    {
        /// <summary>
        /// Bytes released for this activity excluding all sponsored activities
        /// </summary>
        [JsonProperty(PropertyName = "e")]
        public object BytesThisActivity { get; set; }

        /// <summary>
        /// Bytes released for this activity including descendent (sponsored) activities
        /// </summary>
        [JsonProperty(PropertyName = "i")]
        public object BytesThisActivityPlusSponsored { get; set; }

        /// <summary>
        /// Number of times release occurred for this activity excluding all sponsored activities
        /// </summary>
        [JsonProperty(PropertyName = "ne")]
        public long NumberOfReleasesThisActivity { get; set; }

        /// <summary>
        /// Number of times release occurred for this activity including descendent (sponsored) activities
        /// </summary>
        [JsonProperty(PropertyName = "ni")]
        public long NumberOfReleasesThisActivityPlusSponsored { get; set; }
    }
}