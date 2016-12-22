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
    /// Query Mongo for admin-initiated actions from tabadmin logs and store them in a database.
    /// </summary>
    public class TabadminActionProcessor : TabadminEventBase
    {
        public static void Execute(IMongoCollection<BsonDocument> collection, IPersister<TabadminModelBase> persister, PluginResponse pluginResponse, Guid logsetHash)
        {
            var tap = new TabadminActionProcessor(collection, persister, pluginResponse, logsetHash);
            tap.QueryMongo();
        }

        private TabadminActionProcessor(IMongoCollection<BsonDocument> collection, IPersister<TabadminModelBase> persister, PluginResponse pluginResponse, Guid logsetHash)
            : base(collection, persister, pluginResponse, logsetHash)
        { }

        public static FilterDefinition<BsonDocument> QueryTabadminActions(IMongoCollection<BsonDocument> collection)
        {
            return Query.Regex("message", new BsonRegularExpression("/^run as: <script>/"));
        }

        /// <summary>
        /// Query Mongo and start processing the results.
        /// </summary>
        private void QueryMongo()
        {
            FilterDefinition<BsonDocument> query = QueryTabadminActions(collection);
            IAsyncCursor<BsonDocument> cursor = collection.Find(query).ToCursor();
            // Start processing requests.
            var tasks = new List<Task>();
            while (cursor.MoveNext())
            {
                tasks.AddRange(cursor.Current.Select(document => Task.Factory.StartNew(() => ProcessTabadminAction(document))));
            }
            Task.WaitAll(tasks.ToArray());
        }

        /// <summary>
        /// Create a TabadminAction object from mongoDocument and enqueue it to be persisted to the database.
        /// </summary>
        /// <param name="mongoDocument">Log message values to use to create the object.</param>
        protected void ProcessTabadminAction(BsonDocument mongoDocument)
        {
            try
            {
                TabadminAction tabadminAction = new TabadminAction(mongoDocument, logsetHash);
                persister.Enqueue(tabadminAction);
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