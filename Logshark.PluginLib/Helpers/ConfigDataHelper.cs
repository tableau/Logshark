using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;

namespace Logshark.PluginLib.Helpers
{
    public static class ConfigDataHelper
    {
        public static IDictionary<int, string> GetWorkerHostnameMap(IMongoDatabase database)
        {
            IMongoCollection<BsonDocument> configCollection = database.GetCollection<BsonDocument>("config");

            var query = Builders<BsonDocument>.Filter.Eq("file", "workgroup.yml");
            var projection = Builders<BsonDocument>.Projection.Include("contents.worker.hosts");

            IDictionary<int, string> workerHostnameMap = new Dictionary<int, string>();
            try
            {
                BsonDocument config = configCollection.Find(query).Project<BsonDocument>(projection).First();
                BsonDocument hostsDocument = BsonDocumentHelper.GetBsonDocument("worker", BsonDocumentHelper.GetBsonDocument("contents", config));

                string hosts = BsonDocumentHelper.GetString("hosts", hostsDocument);
                var hostNames = hosts.Split(',');
                for (int hostIndex = 0; hostIndex < hostNames.Length; hostIndex++)
                {
                    workerHostnameMap.Add(hostIndex, hostNames[hostIndex].Trim());
                }
            }
            catch (Exception) { }

            return workerHostnameMap;
        }
    }
}