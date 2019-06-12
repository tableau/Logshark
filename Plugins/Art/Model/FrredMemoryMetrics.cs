using MongoDB.Bson.Serialization.Attributes;

namespace Logshark.Plugins.Art.Model
{
    [BsonIgnoreExtraElements]
    public class FreedMemoryMetrics
    {
        /// <summary>
        /// Bytes released for this activity excluding all sponsored activities
        /// </summary>
        [BsonElement("e")]
        [BsonIgnoreIfNull]
        public object BytesThisActivity { get; set; }

        /// <summary>
        /// Bytes released for this activity including descendent (sponsored) activities
        /// </summary>
        [BsonElement("i")]
        [BsonIgnoreIfNull]
        public object BytesThisActivityPlusSponsored { get; set; }

        /// <summary>
        /// Number of times release occurred for this activity excluding all sponsored activities
        /// </summary>
        [BsonElement("ne")]
        [BsonIgnoreIfNull]
        public int NumberOfReleasesThisActivity { get; set; }

        /// <summary>
        /// Number of times release occurred for this activity including descendent (sponsored) activities
        /// </summary>
        [BsonElement("ni")]
        [BsonIgnoreIfNull]
        public int NumberOfReleasesThisActivityPlusSponsored { get; set; }
    }
}