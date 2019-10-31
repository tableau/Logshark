using Logshark.ArtifactProcessors.TableauServerLogProcessor.Parsers;
using Logshark.ArtifactProcessors.TableauServerLogProcessor.PluginInterfaces;
using Logshark.PluginLib.Model.Impl;
using Logshark.PluginModel.Model;
using Logshark.Plugins.ResourceManager.Helpers;
using Logshark.Plugins.ResourceManager.Model;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Logshark.Plugins.ResourceManager
{
    public class ResourceManager : BaseWorkbookCreationPlugin, IServerClassicPlugin, IServerTsmPlugin
    {
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
                    "ResourceManager.twbx"
                };
            }
        }

        public ResourceManager()
        {
        }

        public ResourceManager(IPluginRequest request) : base(request)
        {
        }

        public override IPluginResponse Execute()
        {
            var pluginResponse = CreatePluginResponse();

            var collectionWorkerMap = CollectionDependencies.ToDictionary(collection => collection,
                                                                          collection => ResourceManagerQueries.GetDistinctWorkers(MongoDatabase.GetCollection<BsonDocument>(collection)));

            bool persistedThresholds = ProcessDocuments(collectionWorkerMap, GetThresholds);
            bool persistedCpuInfo = ProcessDocuments(collectionWorkerMap, SelectByPid(ResourceManagerQueries.GetCpuSamples));
            bool persistedMemoryInfo = ProcessDocuments(collectionWorkerMap, SelectByPid(ResourceManagerQueries.GetMemorySamples));
            bool persistedActions = ProcessDocuments(collectionWorkerMap, SelectByPid(ResourceManagerQueries.GetActions));

            if (!persistedThresholds && !persistedCpuInfo && !persistedMemoryInfo && !persistedActions)
            {
                Log.Info("Failed to persist any Server Resource Manager data!");
                pluginResponse.GeneratedNoData = true;
            }

            return pluginResponse;
        }

        private bool ProcessDocuments<T>(IDictionary<string, ISet<string>> collectionWorkerMap, Func<string, IMongoCollection<BsonDocument>, IEnumerable<T>> selector) where T : new()
        {
            Log.InfoFormat("Processing {0} events..", typeof(T).Name);

            using (var persister = ExtractFactory.CreateExtract<T>())
            using (GetPersisterStatusWriter(persister))
            {
                foreach (var collectionWorkerMapping in collectionWorkerMap)
                {
                    var collection = MongoDatabase.GetCollection<BsonDocument>(collectionWorkerMapping.Key);

                    foreach (var worker in collectionWorkerMapping.Value)
                    {
                        var records = selector(worker, collection);
                        persister.Enqueue(records);
                    }
                }

                return persister.ItemsPersisted > 0;
            }
        }

        private static Func<string, IMongoCollection<BsonDocument>, IEnumerable<T>> SelectByPid<T>(Func<string, int, IMongoCollection<BsonDocument>, IEnumerable<T>> selector) where T : new()
        {
            return (worker, collection) => ResourceManagerQueries.GetDistinctPids(worker, collection)
                                                                 .SelectMany(pid => selector(worker, pid, collection));
        }

        private static IEnumerable<ResourceManagerThreshold> GetThresholds(string workerId, IMongoCollection<BsonDocument> collection)
        {
            return ResourceManagerQueries.GetSrmStartEventsForWorker(workerId, collection)
                                         .Select(startEvent => ResourceManagerQueries.GetThreshold(startEvent, collection));
        }
    }
}