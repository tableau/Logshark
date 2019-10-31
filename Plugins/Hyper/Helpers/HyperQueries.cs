using Logshark.Plugins.Hyper.Models;
using MongoDB.Driver;

namespace Logshark.Plugins.Hyper.Helpers
{
    internal static class HyperQueries
    {
        public static FilterDefinition<HyperError> GetErrors()
        {
            return Builders<HyperError>.Filter.Eq("sev", "error") | Builders<HyperError>.Filter.Eq("sev", "fatal");
        }

        public static FilterDefinition<HyperQuery> GetQueryEndEvents()
        {
            return Builders<HyperQuery>.Filter.Eq("k", "query-end");
        }
    }
}