using MongoDB.Bson;
using MongoDB.Driver;

namespace Logshark.Plugins.Netstat.Helpers
{
    public static class MongoQueryHelper
    {
        private static readonly FilterDefinitionBuilder<BsonDocument> Query = Builders<BsonDocument>.Filter;

        public static FilterDefinition<BsonDocument> GetNetstatForWorker(IMongoCollection<BsonDocument> collection, int worker)
        {
            return Query.Eq("file", "netstat-info.txt") & Query.Eq("worker", worker);
        }
    }
}
