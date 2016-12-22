using MongoDB.Bson;
using MongoDB.Driver;

namespace Logshark.Plugins.Apache.Helpers
{
    internal class MongoQueryHelper
    {
        private static readonly FilterDefinitionBuilder<BsonDocument> Query = Builders<BsonDocument>.Filter;

        public static FilterDefinition<BsonDocument> GetApacheRequests(IMongoCollection<BsonDocument> collection, bool includeGatewayHealthCheckRequests = false)
        {
            // Filter down to only access files.
            FilterDefinition<BsonDocument> query = Query.Regex("file", new BsonRegularExpression("^access.*"));

            // Gateway health check requests are generally noise, but may be desired in some situations.
            if (!includeGatewayHealthCheckRequests)
            {
                query = Query.And(query,
                                  Query.Ne("resource", "/favicon.ico"));
            }

            return query;
        }

        public static ProjectionDefinition<BsonDocument> IgnoreUnusedApacheFieldsProjection()
        {
            // Include all fields except for the following.
            return Builders<BsonDocument>.Projection.Exclude("http_version");
        }
    }
}
