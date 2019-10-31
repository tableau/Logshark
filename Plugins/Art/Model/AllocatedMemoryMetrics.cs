using MongoDB.Bson.Serialization.Attributes;

namespace Logshark.Plugins.Art.Model
{
    [BsonIgnoreExtraElements]
    public class AllocatedMemoryMetrics
    {
        /// <summary>
        /// Bytes allocated for this activity excluding all sponsored activities
        /// </summary>
        [BsonElement("e")]
        [BsonIgnoreIfNull]
        public object BytesThisActivity { get; set; }

        /// <summary>
        /// Bytes allocated for this activity including descendent (sponsored) activities
        /// </summary>
        [BsonElement("i")]
        [BsonIgnoreIfNull]
        public object BytesThisActivityPlusSponsored { get; set; }

        /// <summary>
        /// Max (as in high water mark) bytes allocated at some point, for this activity
        /// </summary>
        [BsonElement("peak")]
        [BsonIgnoreIfNull]
        public object MaxThisActivity { get; set; }

        /// <summary>
        /// Number of times allocations occurred for this activity excluding all sponsored activities
        /// </summary>
        [BsonElement("ne")]
        [BsonIgnoreIfNull]
        public int NumberOfAllocationsThisActivity { get; set; }

        /// <summary>
        /// Number of times allocations occurred for this activity including descendent (sponsored) activities
        /// </summary>
        [BsonElement("ni")]
        [BsonIgnoreIfNull]
        public int NumberOfAllocationsThisActivityPlusSponsored { get; set; }
    }
}