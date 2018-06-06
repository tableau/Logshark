using Logshark.Core.Controller.Initialization;
using Logshark.Core.Controller.Workbook;
using System;
using System.Collections.Generic;

namespace Logshark.Core.Controller.Plugin
{
    internal class PluginExecutionRequest
    {
        public string LogsetHash { get; protected set; }

        public string MongoDatabaseName { get; protected set; }

        public IDictionary<string, object> PluginArguments { get; protected set; }

        public ICollection<Type> PluginsToExecute { get; protected set; }

        public string PostgresDatabaseName { get; protected set; }

        public PublishingOptions PublishingOptions { get; protected set; }

        public string RunId { get; protected set; }

        public PluginExecutionRequest(RunInitializationResult initializationResult, PublishingOptions publishingOptions, IDictionary<string, object> pluginArguments, string runId, string postgresDatabaseName)
        {
            LogsetHash = initializationResult.LogsetHash;
            MongoDatabaseName = initializationResult.LogsetHash;
            PluginArguments = pluginArguments;
            PluginsToExecute = initializationResult.PluginTypesToExecute;
            PostgresDatabaseName = postgresDatabaseName;
            PublishingOptions = publishingOptions;
            RunId = runId;
        }
    }
}