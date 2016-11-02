using Logshark.PluginLib.Extensions;
using Logshark.PluginLib.Model;
using Logshark.PluginLib.Model.Impl;
using Logshark.Plugins.Config.Helpers;
using Logshark.Plugins.Config.Model;
using System;
using System.Collections.Generic;

namespace Logshark.Plugins.Config
{
    public class Config : BaseWorkbookCreationPlugin, IServerPlugin
    {
        private PluginResponse pluginResponse;

        public static readonly string ConfigCollectionName = "config";

        public override ISet<string> CollectionDependencies
        {
            get
            {
                return new HashSet<string>
                {
                    ConfigCollectionName
                };
            }
        }

        public override ICollection<string> WorkbookNames
        {
            get
            {
                return new List<string>
                {
                    "Config.twb"
                };
            }
        }

        public override IPluginResponse Execute(IPluginRequest request)
        {
            pluginResponse = CreatePluginResponse();

            ICollection<ConfigEntry> configEntries = null;
            ICollection<ConfigProcessInfo> configProcessInfo = null;

            // Load config data.
            try
            {
                // Load config into memory.
                var configReader = new ConfigReader(MongoDatabase);

                configEntries = configReader.GetConfigEntries();
                configProcessInfo = configReader.GetConfigProcessInfo();
            }
            catch (Exception ex)
            {
                Log.ErrorFormat("Failed to load configuration information from MongoDB: {0}", ex.Message);
            }

            // Persist config data.
            try
            {
                PersistConfigEntryData(configEntries, request);
                PersistConfigProcessInfoData(configProcessInfo, request);
            }
            catch (Exception ex)
            {
                string errorMessage = String.Format("Failed to persist configuration information to database: {0}", ex.Message);
                Log.Error(errorMessage);
                pluginResponse.SetExecutionOutcome(isSuccessful: false, failureReason: errorMessage);
            }

            // Check if we persisted any data.
            if (!PersistedData())
            {
                Log.Info("Failed to persist any data from Config logs!");
                pluginResponse.GeneratedNoData = true;
            }

            return pluginResponse;
        }

        protected void PersistConfigEntryData(ICollection<ConfigEntry> configEntries, IPluginRequest pluginRequest)
        {
            if (configEntries == null)
            {
                return;
            }

            Log.InfoFormat("Persisting configuration entry data for {0} keys to database..", configEntries.Count);
            GetOutputDatabaseConnection().CreateOrMigrateTable<ConfigEntry>();

            var configEntryPersister = GetConcurrentBatchPersister<ConfigEntry>(pluginRequest);
            using (GetPersisterStatusWriter(configEntryPersister, configEntries.Count))
            {
                configEntryPersister.Enqueue(configEntries);
                configEntryPersister.Shutdown();
            }
        }

        protected void PersistConfigProcessInfoData(ICollection<ConfigProcessInfo> workerDetails, IPluginRequest pluginRequest)
        {
            if (workerDetails == null)
            {
                return;
            }

            Log.InfoFormat("Persisting configuration topology data for {0} processes to database..", workerDetails.Count);
            GetOutputDatabaseConnection().CreateOrMigrateTable<ConfigProcessInfo>();

            var configProcessInfoPersister = GetConcurrentBatchPersister<ConfigProcessInfo>(pluginRequest);
            using (GetPersisterStatusWriter(configProcessInfoPersister, workerDetails.Count))
            {
                configProcessInfoPersister.Enqueue(workerDetails);
                configProcessInfoPersister.Shutdown();
            }
        }
    }
}