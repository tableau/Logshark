using Logshark.ArtifactProcessors.TableauServerLogProcessor.Parsers;
using Logshark.ArtifactProcessors.TableauServerLogProcessor.PluginInterfaces;
using Logshark.PluginLib.Extensions;
using Logshark.PluginLib.Helpers;
using Logshark.PluginLib.Model.Impl;
using Logshark.PluginLib.Persistence;
using Logshark.PluginModel.Model;
using Logshark.Plugins.Vizql.Helpers;
using Logshark.Plugins.Vizql.Models;
using Logshark.Plugins.Vizql.Models.Events.Error;
using MongoDB.Bson;
using MongoDB.Driver;
using ServiceStack.Common;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace Logshark.Plugins.Vizql
{
    public class VizqlServer : BaseWorkbookCreationPlugin, IServerClassicPlugin, IServerTsmPlugin
    {
        protected IPluginRequest pluginRequest;
        protected Guid logsetHash;
        protected PluginResponse pluginResponse;
        protected IDictionary<int, string> workerHostnameMap;

        public override ICollection<string> WorkbookNames
        {
            get
            {
                return new List<string>
                {
                    "VizqlServer.twb"
                };
            }
        }

        public override ISet<string> CollectionDependencies
        {
            get { return collectionsToQuery.ToHashSet(); }
        }

        protected readonly IList<string> collectionsToQuery = new List<string>
        {
            ParserConstants.VizqlServerCppCollectionName,
            ParserConstants.BackgrounderCppCollectionName,
            ParserConstants.DataserverCppCollectionName
        };

        public override IPluginResponse Execute(IPluginRequest pluginRequest)
        {
            //Set member variables for execution.
            this.pluginRequest = pluginRequest;
            this.logsetHash = pluginRequest.LogsetHash;
            this.pluginResponse = CreatePluginResponse();
            this.workerHostnameMap = ConfigDataHelper.GetWorkerHostnameMap(MongoDatabase);

            // Create output database.
            using (var outputDatabase = GetOutputDatabaseConnection())
            {
                CreateTables(outputDatabase);
            }

            //Process collections.
            ProcessCollections(GetMongoCollections());

            // Check if we persisted any data.
            if (!PersistedData())
            {
                Log.Info("Failed to persist any data from Vizqlserver logs!");
                pluginResponse.GeneratedNoData = true;
            }

            return pluginResponse;
        }

        protected virtual IPersister<VizqlServerSession> GetPersister(IPluginRequest pluginRequest)
        {
            return GetConcurrentCustomPersister<VizqlServerSession>(pluginRequest, ServerSessionPersistenceHelper.PersistSession);
        }

        protected virtual VizqlServerSession ProcessSession(string sessionId, IMongoCollection<BsonDocument> collection)
        {
            try
            {
                VizqlServerSession session = MongoQueryHelper.GetServerSession(sessionId, collection, logsetHash);
                session = MongoQueryHelper.AppendErrorEvents(session, collection) as VizqlServerSession;
                SetHostname(session);
                return session;
            }
            catch (Exception ex)
            {
                string errorMessage = String.Format("Failed to process session {0} in {1}: {2}", sessionId, collection.CollectionNamespace.CollectionName, ex.Message);
                Log.Error(errorMessage);
                pluginResponse.AppendError(errorMessage);
                return null;
            }
        }

        protected virtual void ProcessCollections(IEnumerable<IMongoCollection<BsonDocument>> collections)
        {
            var sessionPersister = GetPersister(this.pluginRequest);

            var tasks = new List<Task>();
            var totalVizqlSessions = MongoQueryHelper.GetUniqueSessionIdCount(collections);

            using (GetPersisterStatusWriter(sessionPersister, totalVizqlSessions))
            {
                foreach (IMongoCollection<BsonDocument> collection in collections)
                {
                    var sessionIds = MongoQueryHelper.GetAllUniqueServerSessionIds(collection);
                    foreach (var sessionId in sessionIds)
                    {
                        tasks.Add(Task.Factory.StartNew(() => sessionPersister.Enqueue(ProcessSession(sessionId, collection))));
                    }
                }

                Task.WaitAll(tasks.ToArray());
                sessionPersister.Shutdown();
            }
        }

        protected virtual void CreateTables(IDbConnection database)
        {
            // Session
            database.CreateOrMigrateTable<VizqlServerSession>();

            // Errors
            database.CreateOrMigrateTable<VizqlErrorEvent>();
        }

        protected List<IMongoCollection<BsonDocument>> GetMongoCollections()
        {
            // Create collection handles.
            var collections = new List<IMongoCollection<BsonDocument>>();
            foreach (var collection in collectionsToQuery)
            {
                collections.Add(MongoDatabase.GetCollection<BsonDocument>(collection));
            }

            return collections;
        }

        protected void SetHostname(VizqlServerSession session)
        {
            if (session.Worker == null)
            {
                session.Hostname = "Unknown";
            }
            else
            {
                session.Hostname = GetHostnameForWorkerId(session.Worker);
            }
        }

        protected string GetHostnameForWorkerId(string workerId)
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