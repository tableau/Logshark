using Logshark.PluginLib.Extensions;
using Logshark.PluginLib.Model;
using Logshark.PluginLib.Model.Impl;
using Logshark.PluginLib.Persistence;
using Logshark.Plugins.Vizql.Helpers;
using Logshark.Plugins.Vizql.Models;
using Logshark.Plugins.Vizql.Models.Events;
using Logshark.Plugins.Vizql.Models.Events.Caching;
using Logshark.Plugins.Vizql.Models.Events.Compute;
using Logshark.Plugins.Vizql.Models.Events.Connection;
using Logshark.Plugins.Vizql.Models.Events.Error;
using Logshark.Plugins.Vizql.Models.Events.Etc;
using Logshark.Plugins.Vizql.Models.Events.Performance;
using Logshark.Plugins.Vizql.Models.Events.Query;
using Logshark.Plugins.Vizql.Models.Events.Render;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace Logshark.Plugins.Vizql
{
    public class VizqlDesktop : BaseWorkbookCreationPlugin, IDesktopPlugin
    {
        private PluginResponse pluginResponse;

        private IPersister<VizqlDesktopSession> persistenceHelper;
        private Guid logsetHash;

        public override ISet<string> CollectionDependencies
        {
            get
            {
                return new HashSet<string>
                {
                    "desktop_cpp"
                };
            }
        }

        public override ICollection<string> WorkbookNames
        {
            get
            {
                return new List<string>
                {
                    "VizqlDesktop.twb"
                };
            }
        }

        public override IPluginResponse Execute(IPluginRequest pluginRequest)
        {
            pluginResponse = CreatePluginResponse();

            GetOutputDatabaseConnection();

            logsetHash = pluginRequest.LogsetHash;

            // Create output database.
            using (var outputDatabase = GetOutputDatabaseConnection())
            {
                CreateTables(outputDatabase);
            }

            IMongoCollection<BsonDocument> desktopCollection = MongoDatabase.GetCollection<BsonDocument>("desktop_cpp");

            var tasks = new List<Task>();

            persistenceHelper = GetConcurrentCustomPersister<VizqlDesktopSession>(DesktopSessionPersistenceHelper.PersistSession, pluginRequest);

            using (GetPersisterStatusWriter(persistenceHelper))
            {
                ProcessSessions(tasks, desktopCollection);
                Task.WaitAll(tasks.ToArray());
                persistenceHelper.Shutdown();
            }

            // Check if we persisted any data.
            if (!PersistedData())
            {
                Log.Info("Failed to persist any data from Vizql desktop logs!");
                pluginResponse.GeneratedNoData = true;
            }

            return pluginResponse;
        }

        private void ProcessSessions(IList<Task> tasks, IMongoCollection<BsonDocument> collection)
        {
            // Process all sessions in the collection.
            foreach (var vizqlDesktopSession in MongoQueryHelper.GetAllDesktopSessions(collection, logsetHash))
            {
                tasks.Add(Task.Factory.StartNew(() => ProcessSession(vizqlDesktopSession, collection)));
            }
        }

        private void ProcessSession(VizqlSession session, IMongoCollection<BsonDocument> collection)
        {
            try
            {
                session = MongoQueryHelper.AppendAllSessionEvents(session, collection);
                persistenceHelper.Enqueue(session as VizqlDesktopSession);
            }
            catch (Exception ex)
            {
                string errorMessage = String.Format("Failed to process session {0} in {1}: {2}", session.VizqlSessionId, collection.CollectionNamespace.CollectionName, ex.Message);
                Log.Error(errorMessage);
                pluginResponse.AppendError(errorMessage);
            }
        }

        protected void CreateTables(IDbConnection database)
        {
            //DLL Load
            database.CreateOrMigrateTable<VizqlDllVersionInfo>();

            //Session Table
            database.CreateOrMigrateTable<VizqlDesktopSession>();

            //Errors
            database.CreateOrMigrateTable<VizqlErrorEvent>();

            //Performance
            database.CreateOrMigrateTable<VizqlPerformanceEvent>();

            //Query
            database.CreateOrMigrateTable<VizqlEndQuery>();
            database.CreateOrMigrateTable<VizqlQpQueryEnd>();
            database.CreateOrMigrateTable<VizqlEndPrepareQuickFilterQueries>();
            database.CreateOrMigrateTable<VizqlEndSqlTempTableTuplesCreate>();
            database.CreateOrMigrateTable<VizqlQpBatchSummary>();
            database.CreateOrMigrateTable<VizqlQpBatchSummaryJob>();
            database.CreateOrMigrateTable<VizqlDsInterpretMetadata>();
            database.CreateOrMigrateTable<VizqlSetCollation>();
            database.CreateOrMigrateTable<VizqlProcessQuery>();

            //Connections
            database.CreateOrMigrateTable<VizqlConstructProtocol>();
            database.CreateOrMigrateTable<VizqlConstructProtocolGroup>();
            database.CreateOrMigrateTable<VizqlDsConnect>();

            //Caching
            database.CreateOrMigrateTable<VizqlEcDrop>();
            database.CreateOrMigrateTable<VizqlEcLoad>();
            database.CreateOrMigrateTable<VizqlEcStore>();
            database.CreateOrMigrateTable<VizqlEqcLoad>();
            database.CreateOrMigrateTable<VizqlEqcStore>();

            //Compute
            database.CreateOrMigrateTable<VizqlEndComputeQuickFilterState>();

            //Render
            database.CreateOrMigrateTable<VizqlEndUpdateSheet>();

            //Message
            database.CreateOrMigrateTable<VizqlMessage>();

            //Etc
            database.CreateOrMigrateTable<VizqlEtc>();
        }
    }
}