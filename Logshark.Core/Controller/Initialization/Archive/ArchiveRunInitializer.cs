using log4net;
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
    public class ArchiveRunInitializer : IRunInitializer
    {
        private readonly string _applicationTempDirectory;

        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public ArchiveRunInitializer(string applicationTempDirectory)
        {
            _applicationTempDirectory = applicationTempDirectory;
        }

        #region Public Methods

        public RunInitializationResult Initialize(RunInitializationRequest request)
        {
            if (request.Target.Type != LogsetTarget.File && request.Target.Type != LogsetTarget.Directory)
            {
                throw new ArgumentException("Request target must be a file or directory!", nameof(request));
            }

            var extractionResult = ExtractLogset(request.Target);

            var artifactProcessorLoader = new ArchiveArtifactProcessorLoader();
            var artifactProcessor = artifactProcessorLoader.LoadArtifactProcessor(extractionResult.RootLogDirectory);

            var logsetHash = ComputeLogsetHash(extractionResult.RootLogDirectory, artifactProcessor);

            var pluginLoader = new PluginLoader(request.ArtifactProcessorOptions);
            var pluginsToExecute = pluginLoader.LoadPlugins(request.RequestedPlugins, artifactProcessor);

            var extractedTarget = new LogsharkRequestTarget(extractionResult.RootLogDirectory);

            var collectionDependencies = request.ParseFullLogset
                ? GetAllSupportedCollections(artifactProcessor, extractionResult.RootLogDirectory)
                : PluginLoader.GetCollectionDependencies(pluginsToExecute);

            return new RunInitializationResult(extractedTarget, artifactProcessor, collectionDependencies, logsetHash, pluginsToExecute);
        }

        #endregion Public Methods

        #region Protected Methods

        private ExtractionResult ExtractLogset(LogsharkRequestTarget target)
        {
            var runTempDirectory = GetRunTempDirectory();

            var extractionWhitelist = BuildExtractionWhitelist();

            var logsetLocation = PrepareLogsetLocation(target, runTempDirectory, extractionWhitelist);

            var extractor = new LogsetExtractor(extractionWhitelist);
            return extractor.Extract(logsetLocation, runTempDirectory);
        }

        private static ISet<Regex> BuildExtractionWhitelist()
        {
            var artifactProcessorLoader = new ArtifactProcessorLoader();
            var availableArtifactProcessors = artifactProcessorLoader.LoadAllArtifactProcessors();

            ISet<Regex> supportedFilePatterns = new HashSet<Regex>();
            foreach (var processor in availableArtifactProcessors)
            {
                supportedFilePatterns.UnionWith(processor.SupportedFilePatterns);
            }

            return supportedFilePatterns;
        }

        private static string PrepareLogsetLocation(LogsharkRequestTarget target, string runTempDirectory, ISet<Regex> fileWhitelist)
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

        private static string ComputeLogsetHash(string rootLogDirectory, IArtifactProcessor artifactProcessor)
        {
            Log.Info("Computing logset hash..");
            try
            {
                var logsetHash = artifactProcessor.ComputeArtifactHash(rootLogDirectory);

                Log.InfoFormat($"Logset hash is '{logsetHash}'.");
                return logsetHash;
            }
            catch (Exception ex)
            {
                Log.FatalFormat($"Unable to determine logset hash: {ex.Message}");
                throw;
            }
        }

        private static ISet<string> GetAllSupportedCollections(IArtifactProcessor artifactProcessor, string rootLogLocation)
        {
            return artifactProcessor.GetParserFactory(rootLogLocation)
                                    .GetAllParsers()
                                    .Select(parser => parser.CollectionSchema.CollectionName)
                                    .ToHashSet();
        }

        /// <summary>
        /// Retrieves an absolute path to a folder where temporary files associated with a single run can be stored.
        /// </summary>
        private string GetRunTempDirectory()
        {
            var currentTimestamp = DateTime.Now.ToString("yyMMddHHmmssff");
            return Path.Combine(_applicationTempDirectory, currentTimestamp);
        }

        #endregion Protected Methods
    }
}