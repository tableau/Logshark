using MongoDB.Bson.Serialization.Attributes;

namespace Logshark.Plugins.Art.Model
{
    [BsonIgnoreExtraElements]
    public class ResourceConsumptionMetrics
    {
        [BsonElement("alloc")]
        [BsonIgnoreIfNull]
        public AllocatedMemoryMetrics AllocatedMemoryMetrics { get; set; }

        [BsonElement("free")]
        [BsonIgnoreIfNull]
        public FreedMemoryMetrics FreedMemoryMetrics { get; set; }
        
        [BsonElement("kcpu")]
        [BsonIgnoreIfNull]
        public CpuMetrics KernelSpaceCpuMetrics { get; set; }
        
        [BsonElement("ntid")]
        [BsonIgnoreIfNull]
        public int NumberOfThreadsActivityRanOn { get; set; }
        
        [BsonElement("ucpu")]
        [BsonIgnoreIfNull]
        public CpuMetrics UserSpaceCpuMetrics { get; set; }
    }
}