using Logshark.ArtifactProcessors.TableauServerLogProcessor.Parsers;
using Logshark.ArtifactProcessors.TableauServerLogProcessor.PluginInterfaces;
using Logshark.PluginLib.Model.Impl;
using Logshark.PluginModel.Model;
using Logshark.Plugins.ClusterController.Helpers;
using Logshark.Plugins.ClusterController.Models;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;

namespace Logshark.Plugins.ClusterController
{
    public sealed class ClusterController : BaseWorkbookCreationPlugin, IServerClassicPlugin, IServerTsmPlugin
    {
        public override ISet<string> CollectionDependencies
        {
            get
            {
                return new HashSet<string>
                {
                    ParserConstants.ClusterControllerCollectionName,
                    ParserConstants.ZookeeperCollectionName
                };
            }
        }

        public override ICollection<string> WorkbookNames
        {
            get
            {
                return new List<string>
                {
                    "ClusterController.twbx"
                };
            }
        }

        public ClusterController()
        {
        }

        public ClusterController(IPluginRequest request) : base(request)
        {
        }

        public override IPluginResponse Execute()
        {
            var pluginResponse = CreatePluginResponse();

            bool processedClusterControllerErrors = ProcessDocuments(ParserConstants.ClusterControllerCollectionName, Queries.GetErrorEvents(), document => new ClusterControllerError(document));
            bool processedPostgresActions = ProcessDocuments(ParserConstants.ClusterControllerCollectionName, Queries.GetPostgresActions(), document => new ClusterControllerPostgresAction(document));
            bool processedDiskIoSamples = ProcessDocuments(ParserConstants.ClusterControllerCollectionName, Queries.GetDiskIoSamples(), document => new ClusterControllerDiskIoSample(document));
            bool processedZookeeperErrors = ProcessDocuments(ParserConstants.ZookeeperCollectionName, Queries.GetErrorEvents(), document => new ZookeeperError(document));
            bool processedZookeeperFsyncEvents = ProcessDocuments(ParserConstants.ZookeeperCollectionName, Queries.GetFsyncLatencyEvents(), document => new ZookeeperFsyncLatency(document));

            bool persistedData = processedClusterControllerErrors || processedPostgresActions || processedDiskIoSamples || processedZookeeperErrors || processedZookeeperFsyncEvents;
            if (!persistedData)
            {
                Log.Info("Failed to persist any data from Cluster Controller logs!");
                pluginResponse.GeneratedNoData = true;
            }

            return pluginResponse;
        }

        private bool ProcessDocuments<T>(string collectionName, FilterDefinition<BsonDocument> query, Func<BsonDocument, T> transform) where T : new()
        {
            bool persistedData;
            Log.InfoFormat("Processing {0} events..", typeof(T).Name);

            using (var persister = ExtractFactory.CreateExtract<T>())
            using (GetPersisterStatusWriter(persister))
            {
                var collection = MongoDatabase.GetCollection<BsonDocument>(collectionName);
                var documents = collection.Find(query).ToEnumerable();

                foreach (var document in documents)
                {
                    var instance = TransformDocument(document, transform);
                    persister.Enqueue(instance);
                }

                persistedData = persister.ItemsPersisted > 0;
            }

            Log.InfoFormat("Finished processing {0} events!", typeof(T).Name);
            return persistedData;
        }

        private T TransformDocument<T>(BsonDocument document, Func<BsonDocument, T> transform)
        {
            try
            {
                return transform(document);
            }
            catch (Exception ex)
            {
                Log.ErrorFormat("Failed to process {0} document: {1}", typeof(T).Name, ex.Message);
                return default(T);
            }
        }
    }
}