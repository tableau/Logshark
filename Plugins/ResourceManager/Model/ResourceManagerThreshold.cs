using MongoDB.Bson;

namespace Logshark.Plugins.ResourceManager.Model
{
    public class ResourceManagerThreshold : ResourceManagerEvent
    {
        public int CpuLimit { get; set; }
        public long PerProcessMemoryLimit { get; set; }
        public long TotalMemoryLimit { get; set; }

        public ResourceManagerThreshold()
        {
        }

        public ResourceManagerThreshold(BsonDocument srmStartEvent, string processName, int cpuLimit, long processMemoryLimit, long totalMemoryLimit)
            : base(srmStartEvent, processName)
        {
            CpuLimit = cpuLimit;
            PerProcessMemoryLimit = processMemoryLimit;
            TotalMemoryLimit = totalMemoryLimit;
        }
    }
}
