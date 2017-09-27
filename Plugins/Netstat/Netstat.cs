using Logshark.PluginLib.Extensions;
using Logshark.PluginLib.Helpers;
using Logshark.PluginLib.Model;
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
    public class Netstat : BaseWorkbookCreationPlugin, IServerPlugin
    {
        private PluginResponse pluginResponse;
        private Guid logsetHash;
        private IMongoCollection<BsonDocument> netstatCollection;
        private IDictionary<int, string> workerHostnameMap;

        private const string NetstatCollectionName = "netstat";

        public override ISet<string> CollectionDependencies
        {
            get
            {
                return new HashSet<string>
                {
                    NetstatCollectionName
                };
            }
        }

        public override ICollection<string> WorkbookNames
        {
            get
            {
                return new List<string>
                {
                    "Netstat.twb"
                };
            }
        }

        public override IPluginResponse Execute(IPluginRequest pluginRequest)
        {
            pluginResponse = CreatePluginResponse();

            Log.Info("Retrieving configuration information for workers..");
            workerHostnameMap = ConfigDataHelper.GetWorkerHostnameMap(MongoDatabase);

            netstatCollection = MongoDatabase.GetCollection<BsonDocument>(NetstatCollectionName);

            logsetHash = pluginRequest.LogsetHash;

            List<NetstatEntry> netstatEntries = new List<NetstatEntry>();
            foreach (int workerIndex in workerHostnameMap.Keys)
            {
                Log.InfoFormat("Retrieving netstat information for worker {0}..", workerIndex);
                IEnumerable<NetstatEntry> entriesForWorker = GetNetstatEntriesForWorker(workerIndex);
                netstatEntries.AddRange(entriesForWorker);
            }

            Log.InfoFormat("Writing netstat information to database..");
            CreateTables();
            PersistNetstatEntries(netstatEntries);

            Log.Info("Finished processing netstat data!");

            // Check if we persisted any data.
            if (!PersistedData())
            {
                Log.Info("Failed to persist any netstat data!");
                pluginResponse.GeneratedNoData = true;
            }

            return pluginResponse;
        }

        private void CreateTables()
        {
            using (var dbConnection = GetOutputDatabaseConnection())
            {
                dbConnection.CreateOrMigrateTable<NetstatEntry>();
            }
        }

        private void PersistNetstatEntries(IEnumerable<NetstatEntry> netstatEntries)
        {
            IPersister<NetstatEntry> persister = GetConcurrentBatchPersister<NetstatEntry>();
            persister.Enqueue(netstatEntries);
            persister.Shutdown();
        }

        private IEnumerable<NetstatEntry> GetNetstatEntriesForWorker(int workerIndex)
        {
            ICollection<NetstatEntry> netstatEntries = new List<NetstatEntry>();

            var netstatQuery = MongoQueryHelper.GetNetstatForWorker(netstatCollection, workerIndex);
            BsonDocument netstatDocument = netstatCollection.Find(netstatQuery).FirstOrDefault();

            if (netstatDocument == null)
            {
                Log.InfoFormat("No netstat data available for worker {0}.", workerIndex);
                return netstatEntries;
            }

            BsonArray netstatEntryArray = netstatDocument["entries"].AsBsonArray;
            DateTime? fileLastModified = BsonDocumentHelper.GetNullableDateTime("last_modified_at", netstatDocument);
            if (netstatEntryArray.Count > 0)
            {
                foreach (BsonValue netstatEntry in netstatEntryArray)
                {
                    IEnumerable<BsonValue> transportReservations = GetTransportReservationDocuments(netstatEntry.AsBsonDocument);
                    foreach (var transportReservation in transportReservations)
                    {
                        var entry = new NetstatEntry(logsetHash, workerIndex, netstatEntry.AsBsonDocument, transportReservation.AsBsonDocument, fileLastModified);
                        netstatEntries.Add(entry);
                    }
                }
            }

            return netstatEntries;
        }

        private IEnumerable<BsonValue> GetTransportReservationDocuments(BsonDocument bsonDocument)
        {
            if (!bsonDocument.Contains("transport_reservations"))
            {
                return new List<BsonValue>();
            }

            return bsonDocument["transport_reservations"].AsBsonArray.ToList();
        }
    }
}