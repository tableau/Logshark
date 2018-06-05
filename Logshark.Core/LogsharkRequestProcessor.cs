using log4net;
using Logshark.Common.Extensions;
using Logshark.Common.Helpers;
using Logshark.ConnectionModel.Helpers;
using Logshark.Core.Controller.Initialization;
using Logshark.Core.Controller.Metadata;
using Logshark.Core.Controller.Parsing;
using Logshark.Core.Controller.Parsing.Mongo;
using Logshark.Core.Controller.Plugin;
using Logshark.Core.Controller.Processing;
using Logshark.Core.Controller.Workbook;
using Logshark.Core.Helpers;
using Logshark.Core.Helpers.Timers;
using Logshark.Core.Mongo;
using Logshark.RequestModel;
using Logshark.RequestModel.Config;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Logshark.Core
{
    /// <summary>
    /// Handles all the orchestration logic around how to process a logset from end to end.
    /// </summary>
    public sealed class LogsharkRequestProcessor
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        #region Public Methods

        /// <summary>
        /// Processes a full logset from end-to-end.
        /// </summary>
        /// <param name="request">The user's processing request.</param>
        /// <returns>Run context containing the run outcome and details of what happened during the run.</returns>
        public LogsharkRunContext ProcessRequest(LogsharkRequest request)
        {
            // Clear any cached event timing data.
            GlobalEventTimingData.Clear();

            // Update log4net to contain the CustomId and RunId properties for any consumers which wish to log them.
            LogicalThreadContext.Properties["CustomId"] = request.CustomId;
            LogicalThreadContext.Properties["RunId"] = request.RunId;

            try
            {
                using (new LocalMongoDatabaseManager(request))
                {
                    // Verify all external dependencies are up and available.
                    var serviceDependencyValidator = new ServiceDependencyValidator(request.Configuration);
                    serviceDependencyValidator.ValidateAllDependencies();

                    var metadataWriter = new LogsharkRunMetadataPostgresWriter(request.Configuration.PostgresConnectionInfo);

                    return ExecuteLogsharkRun(request, metadataWriter);
                }
            }
            catch (Exception ex)
            {
                Log.FatalFormat("Logshark run failed: {0}", ex.Message);
                if (!String.IsNullOrWhiteSpace(ex.StackTrace))
                {
                    Log.Debug(ex.StackTrace);
                }

                throw;
            }
        }

        /// <summary>
        /// Display a list of all available plugins to the user.
        /// </summary>
        public static void PrintAvailablePlugins()
        {
            PluginLoader.PrintAvailablePlugins();
        }

        #endregion Public Methods

        #region Private Methods

        /// <summary>
        /// Orchestrates a Logshark run from end to end.
        /// </summary>
        /// <param name="request">The user's processing request.</param>
        /// <param name="metadataWriter">The metadata writer responsible for writing information about the state of the run.</param>
        /// <returns>Run context containing the run outcome and details of what happened during the run.</returns>
        private LogsharkRunContext ExecuteLogsharkRun(LogsharkRequest request, ILogsharkRunMetadataWriter metadataWriter)
        {
            using (var runTimer = new LogsharkTimer("Logshark Run", request.Target, GlobalEventTimingData.Add))
            {
                var run = new LogsharkRunContext(request);
                try
                {
                    Log.InfoFormat("Preparing logset target '{0}' for processing..", request.Target);

                    StartPhase(ProcessingPhase.Initializing, run, metadataWriter);
                    run.InitializationResult = InitializeRun(request);
                    run.IsValidLogset = true;

                    StartPhase(ProcessingPhase.Parsing, run, metadataWriter);
                    run.ParsingResult = ProcessLogset(request, run.InitializationResult);

                    StartPhase(ProcessingPhase.ExecutingPlugins, run, metadataWriter);
                    run.PluginExecutionResult = ExecutePlugins(request, run.InitializationResult);

                    run.SetRunSuccessful();
                    return run;
                }
                catch (Exception ex)
                {
                    run.SetRunFailed(ex);
                    throw;
                }
                finally
                {
                    StartPhase(ProcessingPhase.Complete, run, metadataWriter);
                    TearDown(run);

                    Log.InfoFormat("Logshark run complete! [{0}]", runTimer.Elapsed.Print());
                    string runSummary = run.BuildRunSummary();
                    if (!String.IsNullOrWhiteSpace(runSummary))
                    {
                        Log.Info(runSummary);
                    }
                }
            }
        }

        /// <summary>
        /// Starts the next phase of the given Logshark run and writes any associated metadata.
        /// </summary>
        /// <param name="phaseToStart">The phase to start.</param>
        /// <param name="context">The current Logshark run.</param>
        /// <param name="metadataWriter">The metadata writer responsible for tracking the state of the run.</param>
        private void StartPhase(ProcessingPhase phaseToStart, LogsharkRunContext context, ILogsharkRunMetadataWriter metadataWriter)
        {
            context.CurrentPhase = phaseToStart;
            metadataWriter.WriteMetadata(context);
        }

        /// <summary>
        /// Handles any initialization tasks associated with the run, such as extracting any archives, loading the relevant artifact processor and plugins.
        /// </summary>
        private RunInitializationResult InitializeRun(LogsharkRequest request)
        {
            // Blow out the application temp directory so that we start with as much disk space as possible.
            PurgeTempDirectory(request.Configuration);

            IRunInitializer runInitializer = RunInitializerFactory.GetRunInitializer(request.Target, request.Configuration);
            var initializationRequest = new RunInitializationRequest(request.Target, request.RunId, request.PluginsToExecute, request.ProcessFullLogset, request.Configuration.ArtifactProcessorOptions);

            return runInitializer.Initialize(initializationRequest);
        }

        /// <summary>
        /// Takes action to process a logset based on the current status of the Logset.
        /// </summary>
        private LogsetParsingResult ProcessLogset(LogsharkRequest request, RunInitializationResult runInitializationResult)
        {
            var statusChecker = new LogsetProcessingStatusChecker(request.Configuration.MongoConnectionInfo);
            LogsetProcessingStatus existingProcessedLogsetStatus = statusChecker.GetStatus(runInitializationResult.LogsetHash, runInitializationResult.CollectionsRequested);

            Func<LogsetParsingRequest, LogsetParsingResult> parseLogset = logsetParsingRequest => ParseLogset(logsetParsingRequest, request.Configuration);
            Action<string> dropLogset = logsetHash => MongoAdminHelper.DropDatabase(request.Configuration.MongoConnectionInfo.GetClient(), logsetHash);

            var parsingRequest = new LogsetParsingRequest(runInitializationResult, request.ForceParse);
            ILogsetProcessingStrategy processingStrategy = LogsetProcessingStrategyFactory.GetLogsetProcessingStrategy(request.Target, parseLogset, dropLogset, request.Configuration);

            return processingStrategy.ProcessLogset(parsingRequest, existingProcessedLogsetStatus);
        }

        /// <summary>
        /// Encapsulates extracting and parsing logset.
        /// </summary>
        private LogsetParsingResult ParseLogset(LogsetParsingRequest parsingRequest, LogsharkConfiguration config)
        {
            try
            {
                var parser = new MongoLogsetParser(config.MongoConnectionInfo, config.TuningOptions);
                return parser.ParseLogset(parsingRequest);
            }
            catch (Exception ex)
            {
                Log.FatalFormat("Encountered a fatal error while processing logset: {0}", ex.Message);
                if (ex.InnerException != null)
                {
                    Log.DebugFormat(ex.InnerException.StackTrace);
                }
                throw;
            }
        }

        /// <summary>
        /// Execute plugins requested by the user against an initialized logset.
        /// </summary>
        private PluginExecutionResult ExecutePlugins(LogsharkRequest request, RunInitializationResult initializationResult)
        {
            PublishingOptions publishingOptions = BuildPublishingOptions(request, initializationResult);
            var pluginExecutionRequest = new PluginExecutionRequest(initializationResult, publishingOptions, request.PluginCustomArguments, request.RunId, request.PostgresDatabaseName);

            var pluginExecutor = new PluginExecutor(request.Configuration);

            return pluginExecutor.ExecutePlugins(pluginExecutionRequest);
        }

        /// <summary>
        /// Builds a PublishingOptions object in accordance with the user's request.
        /// </summary>
        private PublishingOptions BuildPublishingOptions(LogsharkRequest request, RunInitializationResult initializationResult)
        {
            return new PublishingOptions(request.PublishWorkbooks, request.ProjectName, BuildProjectDescription(request, initializationResult), request.WorkbookTags);
        }

        /// <summary>
        /// Builds the project description that will be used for any published workbooks.
        /// </summary>
        private string BuildProjectDescription(LogsharkRequest request, RunInitializationResult initializationResult)
        {
            if (!String.IsNullOrWhiteSpace(request.ProjectDescription))
            {
                return request.ProjectDescription;
            }

            var sb = new StringBuilder();
            sb.AppendFormat("Generated from logset <b>'{0}'</b> on {1} by {2}.<br>", request.Target, DateTime.Now.ToString("M/d/yy"), Environment.UserName);
            sb.Append("<br>");
            sb.AppendFormat(" Logset Hash: <b>{0}</b><br>", initializationResult.LogsetHash);
            sb.AppendFormat(" Postgres DB: <b>{0}</b><br>", request.PostgresDatabaseName);
            sb.AppendFormat(" Plugins Run: <b>{0}</b>", String.Join(", ", initializationResult.PluginTypesToExecute.Select(pluginType => pluginType.Name)));

            return sb.ToString();
        }

        /// <summary>
        /// Performs any teardown tasks.
        /// </summary>
        private void TearDown(LogsharkRunContext context)
        {
            // Purge application temp directory to free up disk space for the user.
            PurgeTempDirectory(context.Request.Configuration);

            // Drop logset if user didn't want to retain it, assuming they didn't piggyback on an existing processed logset.
            bool utilizedExistingLogset = context.ParsingResult != null && context.ParsingResult.UtilizedExistingProcessedLogset;
            if (context.Request.DropMongoDBPostRun && utilizedExistingLogset)
            {
                Log.InfoFormat("Dropping Mongo database {0}..", context.InitializationResult);
                try
                {
                    MongoAdminHelper.DropDatabase(context.Request.Configuration.MongoConnectionInfo.GetClient(), context.InitializationResult.LogsetHash);
                }
                catch (Exception ex)
                {
                    Log.ErrorFormat("Failed to clean up Mongo database '{0}': {1}", context.InitializationResult.LogsetHash, ex.Message);
                }
            }
        }

        /// <summary>
        /// Removes all contents of the root application temp directory.
        /// </summary>
        private bool PurgeTempDirectory(LogsharkConfiguration config)
        {
            string tempDirectoryPath = config.ApplicationTempDirectory;
            if (String.IsNullOrWhiteSpace(tempDirectoryPath) || !Directory.Exists(tempDirectoryPath))
            {
                return false;
            }

            try
            {
                DirectoryHelper.DeleteDirectory(tempDirectoryPath);
                return true;
            }
            catch (Exception ex)
            {
                Log.ErrorFormat("Failed to gracefully clean up logsets left over from previous run(s): {0}", ex.Message);
                return false;
            }
        }

        #endregion Private Methods
    }
}