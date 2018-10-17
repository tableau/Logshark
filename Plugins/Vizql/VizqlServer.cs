using Logshark.ArtifactProcessors.TableauServerLogProcessor.Parsers;
using Logshark.ArtifactProcessors.TableauServerLogProcessor.PluginInterfaces;
using Logshark.PluginLib.Helpers;
using Logshark.PluginLib.Model.Impl;
using Logshark.PluginModel.Model;
using Logshark.Plugins.Vizql.Helpers;
using Logshark.Plugins.Vizql.Models;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Logshark.Plugins.Vizql
{
    public class VizqlServer : BaseWorkbookCreationPlugin, IServerClassicPlugin, IServerTsmPlugin
    {
        protected readonly ISet<string> collectionsToQuery = new HashSet<string>
        {
            ParserConstants.VizqlServerCppCollectionName,
            ParserConstants.BackgrounderCppCollectionName,
            ParserConstants.DataserverCppCollectionName
        };

        public override ICollection<string> WorkbookNames
        {
            get
            {
                return new List<string>
                {
                    "VizqlServer.twbx"
                };
            }
        }

        public override ISet<string> CollectionDependencies
        {
            get { return collectionsToQuery; }
        }

        public VizqlServer()
        {
        }

        public VizqlServer(IPluginRequest request) : base(request)
        {
        }

        public override IPluginResponse Execute()
        {
            var pluginResponse = CreatePluginResponse();

            var collections = GetMongoCollections();

            bool persistedData = ProcessCollections(collections);

            if (!persistedData)
            {
                Log.Info("Failed to persist any data from Vizqlserver logs!");
                pluginResponse.GeneratedNoData = true;
            }

            return pluginResponse;
        }

        protected IList<IMongoCollection<BsonDocument>> GetMongoCollections()
        {
            return collectionsToQuery.Select(collection => MongoDatabase.GetCollection<BsonDocument>(collection)).ToList();
        }

        protected virtual bool ProcessCollections(IList<IMongoCollection<BsonDocument>> collections)
        {
            var workerHostnameMap = ConfigDataHelper.GetWorkerHostnameMap(MongoDatabase);
            var totalVizqlSessions = Queries.GetUniqueSessionIdCount(collections);

            using (var persister = new ServerSessionPersister(ExtractFactory))
            using (GetPersisterStatusWriter(persister, totalVizqlSessions))
            {
                foreach (var collection in collections)
                {
                    ProcessCollection(collection, persister, workerHostnameMap);
                }

                return persister.ItemsPersisted > 0;
            }
        }

        protected void ProcessCollection(IMongoCollection<BsonDocument> collection, IPersister<VizqlServerSession> persister, IDictionary<int, string> workerHostnameMap)
        {
            var uniqueSessionIds = Queries.GetAllUniqueServerSessionIds(collection);
            foreach (var sessionId in uniqueSessionIds)
            {
                var processedSession = ProcessSession(sessionId, collection, workerHostnameMap);
                persister.Enqueue(processedSession);
            }
        }

        protected virtual VizqlServerSession ProcessSession(string sessionId, IMongoCollection<BsonDocument> collection, IDictionary<int, string> workerHostnameMap)
        {
            try
            {
                VizqlServerSession session = Queries.GetServerSession(sessionId, collection);
                session = Queries.AppendErrorEvents(session, collection) as VizqlServerSession;
                return SetHostname(session, workerHostnameMap);
            }
            catch (Exception ex)
            {
                Log.ErrorFormat("Failed to process session {0} in {1}: {2}", sessionId, collection.CollectionNamespace.CollectionName, ex.Message);
                return null;
            }
        }

        protected VizqlServerSession SetHostname(VizqlServerSession session, IDictionary<int, string> workerHostnameMap)
        {
            if (session.Worker == null)
            {
                session.Hostname = "Unknown";
            }
            else
            {
                session.Hostname = GetHostnameForWorkerId(session.Worker, workerHostnameMap);
            }

            return session;
        }

        protected string GetHostnameForWorkerId(string workerId, IDictionary<int, string> workerHostnameMap)
        {
            int workerIndex;
            if (Int32.TryParse(workerId, out workerIndex))
            {
                return workerHostnameMap[workerIndex];
            }

            return String.Format("Unknown ({0})", workerId);
        }
    }
}