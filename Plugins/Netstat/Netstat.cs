using Logshark.ArtifactProcessors.TableauServerLogProcessor.Parsers;
using Logshark.ArtifactProcessors.TableauServerLogProcessor.PluginInterfaces;
using Logshark.PluginLib.Extensions;
using Logshark.PluginLib.Helpers;
using Logshark.PluginLib.Model.Impl;
using Logshark.PluginLib.Persistence;
using Logshark.PluginModel.Model;
using Logshark.Plugins.Netstat.Helpers;
using Logshark.Plugins.Netstat.Model;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;

namespace Logshark.Plugins.Netstat
{
    public class Netstat : BaseWorkbookCreationPlugin, IServerClassicPlugin, IServerTsmPlugin
    {
        private static readonly ISet<string> collectionDependencies = new HashSet<string> { ParserConstants.NetstatCollectionName };
        private static readonly ICollection<string> workbookNames = new List<string> { "Netstat.twb" };

        private Guid logsetHash;

        public override ISet<string> CollectionDependencies
        {
            get { return collectionDependencies; }
        }

        public override ICollection<string> WorkbookNames
        {
            get { return workbookNames; }
        }

        public override IPluginResponse Execute(IPluginRequest pluginRequest)
        {
            IPluginResponse response = CreatePluginResponse();
            logsetHash = pluginRequest.LogsetHash;

            InitializeDatabaseTables();
            IPersister<NetstatActiveConnection> activeConnectionsPersister = GetConcurrentBatchPersister<NetstatActiveConnection>(pluginRequest);

            // Process netstat entries for all available workers.
            var netstatCollection = MongoDatabase.GetCollection<BsonDocument>(ParserConstants.NetstatCollectionName);
            foreach (string workerId in MongoQueryHelper.GetDistinctWorkers(netstatCollection))
            {
                Log.InfoFormat("Retrieving netstat information for worker '{0}'..", workerId);
                IEnumerable<NetstatActiveConnection> activeConnectionsForWorker = GetActiveConnectionEntriesForWorker(workerId, netstatCollection);
                activeConnectionsPersister.Enqueue(activeConnectionsForWorker);
            }

            // Shutdown persister and wait for data to flush.
            activeConnectionsPersister.Shutdown();
            Log.Info("Finished processing netstat data!");

            // Check if we persisted any data.
            if (!PersistedData())
            {
                Log.Info("Failed to persist any netstat data!");
                response.GeneratedNoData = true;
            }

            return response;
        }

        private void InitializeDatabaseTables()
        {
            using (var dbConnection = GetOutputDatabaseConnection())
            {
                dbConnection.CreateOrMigrateTable<NetstatActiveConnection>();
            }
        }

        private IEnumerable<NetstatActiveConnection> GetActiveConnectionEntriesForWorker(string worker, IMongoCollection<BsonDocument> netstatCollection)
        {
            var activeConnectionEntries = new List<NetstatActiveConnection>();

            var netstatQuery = MongoQueryHelper.GetNetstatForWorker(netstatCollection, worker);
            BsonDocument netstatDocument = netstatCollection.Find(netstatQuery).FirstOrDefault();

            if (netstatDocument == null)
            {
                Log.InfoFormat("No netstat data available for worker '{0}'.", worker);
                return activeConnectionEntries;
            }

            DateTime? fileLastModified = BsonDocumentHelper.GetNullableDateTime("last_modified_at", netstatDocument);

            BsonArray activeConnectionsEntries = netstatDocument["active_connections"].AsBsonArray;
            foreach (BsonValue activeConnectionsEntry in activeConnectionsEntries)
            {
                activeConnectionEntries.Add(new NetstatActiveConnection(logsetHash, worker, activeConnectionsEntry.AsBsonDocument, fileLastModified));
            }

            return activeConnectionEntries;
        }
    }
}