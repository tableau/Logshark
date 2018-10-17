using Logshark.PluginLib.Helpers;
using Logshark.PluginModel.Model;
using Logshark.Plugins.Vizql.Helpers;
using Logshark.Plugins.Vizql.Models;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;

namespace Logshark.Plugins.Vizql
{
    public class VizqlServerPerformance : VizqlServer
    {
        public override ICollection<string> WorkbookNames
        {
            get
            {
                return new List<string>
                {
                    "VizqlServerPerformance.twbx"
                };
            }
        }

        public VizqlServerPerformance()
        {
        }

        public VizqlServerPerformance(IPluginRequest request) : base(request)
        {
        }

        protected override bool ProcessCollections(IList<IMongoCollection<BsonDocument>> collections)
        {
            var workerHostnameMap = ConfigDataHelper.GetWorkerHostnameMap(MongoDatabase);
            var totalVizqlSessions = Queries.GetUniqueSessionIdCount(collections);

            using (var persister = new ServerSessionPerformancePersister(pluginRequest, ExtractFactory))
            using (GetPersisterStatusWriter(persister, totalVizqlSessions))
            {
                foreach (var collection in collections)
                {
                    ProcessCollection(collection, persister, workerHostnameMap);
                }

                return persister.ItemsPersisted > 0;
            }
        }

        protected override VizqlServerSession ProcessSession(string sessionId, IMongoCollection<BsonDocument> collection, IDictionary<int, string> workerHostnameMap)
        {
            try
            {
                VizqlServerSession session = Queries.GetServerSession(sessionId, collection);
                session = Queries.AppendAllSessionEvents(session, collection) as VizqlServerSession;
                return SetHostname(session, workerHostnameMap);
            }
            catch (Exception ex)
            {
                Log.ErrorFormat("Failed to process session {0} in {1}: {2}", sessionId, collection.CollectionNamespace.CollectionName, ex.Message);
                return null;
            }
        }
    }
}