using Logshark.Plugins.Netstat.Model;
using MongoDB.Driver;
using System.Collections.Generic;

namespace Logshark.Plugins.Netstat.Helpers
{
    internal static class NetstatQueries
    {
        private static readonly FilterDefinitionBuilder<NetstatDocument> Query = Builders<NetstatDocument>.Filter;

        public static IEnumerable<string> GetDistinctWorkers(IMongoCollection<NetstatDocument> collection)
        {
            var filter = Query.Exists("worker");
            return collection.Distinct<string>("worker", filter).ToList();
        }

        public static FilterDefinition<NetstatDocument> GetNetstatForWorker(string worker)
        {
            // netstat-info.txt used by Windows and netstat-anp.txt used by Linux 
            var fileSubQuery = Query.Eq("file", "netstat-info.txt") | Query.Eq("file", "netstat-anp.txt");
            return Query.Eq("worker", worker) & fileSubQuery;
        }
    }
}