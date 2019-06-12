using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Tableau.ExtractApi.DataAttributes;

namespace Logshark.Plugins.Art.Model
{
    [BsonIgnoreExtraElements]
    public class ContextMetrics
    {
        [BsonElement("vw")]
        [BsonIgnoreIfNull]
        public string View { get; set; }

        [BsonElement("wb")]
        [BsonIgnoreIfNull]
        public string Workbook { get; set; }
    }
}
