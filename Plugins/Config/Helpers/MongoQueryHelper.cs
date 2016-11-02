using MongoDB.Bson;
using MongoDB.Driver;

namespace Logshark.Plugins.Config.Helpers
{
    public static class MongoQueryHelper
    {
        private static readonly FilterDefinitionBuilder<BsonDocument> Query = Builders<BsonDocument>.Filter;

        public static FilterDefinition<BsonDocument> GetConfig(IMongoCollection<BsonDocument> collection)
        {
            // Construct query filter.
            return Query.Eq("worker", 0) & Query.Eq("file", "workgroup.yml");
        }

        public static FilterDefinition<BsonDocument> GetTabSvcYml(IMongoCollection<BsonDocument> collection)
        {
            // Construct query filter.
            return Query.Eq("worker", 0) & Query.Eq("file", "tabsvc.yml");
        }

        public static ProjectionDefinition<BsonDocument> GetHostsProjection()
        {
            // Return list of all worker hosts
            return Builders<BsonDocument>.Projection.Include("contents.worker.hosts");
        }
    }
}
