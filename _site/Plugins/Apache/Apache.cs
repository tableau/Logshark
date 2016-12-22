using Logshark.PluginLib.Extensions;
using Logshark.PluginLib.Helpers;
using Logshark.PluginLib.Model;
using Logshark.PluginLib.Model.Impl;
using Logshark.PluginLib.Persistence;
using Logshark.Plugins.Apache.Helpers;
using Logshark.Plugins.Apache.Model;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Logshark.Plugins.Apache
{
    public class Apache : BaseWorkbookCreationPlugin, IServerPlugin
    {
        private PluginResponse pluginResponse;
        private Guid logsetHash;
        private IPersister<HttpdRequest> apachePersister;

        private bool includeGatewayHealthCheckRequests;
        private static readonly string IncludeGatewayHealthChecksPluginArgumentKey = "Apache.IncludeGatewayHealthChecks";

        public override ISet<string> CollectionDependencies
        {
            get
            {
                return new HashSet<string>
                {
                    "httpd"
                };
            }
        }

        public override ICollection<string> WorkbookNames
        {
            get
            {
                return new List<string>
                {
                    "Apache.twb"
                };
            }
        }

        public override IPluginResponse Execute(IPluginRequest pluginRequest)
        {
            pluginResponse = CreatePluginResponse();
            logsetHash = pluginRequest.LogsetHash;

            HandlePluginRequestArguments(pluginRequest);

            // Process Apache requests.
            IMongoCollection<BsonDocument> apacheCollection = MongoDatabase.GetCollection<BsonDocument>("httpd");
            apachePersister = GetConcurrentBatchPersister<HttpdRequest>(pluginRequest);

            long totalApacheRequests = CountApacheRequests(apacheCollection);
            using (GetPersisterStatusWriter(apachePersister, totalApacheRequests))
            {
                ProcessApacheLogs(apacheCollection);
                apachePersister.Shutdown();
            }

            Log.Info("Finished processing Apache requests!");

            // Check if we persisted any data.
            if (!PersistedData())
            {
                Log.Info("Failed to persist any data from Apache logs!");
                pluginResponse.GeneratedNoData = true;
            }

            return pluginResponse;
        }

        protected void HandlePluginRequestArguments(IPluginRequest pluginRequest)
        {
            if (pluginRequest.ContainsRequestArgument(IncludeGatewayHealthChecksPluginArgumentKey))
            {
                try
                {
                    includeGatewayHealthCheckRequests = PluginArgumentHelper.GetAsBoolean(IncludeGatewayHealthChecksPluginArgumentKey, pluginRequest);
                }
                catch (FormatException)
                {
                    Log.WarnFormat("Invalid value was specified for plugin argument key '{0}': valid values are either 'true' or 'false'.  Proceeding with default value of '{1}'..",
                                    IncludeGatewayHealthChecksPluginArgumentKey, includeGatewayHealthCheckRequests.ToString().ToLowerInvariant());
                }
            }

            // Log results.
            if (includeGatewayHealthCheckRequests)
            {
                Log.Info("Including gateway health check requests due to user request.");
            }
            else
            {
                Log.InfoFormat("Excluding gateway health check requests from plugin output.  Use the plugin argument '{0}:true' if you wish to include them.", IncludeGatewayHealthChecksPluginArgumentKey);
            }
        }

        /// <summary>
        /// Processes all Apache log events and persists results to DB.
        /// </summary>
        protected void ProcessApacheLogs(IMongoCollection<BsonDocument> apacheCollection)
        {
            GetOutputDatabaseConnection().CreateOrMigrateTable<HttpdRequest>();

            Log.Info("Queueing Apache requests for processing..");

            var apacheCursor = GetApacheCursor(apacheCollection);
            var tasks = new List<Task>();

            using (GetTaskStatusWriter(tasks, "Apache Request processing", CountApacheRequests(apacheCollection)))
            {
                while (apacheCursor.MoveNext())
                {
                    tasks.AddRange(apacheCursor.Current.Select(document => Task.Factory.StartNew(() => ProcessApacheRequest(document))));
                }
                Task.WaitAll(tasks.ToArray());
            }
        }

        /// <summary>
        /// Gets a cursor for the Apache collection.
        /// </summary>
        protected IAsyncCursor<BsonDocument> GetApacheCursor(IMongoCollection<BsonDocument> collection)
        {
            var queryRequestsByFile = MongoQueryHelper.GetApacheRequests(collection, includeGatewayHealthCheckRequests);
            var ignoreUnusedFieldsProjection = MongoQueryHelper.IgnoreUnusedApacheFieldsProjection();
            return collection.Find(queryRequestsByFile).Project(ignoreUnusedFieldsProjection).ToCursor();
        }

        /// <summary>
        /// Populates the HttpdRequest object and queues it for insertion.
        /// </summary>
        protected void ProcessApacheRequest(BsonDocument document)
        {
            try
            {
                HttpdRequest httpdRequest = new HttpdRequest(document, logsetHash);
                apachePersister.Enqueue(httpdRequest);
            }
            catch (Exception ex)
            {
                string errorMessage = String.Format("Encountered an exception on {0}: {1}", document.GetValue("request_id"), ex);
                pluginResponse.AppendError(errorMessage);
                Log.Error(errorMessage);
            }
        }

        /// <summary>
        /// Count the number of Apache requests in the collection.
        /// </summary>
        /// <param name="collection">The collection to search for requests in.</param>
        /// <returns>The number of Apache requests in the collection</returns>
        protected long CountApacheRequests(IMongoCollection<BsonDocument> collection)
        {
            var query = MongoQueryHelper.GetApacheRequests(collection, includeGatewayHealthCheckRequests);
            return collection.Count(query);
        }
    }
}