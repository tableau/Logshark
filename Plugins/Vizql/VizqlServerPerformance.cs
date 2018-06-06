using Logshark.PluginLib.Extensions;
using Logshark.PluginLib.Persistence;
using Logshark.PluginModel.Model;
using Logshark.Plugins.Vizql.Helpers;
using Logshark.Plugins.Vizql.Models;
using Logshark.Plugins.Vizql.Models.Events.Error;
using Logshark.Plugins.Vizql.Models.Events.Performance;
using Logshark.Plugins.Vizql.Models.Events.Query;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

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
                    "VizqlServerPerformance.twb"
                };
            }
        }

        protected override IPersister<VizqlServerSession> GetPersister(IPluginRequest pluginRequest)
        {
            return GetConcurrentCustomPersister<VizqlServerSession>(pluginRequest, ServerSessionPerformancePersistenceHelper.PersistSession);
        }

        protected override VizqlServerSession ProcessSession(string sessionId, IMongoCollection<BsonDocument> collection)
        {
            try
            {
                VizqlServerSession session = MongoQueryHelper.GetServerSession(sessionId, collection, logsetHash);
                session = MongoQueryHelper.AppendAllSessionEvents(session, collection) as VizqlServerSession;
                SetHostname(session);
                return session;
            }
            catch (Exception ex)
            {
                string errorMessage = String.Format("Failed to process session {0} in {1}: {2}", sessionId, collection.CollectionNamespace.CollectionName, ex.Message);
                Log.Error(errorMessage);
                pluginResponse.AppendError(errorMessage);
                return null;
            }
        }

        protected override void ProcessCollections(IEnumerable<IMongoCollection<BsonDocument>> collections)
        {
            var totalVizqlSessions = MongoQueryHelper.GetUniqueSessionIdCount(collections);

            var sessionPersister = GetPersister(this.pluginRequest);
            using (GetPersisterStatusWriter(sessionPersister, totalVizqlSessions))
            {
                var tasks = new List<Task>();
                foreach (IMongoCollection<BsonDocument> collection in collections)
                {
                    var sessionIds = MongoQueryHelper.GetAllUniqueServerSessionIds(collection);
                    foreach (var sessionId in sessionIds)
                    {
                        tasks.Add(Task.Factory.StartNew(() => sessionPersister.Enqueue(ProcessSession(sessionId, collection))));
                    }
                }

                Task.WaitAll(tasks.ToArray());
                sessionPersister.Shutdown();
            }
        }

        protected override void CreateTables(IDbConnection database)
        {
            // Session
            database.CreateOrMigrateTable<VizqlServerSession>();

            // Errors
            database.CreateOrMigrateTable<VizqlErrorEvent>();

            // Performance
            database.CreateOrMigrateTable<VizqlPerformanceEvent>();

            // Query
            database.CreateOrMigrateTable<VizqlEndQuery>();
            database.CreateOrMigrateTable<VizqlQpQueryEnd>();
        }
    }
}