using Logshark.ArtifactProcessors.TableauServerLogProcessor.Parsers;
using Logshark.ArtifactProcessors.TableauServerLogProcessor.PluginInterfaces;
using Logshark.PluginLib.Extensions;
using Logshark.PluginLib.Model.Impl;
using Logshark.PluginLib.Persistence;
using Logshark.PluginModel.Model;
using Logshark.Plugins.ResourceManager.Helpers;
using Logshark.Plugins.ResourceManager.Model;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Data;

namespace Logshark.Plugins.ResourceManager
{
    public class ResourceManager : BaseWorkbookCreationPlugin, IServerClassicPlugin, IServerTsmPlugin
    {
        private PluginResponse pluginResponse;

        private Guid logsetHash;
        private IPersister<ResourceManagerEvent> resourceManagerPersister;

        // List of all of the collections in MongoDB that this plugin is dependent on.
        public override ISet<string> CollectionDependencies
        {
            get
            {
                return new HashSet<string>
                {
                    ParserConstants.BackgrounderCppCollectionName,
                    ParserConstants.DataserverCppCollectionName,
                    ParserConstants.ProtocolServerCollectionName,
                    ParserConstants.VizportalCppCollectionName,
                    ParserConstants.VizqlServerCppCollectionName,
                    ParserConstants.WgServerCppCollectionName,
                };
            }
        }

        public override ICollection<string> WorkbookNames
        {
            get
            {
                return new List<string>
                {
                    "ResourceManager.twb"
                };
            }
        }

        private void Setup(IPluginRequest pluginRequest)
        {
            CreateTables();
            logsetHash = pluginRequest.LogsetHash;
            resourceManagerPersister = GetConcurrentCustomPersister<ResourceManagerEvent>(pluginRequest, ResourceManagerPersistenceHelper.PersistResourceManagerInfo);
        }

        public override IPluginResponse Execute(IPluginRequest pluginRequest)
        {
            Setup(pluginRequest);

            pluginResponse = CreatePluginResponse();

            using (GetPersisterStatusWriter(resourceManagerPersister))
            {
                try
                {
                    ProcessSrmEvents();
                }
                finally
                {
                    resourceManagerPersister.Shutdown();
                }
            }

            // Check if we persisted any data.
            if (!PersistedData())
            {
                Log.Info("Failed to persist any Server Resource Manager data!");
                pluginResponse.GeneratedNoData = true;
            }

            return pluginResponse;
        }

        private void ProcessSrmEvents()
        {
            foreach (var collectionName in CollectionDependencies)
            {
                Log.InfoFormat("Processing SRM sessions from {0}..", collectionName);

                var collection = MongoDatabase.GetCollection<BsonDocument>(collectionName);

                var distinctWorkers = MongoQueryHelper.GetDistinctWorkers(collection);
                foreach (string workerId in distinctWorkers)
                {
                    PersistThresholds(workerId, collection);
                    PersistEvents(workerId, collection);
                }
            }
        }

        private void PersistThresholds(string workerId, IMongoCollection<BsonDocument> collection)
        {
            IList<BsonDocument> startEvents = MongoQueryHelper.GetSrmStartEventsForWorker(workerId, collection);
            foreach (var srmStartEvent in startEvents)
            {
                ResourceManagerThreshold threshold = MongoQueryHelper.GetThreshold(srmStartEvent, collection);
                threshold.LogsetHash = logsetHash;
                resourceManagerPersister.Enqueue(threshold);
            }
        }

        private void PersistEvents(string workerId, IMongoCollection<BsonDocument> collection)
        {
            foreach (var pid in MongoQueryHelper.GetAllUniquePidsByWorker(workerId, collection))
            {
                List<ResourceManagerEvent> resourceManagerEvents = new List<ResourceManagerEvent>();
                resourceManagerEvents.AddRange(MongoQueryHelper.GetCpuInfos(workerId, pid, collection));
                resourceManagerEvents.AddRange(MongoQueryHelper.GetMemoryInfos(workerId, pid, collection));
                resourceManagerEvents.AddRange(MongoQueryHelper.GetActions(workerId, pid, collection));

                foreach (ResourceManagerEvent infoEvent in resourceManagerEvents)
                {
                    infoEvent.LogsetHash = logsetHash;
                    resourceManagerPersister.Enqueue(infoEvent);
                }
            }
        }

        private void CreateTables()
        {
            using (IDbConnection connection = GetOutputDatabaseConnection())
            {
                connection.CreateOrMigrateTable<ResourceManagerCpuInfo>();
                connection.CreateOrMigrateTable<ResourceManagerMemoryInfo>();
                connection.CreateOrMigrateTable<ResourceManagerAction>();
                connection.CreateOrMigrateTable<ResourceManagerThreshold>();
            }
        }
    }
}