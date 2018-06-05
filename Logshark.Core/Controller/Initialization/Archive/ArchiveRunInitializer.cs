﻿using log4net;
using Logshark.ArtifactProcessorModel;
using Logshark.Common.Extensions;
using Logshark.Common.Helpers;
using Logshark.Core.Controller.Initialization.Archive.Extraction;
using Logshark.Core.Controller.Initialization.ArtifactProcessor;
using Logshark.Core.Controller.Plugin;
using Logshark.RequestModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Logshark.Core.Controller.Initialization.Archive
{
    internal class ArchiveRunInitializer : IRunInitializer
    {
        protected readonly string applicationTempDirectory;

        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public ArchiveRunInitializer(string applicationTempDirectory)
        {
            this.applicationTempDirectory = applicationTempDirectory;
        }

        #region Public Methods

        public RunInitializationResult Initialize(RunInitializationRequest request)
        {
            if (request.Target.Type != LogsetTarget.File && request.Target.Type != LogsetTarget.Directory)
            {
                throw new ArgumentException("Request target must be a file or directory!", "request");
            }

            ExtractionResult extractionResult = ExtractLogset(request.Target, request.RunId);

            var artifactProcessorLoader = new ArchiveArtifactProcessorLoader();
            IArtifactProcessor artifactProcessor = artifactProcessorLoader.LoadArtifactProcessor(extractionResult.RootLogDirectory);

            string logsetHash = ComputeLogsetHash(extractionResult.RootLogDirectory, artifactProcessor);

            var pluginLoader = new PluginLoader(request.ArtifactProcessorOptions);
            ISet<Type> pluginsToExecute = pluginLoader.LoadPlugins(request.RequestedPlugins, artifactProcessor);

            var extractedTarget = new LogsharkRequestTarget(extractionResult.RootLogDirectory);

            ISet<string> collectionDependencies;
            if (request.ParseFullLogset)
            {
                collectionDependencies = GetAllSupportedCollections(artifactProcessor, extractionResult.RootLogDirectory);
            }
            else
            {
                collectionDependencies = pluginLoader.GetCollectionDependencies(pluginsToExecute);
            }

            return new RunInitializationResult(extractedTarget, artifactProcessor, collectionDependencies, logsetHash, pluginsToExecute);
        }

        #endregion Public Methods

        #region Protected Methods

        protected ExtractionResult ExtractLogset(LogsharkRequestTarget target, string runId)
        {
            string runTempDirectory = GetRunTempDirectory(runId);

            ISet<Regex> extractionWhitelist = BuildExtractionWhitelist();

            string logsetLocation = PrepareLogsetLocation(target, runTempDirectory, extractionWhitelist);

            var extractor = new LogsetExtractor(extractionWhitelist);
            return extractor.Extract(logsetLocation, runTempDirectory);
        }

        protected ISet<Regex> BuildExtractionWhitelist()
        {
            var artifactProcessorLoader = new ArtifactProcessorLoader();
            ISet<IArtifactProcessor> availableArtifactProcessors = artifactProcessorLoader.LoadAllArtifactProcessors();

            ISet<Regex> supportedFilePatterns = new HashSet<Regex>();
            foreach (var processor in availableArtifactProcessors)
            {
                supportedFilePatterns.UnionWith(processor.SupportedFilePatterns);
            }

            return supportedFilePatterns;
        }

        protected string PrepareLogsetLocation(LogsharkRequestTarget target, string runTempDirectory, ISet<Regex> fileWhitelist)
        {
            // If target is a directory and/or exists on a remote drive, make a local copy to avoid
            // destructive operations on the original & possibly improve extraction speed.
            if (target.Type == LogsetTarget.Directory || !PathHelper.ExistsOnLocalDrive(target))
            {
                var logsetCopier = new LogsetCopier(fileWhitelist);
                return logsetCopier.CopyLogset(target, runTempDirectory);
            }

            return target;
        }

        protected string ComputeLogsetHash(string rootLogDirectory, IArtifactProcessor artifactProcessor)
        {
            Log.Info("Computing logset hash..");
            try
            {
                string logsetHash = artifactProcessor.ComputeArtifactHash(rootLogDirectory);

                Log.InfoFormat("Logset hash is '{0}'.", logsetHash);
                return logsetHash;
            }
            catch (Exception ex)
            {
                Log.FatalFormat("Unable to determine logset hash: {0}", ex.Message);
                throw;
            }
        }

        protected ISet<string> GetAllSupportedCollections(IArtifactProcessor artifactProcessor, string rootLogLocation)
        {
            return artifactProcessor.GetParserFactory(rootLogLocation)
                                    .GetAllParsers()
                                    .Select(parser => parser.CollectionSchema.CollectionName)
                                    .ToHashSet();
        }

        /// <summary>
        /// Retrieves an absolute path to a folder where temporary files associated with a single run can be stored.
        /// </summary>
        protected string GetRunTempDirectory(string runId)
        {
            return Path.Combine(applicationTempDirectory, runId);
        }

        #endregion Protected Methods
    }
}