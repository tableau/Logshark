using Logshark.PluginLib.Extensions;
using Logshark.PluginLib.Model;
using Logshark.PluginLib.Model.Impl;
using Logshark.PluginLib.Persistence;
using Logshark.Plugins.SearchServer.Helpers;
using Logshark.Plugins.SearchServer.Models;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Logshark.Plugins.SearchServer
{
    public class SearchServer : BaseWorkbookCreationPlugin, IServerPlugin
    {
        // The PluginResponse contains state about whether this plugin ran successfully, as well as any errors encountered.  Append any non-fatal errors to this.
        private PluginResponse pluginResponse;

        private Guid logsetHash;

        private IPersister<SearchserverEvent> searchserverPersister;

        // Two public properties exist on the BaseWorkbookCreationPlugin which can be leveraged in this class:
        //     MongoDatabase - Open connection handle to MongoDB database containing a parsed logset.
        //     OutputDatabaseConnectionFactory - Connection factory for the Postgres database where backing data should be stored.

        public override ISet<string> CollectionDependencies
        {
            get
            {
                return new HashSet<string>
                {
                    "searchserver"
                };
            }
        }

        public override ICollection<string> WorkbookNames
        {
            get
            {
                return new List<string>
                {
                    "SearchServer.twb"
                };
            }
        }

        public override IPluginResponse Execute(IPluginRequest pluginRequest)
        {
            pluginResponse = CreatePluginResponse();
            logsetHash = pluginRequest.LogsetHash;

            // Process Searchserver events.
            IMongoCollection<BsonDocument> searchserverCollection = MongoDatabase.GetCollection<BsonDocument>("searchserver");

            searchserverPersister = GetConcurrentBatchPersister<SearchserverEvent>(pluginRequest);
            long totalSearchserverEvents = CountSearchserverEvents(searchserverCollection);
            using (GetPersisterStatusWriter<SearchserverEvent>(searchserverPersister, totalSearchserverEvents))
            {
                ProcessSearchserverLogs(searchserverCollection);
            }
            Log.Info("Finished processing Search Server events!");

            // Check if we persisted any data.
            if (!PersistedData())
            {
                Log.Info("Failed to persist any data from Search Server logs!");
                pluginResponse.GeneratedNoData = true;
            }

            return pluginResponse;
        }

        protected IAsyncCursor<BsonDocument> GetSearchserverCursor(IMongoCollection<BsonDocument> collection)
        {
            var queryRequestsByFile = MongoQuerySearchserverHelper.SearchserverByFile(collection);
            var ignoreUnusedFieldsProjection = MongoQuerySearchserverHelper.IgnoreUnusedSearchserverFieldsProjection();
            return collection.Find(queryRequestsByFile).Project(ignoreUnusedFieldsProjection).ToCursor();
        }

        protected void ProcessSearchserverLogs(IMongoCollection<BsonDocument> collection)
        {
            GetOutputDatabaseConnection().CreateOrMigrateTable<SearchserverEvent>();

            Log.Info("Queueing Search Server events for processing..");

            // Construct a cursor to the requests to be processed.
            var cursor = GetSearchserverCursor(collection);
            var tasks = new List<Task>();

            while (cursor.MoveNext())
            {
                tasks.AddRange(cursor.Current.Select(document => Task.Factory.StartNew(() => ProcessSearchserverEvent(document))));
            }

            Task.WaitAll(tasks.ToArray());
            searchserverPersister.Shutdown();
        }

        protected void ProcessSearchserverEvent(BsonDocument document)
        {
            try
            {
                SearchserverEvent searchserverEvent = new SearchserverEvent(document, logsetHash);
                searchserverPersister.Enqueue(searchserverEvent);
            }
            catch (Exception ex)
            {
                string errorMessage = String.Format("Encountered an exception on {0}: {1}", document.GetValue("_id"), ex);
                pluginResponse.AppendError(errorMessage);
                Log.Error(errorMessage);
            }
        }

        /// <summary>
        /// Count the number of Search server events in the collection.
        /// </summary>
        /// <param name="collection">The collection to search for requests in.</param>
        /// <returns>The number of Searchserver Events in the collection</returns>
        protected long CountSearchserverEvents(IMongoCollection<BsonDocument> collection)
        {
            var query = MongoQuerySearchserverHelper.SearchserverByFile(collection);
            return collection.Count(query);
        }
    }
}