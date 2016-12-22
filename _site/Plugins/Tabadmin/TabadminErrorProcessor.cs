using Logshark.PluginLib.Model.Impl;
using Logshark.PluginLib.Persistence;
using Logshark.Plugins.Tabadmin.Models;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Logshark.Plugins.Tabadmin
{
    /// <summary>
    /// Queries Mongo for tabadmin warning and error messages and persists those messages to a database.
    /// </summary>
    public class TabadminErrorProcessor : TabadminEventBase
    {
        public static void Execute(IMongoCollection<BsonDocument> collection, IPersister<TabadminModelBase> persister, PluginResponse pluginResponse, Guid logsetHash)
        {
            var tabadminErrorProcessor = new TabadminErrorProcessor(collection, persister, pluginResponse, logsetHash);
            tabadminErrorProcessor.QueryMongo();
        }

        private TabadminErrorProcessor(IMongoCollection<BsonDocument> collection, IPersister<TabadminModelBase> persister, PluginResponse pluginResponse, Guid logsetHash)
            : base(collection, persister, pluginResponse, logsetHash)
        { }

        private static FilterDefinition<BsonDocument> QueryTabadminErrors()
        {
            string[] errorSeverities = { "WARN", "ERROR", "FATAL" };
            return Query.In("sev", errorSeverities);
        }

        /// <summary>
        /// Query Mongo and start processing the results.
        /// </summary>
        private void QueryMongo()
        {
            FilterDefinition<BsonDocument> query = QueryTabadminErrors();
            IAsyncCursor<BsonDocument> cursor = collection.Find(query).ToCursor();
            // Start processing requests.
            var tasks = new List<Task>();
            while (cursor.MoveNext())
            {
                tasks.AddRange(cursor.Current.Select(document => Task.Factory.StartNew(() => ProcessTabadminError(document))));
            }
            Task.WaitAll(tasks.ToArray());
        }

        /// <summary>
        /// Create a TabadminError object from mongoDocument and enqueue it to be persisted to the database.
        /// </summary>
        /// <param name="mongoDocument">Log message values to use to create the object.</param>
        protected void ProcessTabadminError(BsonDocument mongoDocument)
        {
            try
            {
                TabadminError tabadminError = new TabadminError(mongoDocument, logsetHash);
                persister.Enqueue(tabadminError);
            }
            catch (Exception ex)
            {
                string errorMessage = String.Format("Encountered an exception on {0}: {1}", mongoDocument.GetValue("_id"), ex);
                pluginResponse.AppendError(errorMessage);
                Log.Error(errorMessage);
            }
        }
    }
}