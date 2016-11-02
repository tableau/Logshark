using MongoDB.Bson;
using MongoDB.Driver;

namespace Logshark.Plugins.SearchServer.Helpers
{
    internal class MongoQuerySearchserverHelper
    {
        private static readonly FilterDefinitionBuilder<BsonDocument> Query = Builders<BsonDocument>.Filter;

        public static FilterDefinition<BsonDocument> SearchserverByFile(IMongoCollection<BsonDocument> collection)
        {
            var filenameQuery = Query.Regex("file", new BsonRegularExpression("searchserver.*"));
            return filenameQuery;
        }

        public static ProjectionDefinition<BsonDocument> IgnoreUnusedSearchserverFieldsProjection()
        {
            // Include all fields except for the following.
            return Builders<BsonDocument>.Projection.Exclude("file_path");
        }
    }
}