using Logshark.PluginLib.Extensions;
using Logshark.PluginLib.Model;
using Logshark.PluginLib.Model.Impl;
using Logshark.PluginLib.Persistence;
using Logshark.PluginModel.Model;
using Logshark.Plugins.Backgrounder.Helpers;
using Logshark.Plugins.Backgrounder.Model;
using System.Collections.Generic;

namespace Logshark.Plugins.Backgrounder
{
    public class Backgrounder : BaseWorkbookCreationPlugin, IServerPlugin
    {
        private PluginResponse pluginResponse;

        public override ISet<string> CollectionDependencies
        {
            get
            {
                return new HashSet<string>
                {
                    BackgrounderConstants.BackgrounderJavaCollectionName
                };
            }
        }

        public override ICollection<string> WorkbookNames
        {
            get
            {
                return new List<string>
                {
                    "Backgrounder.twb"
                };
            }
        }

        public override IPluginResponse Execute(IPluginRequest pluginRequest)
        {
            pluginResponse = CreatePluginResponse();

            CreateTables();

            Log.Info("Processing Backgrounder job events..");

            IPersister<BackgrounderJob> backgrounderPersister = GetConcurrentCustomPersister<BackgrounderJob>(pluginRequest, BackgrounderPersistenceHelper.PersistBackgrounderJob);
            using (GetPersisterStatusWriter(backgrounderPersister))
            {
                BackgrounderJobProcessor backgrounderJobProcessor = new BackgrounderJobProcessor(MongoDatabase, backgrounderPersister, pluginRequest.LogsetHash);
                backgrounderJobProcessor.ProcessJobs();
                backgrounderPersister.Shutdown();
            }

            Log.Info("Finished processing Backgrounder job events!");

            // Check if we persisted any data.
            if (!PersistedData())
            {
                Log.Info("Failed to persist any data from Backgrounder logs!");
                pluginResponse.GeneratedNoData = true;
            }

            return pluginResponse;
        }

        private void CreateTables()
        {
            using (var dbConnection = GetOutputDatabaseConnection())
            {
                dbConnection.CreateOrMigrateTable<BackgrounderJob>();
                dbConnection.CreateOrMigrateTable<BackgrounderJobError>();
                dbConnection.CreateOrMigrateTable<BackgrounderExtractJobDetail>();
                dbConnection.CreateOrMigrateTable<BackgrounderSubscriptionJobDetail>();
            }
        }
    }
}