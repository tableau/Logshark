using Logshark.PluginLib.Extensions;
using Logshark.PluginLib.Model;
using Logshark.PluginLib.Model.Impl;
using Logshark.PluginLib.Persistence;
using Logshark.Plugins.ClusterController.Helpers;
using Logshark.Plugins.ClusterController.Models;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Logshark.Plugins.ClusterController
{
    public class ClusterController : BaseWorkbookCreationPlugin, IServerPlugin
    {
        private PluginResponse pluginResponse;

        private Guid logsetHash;

        private IPersister<ClusterControllerError> clusterControllerErrorPersister;
        private IPersister<ClusterControllerPostgresAction> clusterControllerPostgresActionPersister;
        private IPersister<ClusterControllerDiskIoSample> clusterControllerDiskIoSamplePersister;
        private IPersister<ZookeeperError> zookeeperErrorPersister;
        private IPersister<ZookeeperFsyncLatency> zookeeperFsyncPersister;

        public override ISet<string> CollectionDependencies
        {
            get
            {
                return new HashSet<string>
                {
                    "clustercontroller",
                    "zookeeper"
                };
            }
        }

        public override ICollection<string> WorkbookNames
        {
            get
            {
                return new List<string>
                {
                    "ClusterController.twb"
                };
            }
        }

        public override IPluginResponse Execute(IPluginRequest pluginRequest)
        {
            pluginResponse = CreatePluginResponse();
            logsetHash = pluginRequest.LogsetHash;

            clusterControllerErrorPersister = GetConcurrentBatchPersister<ClusterControllerError>(pluginRequest);
            using (GetPersisterStatusWriter(clusterControllerErrorPersister))
            {
                ProcessClusterControllerErrors();
            }
            Log.Info("Finished processing Cluster Controller Error events!");

            clusterControllerPostgresActionPersister = GetConcurrentBatchPersister<ClusterControllerPostgresAction>(pluginRequest);
            using (GetPersisterStatusWriter(clusterControllerPostgresActionPersister))
            {
                ProcessClusterControllerPostgresActions();
            }
            Log.Info("Finished processing Cluster Controller Postgres actions!");

            clusterControllerDiskIoSamplePersister = GetConcurrentBatchPersister<ClusterControllerDiskIoSample>(pluginRequest);
            using (GetPersisterStatusWriter(clusterControllerDiskIoSamplePersister))
            {
                ProcessClusterControllerDiskIoSamples();
            }
            Log.Info("Finished processing Cluster Controller Disk I/O monitoring samples!");

            zookeeperErrorPersister = GetConcurrentBatchPersister<ZookeeperError>(pluginRequest);
            using (GetPersisterStatusWriter(zookeeperErrorPersister))
            {
                ProcessZookeeperErrors();
            }
            Log.Info("Finished processing Zookeeper Error events!");

            zookeeperFsyncPersister = GetConcurrentBatchPersister<ZookeeperFsyncLatency>(pluginRequest);
            using (GetPersisterStatusWriter(zookeeperFsyncPersister))
            {
                ProcessZookeeperFsyncLatencies();
            }
            Log.Info("Finished processing Zookeeper Fsync Latency events!");

            // Check if we persisted any data.
            if (!PersistedData())
            {
                Log.Info("Failed to persist any data from Cluster Controller logs!");
                pluginResponse.GeneratedNoData = true;
            }

            return pluginResponse;
        }

        protected void ProcessClusterControllerErrors()
        {
            IMongoCollection<BsonDocument> clusterControllerCollection = MongoDatabase.GetCollection<BsonDocument>("clustercontroller");

            GetOutputDatabaseConnection().CreateOrMigrateTable<ClusterControllerError>();

            Log.Info("Queueing Cluster Controller errors for processing..");

            // Construct a cursor to the requests to be processed.
            var cursor = MongoQueryHelper.GetErrorEventsCursor(clusterControllerCollection);
            var tasks = new List<Task>();

            while (cursor.MoveNext())
            {
                tasks.AddRange(cursor.Current.Select(document => Task.Factory.StartNew(() => ProcessClusterControllerError(document))));
            }

            Task.WaitAll(tasks.ToArray());
            clusterControllerErrorPersister.Shutdown();
        }

        protected void ProcessClusterControllerError(BsonDocument document)
        {
            try
            {
                ClusterControllerError clustercontrollerError = new ClusterControllerError(document, logsetHash);
                clusterControllerErrorPersister.Enqueue(clustercontrollerError);
            }
            catch (Exception ex)
            {
                string errorMessage = String.Format("Encountered an exception on {0}: {1}", document.GetValue("_id"), ex);
                pluginResponse.AppendError(errorMessage);
                Log.Error(errorMessage);
            }
        }

        protected void ProcessClusterControllerPostgresActions()
        {
            IMongoCollection<BsonDocument> clusterControllerCollection = MongoDatabase.GetCollection<BsonDocument>("clustercontroller");

            GetOutputDatabaseConnection().CreateOrMigrateTable<ClusterControllerPostgresAction>();

            Log.Info("Queueing Cluster Controller Postgres Actions for processing..");

            // Construct a cursor to the requests to be processed.
            var cursor = MongoQueryHelper.GetPostgresActions(clusterControllerCollection);
            var tasks = new List<Task>();

            while (cursor.MoveNext())
            {
                tasks.AddRange(cursor.Current.Select(document => Task.Factory.StartNew(() => ProcessClusterControllerPostgresAction(document))));
            }

            Task.WaitAll(tasks.ToArray());
            clusterControllerPostgresActionPersister.Shutdown();
        }

        protected void ProcessClusterControllerPostgresAction(BsonDocument document)
        {
            try
            {
                ClusterControllerPostgresAction postgresAction = new ClusterControllerPostgresAction(document, logsetHash);
                clusterControllerPostgresActionPersister.Enqueue(postgresAction);
            }
            catch (Exception ex)
            {
                string errorMessage = String.Format("Encountered an exception on {0}: {1}", document.GetValue("_id"), ex);
                pluginResponse.AppendError(errorMessage);
                Log.Error(errorMessage);
            }
        }

        protected void ProcessClusterControllerDiskIoSamples()
        {
            IMongoCollection<BsonDocument> clusterControllerCollection = MongoDatabase.GetCollection<BsonDocument>("clustercontroller");

            GetOutputDatabaseConnection().CreateOrMigrateTable<ClusterControllerDiskIoSample>();

            Log.Info("Queueing Cluster Controller disk I/O monitoring samples for processing..");

            // Construct a cursor to the requests to be processed.
            var cursor = MongoQueryHelper.GetDiskIoSamplesCursor(clusterControllerCollection);
            var tasks = new List<Task>();

            while (cursor.MoveNext())
            {
                tasks.AddRange(cursor.Current.Select(document => Task.Factory.StartNew(() => ProcessClusterControllerDiskIoSample(document))));
            }

            Task.WaitAll(tasks.ToArray());
            clusterControllerDiskIoSamplePersister.Shutdown();
        }

        protected void ProcessClusterControllerDiskIoSample(BsonDocument document)
        {
            try
            {
                ClusterControllerDiskIoSample diskIoSample = new ClusterControllerDiskIoSample(document, logsetHash);
                clusterControllerDiskIoSamplePersister.Enqueue(diskIoSample);
            }
            catch (Exception ex)
            {
                string errorMessage = String.Format("Encountered an exception on {0}: {1}", document.GetValue("_id"), ex);
                pluginResponse.AppendError(errorMessage);
                Log.Error(errorMessage);
            }
        }

        protected void ProcessZookeeperErrors()
        {
            IMongoCollection<BsonDocument> zookeeperCollection = MongoDatabase.GetCollection<BsonDocument>("zookeeper");

            GetOutputDatabaseConnection().CreateOrMigrateTable<ZookeeperError>();
            Log.Info("Queueing Zookeeper error events for processing..");

            var cursor = MongoQueryHelper.GetErrorEventsCursor(zookeeperCollection);
            var tasks = new List<Task>();

            while (cursor.MoveNext())
            {
                tasks.AddRange(cursor.Current.Select(document => Task.Factory.StartNew(() => ProcessZookeeperError(document))));
            }

            Task.WaitAll(tasks.ToArray());

            zookeeperErrorPersister.Shutdown();
        }

        protected void ProcessZookeeperError(BsonDocument document)
        {
            try
            {
                zookeeperErrorPersister.Enqueue(new ZookeeperError(document, logsetHash));
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
        }

        protected void ProcessZookeeperFsyncLatencies()
        {
            IMongoCollection<BsonDocument> zookeeperCollection = MongoDatabase.GetCollection<BsonDocument>("zookeeper");

            GetOutputDatabaseConnection().CreateOrMigrateTable<ZookeeperFsyncLatency>();
            Log.Info("Queueing Zookeeper fsync latencies for processing..");

            var cursor = MongoQueryHelper.GetFsyncLatencyEventsCursor(zookeeperCollection);
            var tasks = new List<Task>();

            while (cursor.MoveNext())
            {
                tasks.AddRange(cursor.Current.Select(document => Task.Factory.StartNew(() => ProcessZookeeperFsyncLatency(document))));
            }

            Task.WaitAll(tasks.ToArray());

            zookeeperFsyncPersister.Shutdown();
        }

        protected void ProcessZookeeperFsyncLatency(BsonDocument document)
        {
            try
            {
                zookeeperFsyncPersister.Enqueue(new ZookeeperFsyncLatency(document, logsetHash));
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
        }
    }
}