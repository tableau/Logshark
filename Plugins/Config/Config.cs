using Logshark.ArtifactProcessors.TableauServerLogProcessor.Parsers;
using Logshark.ArtifactProcessors.TableauServerLogProcessor.PluginInterfaces;
using Logshark.PluginLib.Model.Impl;
using Logshark.PluginModel.Model;
using Logshark.Plugins.Config.Helpers;
using Logshark.Plugins.Config.Model;
using System;
using System.Collections.Generic;

namespace Logshark.Plugins.Config
{
    public class Config : BaseWorkbookCreationPlugin, IServerClassicPlugin, IServerTsmPlugin
    {
        public override ISet<string> CollectionDependencies => new HashSet<string> { ParserConstants.ConfigCollectionName };
        public override ICollection<string> WorkbookNames => new HashSet<string>  { "Config.twbx" };

        public Config() { }
        public Config(IPluginRequest request) : base(request) { }

        public override IPluginResponse Execute()
        {
            var pluginResponse = CreatePluginResponse();

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
                var errorMessage = $"Failed to load configuration information from MongoDB: {ex.Message}";
                Log.ErrorFormat(errorMessage);
                pluginResponse.SetExecutionOutcome(isSuccessful: false, failureReason: errorMessage);

                return pluginResponse;
            }

            // Persist config data.
            try
            {
                var persistedData = PersistConfigData(configEntries, configProcessInfo);

                if (!persistedData)
                {
                    Log.Info("Failed to persist any data from Config logs!");
                    pluginResponse.GeneratedNoData = true;
                }
            }
            catch (Exception ex)
            {
                var errorMessage = $"Failed to persist configuration information: {ex.Message}";
                Log.ErrorFormat(errorMessage);
                pluginResponse.SetExecutionOutcome(isSuccessful: false, failureReason: errorMessage);
            }

            return pluginResponse;
        }

        protected bool PersistConfigData(ICollection<ConfigEntry> configEntries, ICollection<ConfigProcessInfo> workerDetails)
        {
            Log.InfoFormat($"Persisting configuration data for {configEntries.Count} config key entries and {workerDetails.Count} worker processes..");

            var persistedData = false;

            using (var extract = ExtractFactory.CreateExtract<ConfigEntry>("ConfigEntries.hyper"))
            using (GetPersisterStatusWriter(extract, configEntries.Count))
            {
                extract.Enqueue(configEntries);
                persistedData = persistedData || extract.ItemsPersisted > 0;
            }

            using (var extract = ExtractFactory.CreateExtract<ConfigProcessInfo>("ProcessTopology.hyper"))
            using (GetPersisterStatusWriter(extract, workerDetails.Count))
            {
                extract.Enqueue(workerDetails);
                persistedData = persistedData || extract.ItemsPersisted > 0;
            }

            return persistedData;
        }
    }
}