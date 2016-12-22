using log4net;
using Logshark.PluginLib.Logging;
using Logshark.PluginLib.Model.Impl;
using Logshark.PluginLib.Persistence;
using Logshark.Plugins.Tabadmin.Models;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Reflection;

namespace Logshark.Plugins.Tabadmin
{
    /// <summary>
    /// Base class for processing a tabadmin event. Stores connectors to input and output data sources.
    /// </summary>
    public abstract class TabadminEventBase
    {
        protected static readonly FilterDefinitionBuilder<BsonDocument> Query = Builders<BsonDocument>.Filter;
        protected static ILog Log = PluginLogFactory.GetLogger(Assembly.GetExecutingAssembly(), MethodBase.GetCurrentMethod());

        protected IMongoCollection<BsonDocument> collection;
        protected IPersister<TabadminModelBase> persister;
        protected PluginResponse pluginResponse;
        protected Guid logsetHash;

        protected TabadminEventBase(IMongoCollection<BsonDocument> collection, IPersister<TabadminModelBase> persister, PluginResponse pluginResponse, Guid logsetHash)
        {
            this.collection = collection;
            this.persister = persister;
            this.pluginResponse = pluginResponse;
            this.logsetHash = logsetHash;
        }
    }
}