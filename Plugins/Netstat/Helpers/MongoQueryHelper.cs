using MongoDB.Bson;
using MongoDB.Driver;
using System.Collections.Generic;

namespace Logshark.Plugins.Netstat.Helpers
{
    public static class MongoQueryHelper
    {
        private static readonly FilterDefinitionBuilder<BsonDocument> Query = Builders<BsonDocument>.Filter;

        public static IEnumerable<string> GetDistinctWorkers(IMongoCollection<BsonDocument> collection)
        {
            var filter = Query.Exists("worker");
            return collection.Distinct<string>("worker", filter).ToList();
        }

        public static FilterDefinition<BsonDocument> GetNetstatForWorker(IMongoCollection<BsonDocument> collection, string worker)
        {
            return Query.Eq("file", "netstat-info.txt") & Query.Eq("worker", worker);
        }
    }
}
