using Logshark.ArtifactProcessorModel;
using Logshark.RequestModel;
using System;
using System.Collections.Generic;

namespace Logshark.Core.Controller.Initialization
{
    public class RunInitializationResult
    {
        public IArtifactProcessor ArtifactProcessor { get; protected set; }

        public Version ArtifactProcessorVersion { get; protected set; }

        // The log collections requested by the user.
        public ISet<string> CollectionsRequested { get; protected set; }

        public string LogsetHash { get; protected set; }

        // The concrete plugin types that were loaded as a part of initialization.
        public ISet<Type> PluginTypesToExecute { get; set; }

        public LogsharkRequestTarget Target { get; protected set; }

        public RunInitializationResult(LogsharkRequestTarget target, IArtifactProcessor artifactProcessor, ISet<string> collectionsRequested, string logsetHash, ISet<Type> pluginsToExecute)
        {
            Target = target;
            ArtifactProcessor = artifactProcessor;
            ArtifactProcessorVersion = artifactProcessor.GetType().Assembly.GetName().Version;
            CollectionsRequested = collectionsRequested;
            CollectionsRequested.UnionWith(artifactProcessor.RequiredCollections);
            LogsetHash = logsetHash;
            PluginTypesToExecute = pluginsToExecute;
        }
    }
}