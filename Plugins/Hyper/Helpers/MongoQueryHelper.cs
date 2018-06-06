using Logshark.Plugins.Hyper.Models;
using MongoDB.Driver;

namespace Logshark.Plugins.Hyper.Helpers
{
    internal static class MongoQueryHelper
    {
        public static IAsyncCursor<HyperError> GetErrors(IMongoCollection<HyperError> collection)
        {
            var filter = Builders<HyperError>.Filter;

            var query = filter.Eq("sev", "error") | filter.Eq("sev", "fatal");

            return collection.Find(query).ToCursor();
        }

        public static IAsyncCursor<HyperQuery> GetQueryEndEvents(IMongoCollection<HyperQuery> collection)
        {
            var filter = Builders<HyperQuery>.Filter;

            var query = filter.Eq("k", "query-end");

            return collection.Find(query).ToCursor();
        }
    }
}