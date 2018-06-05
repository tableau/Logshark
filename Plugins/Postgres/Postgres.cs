using Logshark.ArtifactProcessors.TableauServerLogProcessor.Parsers;
using Logshark.ArtifactProcessors.TableauServerLogProcessor.PluginInterfaces;
using Logshark.PluginLib.Extensions;
using Logshark.PluginLib.Model.Impl;
using Logshark.PluginLib.Persistence;
using Logshark.PluginModel.Model;
using Logshark.Plugins.Postgres.Helpers;
using Logshark.Plugins.Postgres.Model;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Logshark.Plugins.Postgres
{
    public class Postgres : BaseWorkbookCreationPlugin, IServerClassicPlugin, IServerTsmPlugin
    {
        // The PluginResponse contains state about whether this plugin ran successfully, as well as any errors encountered.  Append any non-fatal errors to this.
        private PluginResponse pluginResponse;

        private Guid logsetHash;

        private IPersister<PostgresEvent> postgresPersister;

        // Two public properties exist on the BaseWorkbookCreationPlugin which can be leveraged in this class:
        //     MongoDatabase - Open connection handle to MongoDB database containing a parsed logset.
        //     OutputDatabaseConnectionFactory - Connection factory for the Postgres database where backing data should be stored.

        public override ISet<string> CollectionDependencies
        {
            get
            {
                return new HashSet<string>
                {
                    ParserConstants.PgSqlCollectionName
                };
            }
        }

        public override ICollection<string> WorkbookNames
        {
            get
            {
                return new List<string>
                {
                    "Postgres.twb"
                };
            }
        }

        public override IPluginResponse Execute(IPluginRequest pluginRequest)
        {
            pluginResponse = CreatePluginResponse();
            logsetHash = pluginRequest.LogsetHash;

            // Your plugin logic goes here.
            IMongoCollection<BsonDocument> postgresCollection = MongoDatabase.GetCollection<BsonDocument>("pgsql");
            postgresPersister = GetConcurrentBatchPersister<PostgresEvent>(pluginRequest);

            long totalPostgresLines = CountPostgresLines(postgresCollection);
            using (GetPersisterStatusWriter<PostgresEvent>(postgresPersister, totalPostgresLines))
            {
                ProcessPostgresLogs(postgresCollection);
                postgresPersister.Shutdown();
            }

            Log.Info("Finished processing Postgres Logs!");

            // Check if we persisted any data.
            if (!PersistedData())
            {
                Log.Info("Failed to persist any data from Postgres logs!");
                pluginResponse.GeneratedNoData = true;
            }

            return pluginResponse;
        }

        protected void ProcessPostgresLogs(IMongoCollection<BsonDocument> postgresCollection)
        {
            GetOutputDatabaseConnection().CreateOrMigrateTable<PostgresEvent>();

            Log.Info("Queuing Postgres info for processing..");

            var postgresCursor = GetPostgresCursor(postgresCollection);
            var tasks = new List<Task>();

            using (GetTaskStatusWriter(tasks, "Postgres processing", CountPostgresLines(postgresCollection)))
            {
                while (postgresCursor.MoveNext())
                {
                    tasks.AddRange(postgresCursor.Current.Select(document => Task.Factory.StartNew(() => ProcessPostgresLine(document))));
                }
                Task.WaitAll(tasks.ToArray());
            }
        }

        protected IAsyncCursor<BsonDocument> GetPostgresCursor(IMongoCollection<BsonDocument> collection)
        {
            var queryPostgresInfoByFile = MongoQueryHelper.LogLinesByFile(collection);
            var ignoreUnusedFieldsProjection = MongoQueryHelper.IgnoreUnusedFieldsProjection();
            return collection.Find(queryPostgresInfoByFile).Project(ignoreUnusedFieldsProjection).ToCursor();
        }

        protected void ProcessPostgresLine(BsonDocument document)
        {
            try
            {
                PostgresEvent postgresInformation = new PostgresEvent(document, logsetHash);
                postgresPersister.Enqueue(postgresInformation);
            }
            catch (Exception ex)
            {
                string errorMessage = String.Format("Encountered an exception processing line: {0}", ex);
                pluginResponse.AppendError(errorMessage);
                Log.Error(errorMessage);
            }
        }

        protected long CountPostgresLines(IMongoCollection<BsonDocument> collection)
        {
            var query = MongoQueryHelper.LogLinesByFile(collection);
            return collection.Count(query);
        }
    }
}