using Logshark.ArtifactProcessorModel;
using Logshark.ConnectionModel.Mongo;
using Logshark.Core.Controller.Initialization.ArtifactProcessor;
using Logshark.Core.Controller.Parsing.Mongo.Metadata;
using Logshark.Core.Controller.Plugin;
using Logshark.Core.Exceptions;
using Logshark.RequestModel;
using System;
using System.Collections.Generic;

namespace Logshark.Core.Controller.Initialization.Hash
{
    internal class HashRunInitializer : IRunInitializer
    {
        protected readonly MongoConnectionInfo mongoConnectionInfo;

        public HashRunInitializer(MongoConnectionInfo mongoConnectionInfo)
        {
            this.mongoConnectionInfo = mongoConnectionInfo;
        }

        public RunInitializationResult Initialize(RunInitializationRequest request)
        {
            if (request.Target.Type != LogsetTarget.Hash)
            {
                throw new ArgumentException("Request target must be a logset hash!", "request");
            }

            var metadataReader = new MongoLogProcessingMetadataWriter(mongoConnectionInfo);
            LogProcessingMetadata logsetMetadata = metadataReader.Read(request.Target);
            if (logsetMetadata == null)
            {
                throw new InvalidTargetHashException(String.Format("No logset exists that matches logset hash '{0}'. Aborting..", request.Target));
            }

            var artifactProcessorLoader = new HashArtifactProcessorLoader();
            IArtifactProcessor artifactProcessor = artifactProcessorLoader.LoadArtifactProcessor(logsetMetadata.ArtifactProcessorType);

            var pluginLoader = new PluginLoader(request.ArtifactProcessorOptions);
            ISet<Type> pluginsToExecute = pluginLoader.LoadPlugins(request.RequestedPlugins, artifactProcessor);

            return new RunInitializationResult(request.Target, artifactProcessor, logsetMetadata.CollectionsParsed, request.Target, pluginsToExecute);
        }
    }
}