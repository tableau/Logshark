using MongoDB.Bson.Serialization.Attributes;

namespace Logshark.Plugins.Art.Model
{
    [BsonIgnoreExtraElements]
    public class CpuMetrics
    {
        /// <summary>
        /// Cpu thread time for this activity excluding descendent (sponsored) activities.
        /// </summary>
        [BsonElement("e")]
        [BsonIgnoreIfNull]
        public int CpuTimeThisActivityMilliseconds { get; set; }
        
        /// <summary>
        /// Cpu thread time for this activity including all sponsored activities.
        /// </summary>
        [BsonElement("i")]
        [BsonIgnoreIfNull]
        public int CpuTimeThisActivityPlusSponsoredMilliseconds { get; set; }
    }
}