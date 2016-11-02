using MongoDB.Bson;
using MongoDB.Driver;

namespace Logshark.Plugins.Vizportal.Helpers
{
    class MongoQueryHelper
    {
        private static readonly FilterDefinitionBuilder<BsonDocument> Query = Builders<BsonDocument>.Filter;

        public static FilterDefinition<BsonDocument> VizportalRequestsByFile(IMongoCollection<BsonDocument> collection)
        {
            var filenameQuery = Query.Regex("file", new BsonRegularExpression("vizportal.*"));
            return filenameQuery;         
        }

        public static ProjectionDefinition<BsonDocument> IgnoreUnusedVizportalFieldsProjection()
        {
            // Include all fields except for the following.
            return Builders<BsonDocument>.Projection.Exclude("ts_offset")
                                                    .Exclude("file_path")
                                                    .Exclude("thread")
                                                    .Exclude("line");
        }
    }
}
