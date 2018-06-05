using Logshark.ArtifactProcessors.TableauServerLogProcessor.Parsers;
using Logshark.ArtifactProcessors.TableauServerLogProcessor.PluginInterfaces;
using Logshark.PluginLib.Extensions;
using Logshark.PluginLib.Helpers;
using Logshark.PluginLib.Model.Impl;
using Logshark.PluginLib.Persistence;
using Logshark.PluginModel.Model;
using Logshark.Plugins.Tabadmin.Helpers;
using Logshark.Plugins.Tabadmin.Models;
using MongoDB.Bson;
using MongoDB.Driver;
using ServiceStack.OrmLite;
using System;
using System.Collections.Generic;
using System.Data;

namespace Logshark.Plugins.Tabadmin
{
    /// <summary>
    /// Process tabadmin log data and extract action, error, and version information.
    /// </summary>
    public class Tabadmin : BaseWorkbookCreationPlugin, IServerClassicPlugin
    {
        // The PluginResponse contains state about whether this plugin ran successfully, as well as any errors encountered.  Append any non-fatal errors to this.
        private PluginResponse pluginResponse;

        private Guid logsetHash;

        private IPersister<TabadminModelBase> persistenceHelper;
        private const string collectionToQuery = "tabadmin";

        // Two public properties exist on the BaseWorkbookCreationPlugin which can be leveraged in this class:
        //     MongoDatabase - Open connection handle to MongoDB database containing a parsed logset.
        //     OutputDatabaseConnectionFactory - Connection factory for the Postgres database where backing data should be stored.

        public override ISet<string> CollectionDependencies
        {
            get
            {
                return new HashSet<string>
                {
                    ParserConstants.TabAdminCollectionName
                };
            }
        }

        public override ICollection<string> WorkbookNames
        {
            get
            {
                return new List<string>
                {
                    "Tabadmin.twb"
                };
            }
        }

        public override IPluginResponse Execute(IPluginRequest pluginRequest)
        {
            pluginResponse = CreatePluginResponse();
            logsetHash = pluginRequest.LogsetHash;
            IMongoCollection<BsonDocument> tabadminCollection = MongoDatabase.GetCollection<BsonDocument>(collectionToQuery);
            persistenceHelper = GetConcurrentBatchPersister<TabadminModelBase>(pluginRequest);

            using (IDbConnection dbConnection = GetOutputDatabaseConnection())
            {
                Log.Info("Processing Tableau Server version data from tabadmin logs...");
                dbConnection.CreateOrMigrateTable<TSVersion>();
                TabadminVersionProcessor.Execute(tabadminCollection, persistenceHelper, pluginResponse, logsetHash);

                // TODO: Create one class for processing Action and Error objects, as they are nearly identical.
                Log.Info("Processing tabadmin error data...");
                dbConnection.CreateOrMigrateTable<TabadminError>();
                TabadminErrorProcessor.Execute(tabadminCollection, persistenceHelper, pluginResponse, logsetHash);

                Log.Info("Processing tabadmin admin action data...");
                dbConnection.CreateOrMigrateTable<TabadminAction>();
                TabadminActionProcessor.Execute(tabadminCollection, persistenceHelper, pluginResponse, logsetHash);

                // Shutdown the persistenceHelper to force a flush to the database, then re-initialize it for future use.
                persistenceHelper.Shutdown();
                persistenceHelper = GetConcurrentBatchPersister<TabadminModelBase>(pluginRequest);

                IList<TSVersion> allTsVersions = dbConnection.Query<TSVersion>("select * from tabadmin_ts_version");

                // TODO: Figure out how to do a lazy query of Error and Action objects, and update them one at a time, rather than loading
                // the entire table into memory. I ran into issues updating the objects while holding the SELECT query connection open with Each().
                // The driver doesn't seem to be able to handle two connections at once.
                Log.Info("Updating version_id foreign keys for TabadminError objects...");
                foreach (var tabadminError in dbConnection.Query<TabadminError>("select * from tabadmin_error"))
                {
                    tabadminError.VersionId = TSVersionHelper.GetTSVersionIdByDate(allTsVersions, tabadminError);
                    dbConnection.Update(tabadminError);
                }

                Log.Info("Updating version_id foreign keys for TabadminAction objects...");
                foreach (var tabadminAction in dbConnection.Query<TabadminAction>("select * from tabadmin_action"))
                {
                    tabadminAction.VersionId = TSVersionHelper.GetTSVersionIdByDate(allTsVersions, tabadminAction);
                    dbConnection.Update(tabadminAction);
                }
            }
            persistenceHelper.Shutdown();

            // Check if we persisted any data.
            if (!PostgresHelper.ContainsRecord<TSVersion>(OutputDatabaseConnectionFactory) &&
                !PostgresHelper.ContainsRecord<TabadminError>(OutputDatabaseConnectionFactory) &&
                !PostgresHelper.ContainsRecord<TabadminAction>(OutputDatabaseConnectionFactory))
            {
                Log.Info("Failed to persist any data from Tabadmin logs!");
                pluginResponse.GeneratedNoData = true;
            }

            return pluginResponse;
        }
    }
}