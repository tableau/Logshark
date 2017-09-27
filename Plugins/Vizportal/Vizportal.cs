using Logshark.PluginLib.Extensions;
using Logshark.PluginLib.Model;
using Logshark.PluginLib.Model.Impl;
using Logshark.PluginLib.Persistence;
using Logshark.PluginModel.Model;
using Logshark.Plugins.Vizportal.Helpers;
using Logshark.Plugins.Vizportal.Models;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Logshark.Plugins.Vizportal
{
    public class Vizportal : BaseWorkbookCreationPlugin, IServerPlugin
    {
        private PluginResponse pluginResponse;
        private IPersister<VizportalEvent> vizportalPersister;
        private Guid logsetHash;

        public override ISet<string> CollectionDependencies
        {
            get
            {
                return new HashSet<string>
                {
                    "vizportal_java"
                };
            }
        }

        public override ICollection<string> WorkbookNames
        {
            get
            {
                return new List<string>
                {
                    "Vizportal.twb"
                };
            }
        }

        public override IPluginResponse Execute(IPluginRequest pluginRequest)
        {
            pluginResponse = CreatePluginResponse();

            logsetHash = pluginRequest.LogsetHash;

            // Process Vizportal events.
            IMongoCollection<BsonDocument> vizportalCollection = MongoDatabase.GetCollection<BsonDocument>("vizportal_java");
            vizportalPersister = GetConcurrentBatchPersister<VizportalEvent>(pluginRequest);

            long totalVizportalRequests = CountVizportalRequests(vizportalCollection);
            using (GetPersisterStatusWriter(vizportalPersister, totalVizportalRequests))
            {
                ProcessVizportalLogs(vizportalCollection);
                vizportalPersister.Shutdown();
            }

            Log.Info("Finished processing Vizportal events!");

            // Check if we persisted any data.
            if (!PersistedData())
            {
                Log.Info("Failed to persist any data from Vizportal logs!");
                pluginResponse.GeneratedNoData = true;
            }

            return pluginResponse;
        }

        /// <summary>
        /// Processes all Vizportal log events and persists results to DB.
        /// </summary>
        protected void ProcessVizportalLogs(IMongoCollection<BsonDocument> vizportalCollection)
        {
            GetOutputDatabaseConnection().CreateOrMigrateTable<VizportalEvent>();

            Log.Info("Queueing Vizportal events for processing..");

            var vizportalCursor = GetVizportalCursor(vizportalCollection);
            var tasks = new List<Task>();

            while (vizportalCursor.MoveNext())
            {
                tasks.AddRange(vizportalCursor.Current.Select(document => Task.Factory.StartNew(() => ProcessVizportalRequest(document))));
            }

            Task.WaitAll(tasks.ToArray());
        }

        /// <summary>
        /// Populates the VizPortalRequest object and queues it for insertion.
        /// </summary>
        protected void ProcessVizportalRequest(BsonDocument mongoDocument)
        {
            try
            {
                VizportalEvent vizportalRequest = new VizportalEvent(mongoDocument, logsetHash);
                vizportalPersister.Enqueue(vizportalRequest);
            }
            catch (Exception ex)
            {
                string errorMessage = String.Format("Encountered an exception on {0}: {1}", mongoDocument.GetValue("req"), ex);
                pluginResponse.AppendError(errorMessage);
                Log.Error(errorMessage);
            }
        }

        /// <summary>
        /// Count the number of Vizportal requests in the collection.
        /// </summary>
        /// <param name="collection">The collection to search for requests in.</param>
        /// <returns>The number of Vizportal requests in the collection</returns>
        protected long CountVizportalRequests(IMongoCollection<BsonDocument> collection)
        {
            var query = MongoQueryHelper.VizportalRequestsByFile(collection);
            return collection.Count(query);
        }

        /// <summary>
        /// Gets a cursor for the collection
        /// </summary>
        /// <param name="collection">The current collection in use</param>
        /// <returns>Cursor to mongo collection</returns>
        protected IAsyncCursor<BsonDocument> GetVizportalCursor(IMongoCollection<BsonDocument> collection)
        {
            var queryRequestsByFile = MongoQueryHelper.VizportalRequestsByFile(collection);
            var ignoreUnusedFieldsProjection = MongoQueryHelper.IgnoreUnusedVizportalFieldsProjection();
            return collection.Find(queryRequestsByFile).Project(ignoreUnusedFieldsProjection).ToCursor();
        }
    }
}