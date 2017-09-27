using log4net;
using Logshark.ArtifactProcessorModel;
using Logshark.Common.Extensions;
using Logshark.ConnectionModel.Helpers;
using Logshark.Core.Controller;
using Logshark.Core.Controller.Metadata.Logset.Mongo;
using Logshark.Core.Controller.Metadata.Run;
using Logshark.Core.Controller.Parsing;
using Logshark.Core.Exceptions;
using Logshark.Core.Mongo;
using Logshark.RequestModel;
using Logshark.RequestModel.RunContext;
using System;
using System.Reflection;

namespace Logshark.Core
{
    /// <summary>
    /// Handles all the business logic around how to use the controller to process a LogsharkRequest from end to end.
    /// </summary>
    public class LogsharkRequestProcessor
    {
        protected readonly LogsharkRunMetadataWriter metadataWriter;

        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public LogsharkRequestProcessor(LogsharkRunMetadataWriter metadataWriter)
        {
            this.metadataWriter = metadataWriter;
        }

        #region Public Methods

        /// <summary>
        /// Display a list of all available plugins to the user.
        /// </summary>
        public static void PrintAvailablePlugins()
        {
            LogsharkController.PrintAvailablePlugins();
        }

        /// <summary>
        /// Processes a full logset from end-to-end.
        /// </summary>
        public virtual void ProcessRequest(LogsharkRequest request)
        {
            var runTimer = request.RunContext.CreateTimer("Logshark Run", request.Target);

            // Update log4net to contain the CustomId and RunId properties for any consumers which wish to log them.
            LogicalThreadContext.Properties["CustomId"] = request.CustomId;
            LogicalThreadContext.Properties["RunId"] = request.RunId;

            LocalMongoProcessManager localMongoProcessManager = StartLocalMongoIfRequested(request);
            request.RunContext.CurrentPhase = ProcessingPhase.Pending;

            try
            {
                ExtractLogset(request);
                IArtifactProcessor artifactProcessor = InitializeArtifactProcessor(request);
                ProcessLogset(request, artifactProcessor);
                ExecutePlugins(request);
                SetRunSuccess(request);
            }
            catch (Exception ex)
            {
                SetRunFailed(request, ex);
                throw;
            }
            finally
            {
                LogsharkController.TearDown(request);
                StopLocalMongoIfRequested(request, localMongoProcessManager);

                runTimer.Stop();
                Log.InfoFormat("Logshark run complete! [{0}]", runTimer.Elapsed.Print());
                LogsharkController.DisplayRunSummary(request);
            }
        }

        #endregion Public Methods

        /// <summary>
        /// Spin up local MongoDB instance if the user requested it.
        /// </summary>
        protected LocalMongoProcessManager StartLocalMongoIfRequested(LogsharkRequest request)
        {
            LocalMongoProcessManager localMongoProcessManager = null;
            if (request.StartLocalMongo)
            {
                localMongoProcessManager = LogsharkController.StartLocalMongoDbInstance(request);
            }

            return localMongoProcessManager;
        }

        #region Protected Methods

        protected IArtifactProcessor InitializeArtifactProcessor(LogsharkRequest request)
        {
            StartPhase(request, ProcessingPhase.Initializing);
            IArtifactProcessor artifactProcessor = LogsharkController.InitializeArtifactProcessor(request);
            metadataWriter.WriteCustomMetadata(request);
            return artifactProcessor;
        }

        /// <summary>
        /// Takes action to process a logset based on the current status of the Logset.
        /// </summary>
        protected void ProcessLogset(LogsharkRequest request, IArtifactProcessor artifactProcessor)
        {
            LogsetStatus existingProcessedLogsetStatus = LogsharkController.GetExistingLogsetStatus(request);

            if (request.ForceParse && !request.Target.IsHashId)
            {
                // If we are forcing a reparsing of this logset, first drop any existing logset in our MongoCluster which matches this hash-id.
                if (existingProcessedLogsetStatus != LogsetStatus.NonExistent)
                {
                    Log.InfoFormat("'Force Parse' request issued, dropping existing Mongo database '{0}'..", request.RunContext.MongoDatabaseName);
                    MongoAdminUtil.DropDatabase(request.Configuration.MongoConnectionInfo.GetClient(), request.RunContext.MongoDatabaseName);
                }

                ParseLogset(request, artifactProcessor);
                return;
            }

            switch (existingProcessedLogsetStatus)
            {
                case LogsetStatus.NonExistent:
                    if (request.Target.IsHashId)
                    {
                        request.RunContext.IsValidLogset = false;
                        throw new InvalidTargetHashException(String.Format("No logset exists that matches logset hash '{0}'. Aborting..", request.RunContext.LogsetHash));
                    }
                    ParseLogset(request, artifactProcessor);
                    break;

                case LogsetStatus.Corrupt:
                    if (request.Target.IsHashId)
                    {
                        request.RunContext.IsValidLogset = false;
                        throw new InvalidTargetHashException(String.Format("Mongo database matching logset hash '{0}' exists but is corrupted. Aborting..", request.RunContext.LogsetHash));
                    }
                    Log.InfoFormat("Logset matching hash '{0}' exists but is corrupted. Dropping it and reprocessing..", request.RunContext.MongoDatabaseName);
                    MongoAdminUtil.DropDatabase(request.Configuration.MongoConnectionInfo.GetClient(), request.RunContext.MongoDatabaseName);
                    ParseLogset(request, artifactProcessor);
                    break;

                case LogsetStatus.InFlight:
                    string collisionErrorMessage = String.Format("Logset matching hash '{0}' exists but is currently being processed by another user.  Aborting..", request.RunContext.MongoDatabaseName);
                    Log.InfoFormat(collisionErrorMessage);
                    throw new ProcessingUserCollisionException(collisionErrorMessage);

                case LogsetStatus.Incomplete:
                    if (request.Target.IsHashId)
                    {
                        throw new InvalidTargetHashException("Found existing logset matching hash, but it is a partial logset that does not contain all of the data required to run specified plugins. Aborting..");
                    }
                    MongoAdminUtil.DropDatabase(request.Configuration.MongoConnectionInfo.GetClient(), request.RunContext.MongoDatabaseName);
                    Log.Info("Found existing logset matching hash, but it is a partial logset that does not contain all of the data required to run specified plugins. Dropping it and reprocessing..");
                    ParseLogset(request, artifactProcessor);
                    break;

                case LogsetStatus.Indeterminable:
                    throw new IndeterminableLogsetStatusException("Unable to determine status of logset. Aborting..");

                case LogsetStatus.Valid:
                    request.RunContext.UtilizedExistingProcessedLogset = true;
                    request.RunContext.IsValidLogset = true;
                    Log.Info("Found existing logset matching hash, skipping extraction and parsing.");
                    LogsetMetadataReader.SetLogsetType(request);
                    LogsetMetadataReader.SetLogsetSize(request);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(String.Format("'{0}' is not a valid LogsetStatus!", existingProcessedLogsetStatus));
            }

            LogsharkController.ValidateMongoDatabaseContainsData(request);
        }

        /// <summary>
        /// Encapsulates extracting and parsing logset.
        /// </summary>
        protected void ParseLogset(LogsharkRequest request, IArtifactProcessor artifactProcessor)
        {
            StartPhase(request, ProcessingPhase.Parsing);
            try
            {
                LogsharkController.ParseLogset(request, artifactProcessor.GetParserFactory(request.RunContext.RootLogDirectory));
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
        /// Encapsulates extracting and parsing logset.
        /// </summary>
        protected void ExtractLogset(LogsharkRequest request)
        {
            StartPhase(request, ProcessingPhase.Extracting);
            try
            {
                LogsharkController.ExtractLogFiles(request);
            }
            catch (Exception ex)
            {
                Log.FatalFormat("Encountered a fatal error while extracting logset: {0}", ex.Message);
                if (ex.InnerException != null)
                {
                    Log.DebugFormat(ex.InnerException.StackTrace);
                }
                throw;
            }
        }

        protected void ExecutePlugins(LogsharkRequest request)
        {
            // Execute plugins.
            StartPhase(request, ProcessingPhase.ExecutingPlugins);
            LogsharkController.ExecutePlugins(request);
            metadataWriter.WritePluginExecutionMetadata(request);
        }

        protected void SetRunSuccess(LogsharkRequest request)
        {
            request.RunContext.IsRunSuccessful = true;
            StartPhase(request, ProcessingPhase.Complete);
        }

        protected void SetRunFailed(LogsharkRequest request, Exception ex)
        {
            request.RunContext.IsRunSuccessful = false;
            request.RunContext.RunFailureExceptionType = ex.GetType().FullName;
            request.RunContext.RunFailurePhase = request.RunContext.CurrentPhase;
            request.RunContext.RunFailureReason = ex.Message;

            if (ex is InvalidLogsetException || ex is InvalidTargetHashException)
            {
                request.RunContext.IsValidLogset = false;
            }

            Log.DebugFormat("Logshark run failed: {0}", ex.Message);
            if (!String.IsNullOrWhiteSpace(ex.StackTrace))
            {
                Log.Debug(ex.StackTrace);
            }

            StartPhase(request, ProcessingPhase.Complete);
        }

        protected void StopLocalMongoIfRequested(LogsharkRequest request, LocalMongoProcessManager localMongoProcessManager)
        {
            if (request.StartLocalMongo)
            {
                LogsharkController.ShutDownLocalMongoDbInstance(localMongoProcessManager);
            }
        }

        protected void StartPhase(LogsharkRequest request, ProcessingPhase phaseToStart)
        {
            request.RunContext.CurrentPhase = phaseToStart;
            metadataWriter.UpdateMetadata(request);
        }

        #endregion Protected Methods
    }
}