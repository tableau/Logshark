using Newtonsoft.Json;

namespace LogShark.Plugins.Art.Model
{
    public class ResourceConsumptionMetrics
    {
        [JsonProperty(PropertyName = "alloc")]
        public AllocatedMemoryMetrics AllocatedMemoryMetrics { get; set; }

        [JsonProperty(PropertyName = "free")]
        public FreedMemoryMetrics FreedMemoryMetrics { get; set; }
        
        [JsonProperty(PropertyName = "kcpu")]
        public CpuMetrics KernelSpaceCpuMetrics { get; set; }
        
        [JsonProperty(PropertyName = "ntid")]
        public int NumberOfThreadsActivityRanOn { get; set; }
        
        [JsonProperty(PropertyName = "ucpu")]
        public CpuMetrics UserSpaceCpuMetrics { get; set; }
    }
}