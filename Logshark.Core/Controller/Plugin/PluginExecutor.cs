using log4net;
using Logshark.Common.Extensions;
using Logshark.ConnectionModel.Mongo;
using Logshark.ConnectionModel.Postgres;
using Logshark.ConnectionModel.TableauServer;
using Logshark.Core.Controller.Workbook;
using Logshark.Core.Exceptions;
using Logshark.Core.Helpers.Timers;
using Logshark.PluginLib.Model;
using Logshark.PluginLib.Model.Impl;
using Logshark.PluginModel.Model;
using Logshark.RequestModel.Config;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using Tableau.RestApi;
using Tableau.RestApi.Model;

namespace Logshark.Core.Controller.Plugin
{
    /// <summary>
    ///  Handles execution of plugins.
    /// </summary>
    internal class PluginExecutor
    {
        protected readonly MongoConnectionInfo mongoConnectionInfo;
        protected readonly PostgresConnectionInfo postgresConnectionInfo;
        protected readonly TableauServerConnectionInfo tableauConnectionInfo;

        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public PluginExecutor(LogsharkConfiguration config)
        {
            mongoConnectionInfo = config.MongoConnectionInfo;
            postgresConnectionInfo = config.PostgresConnectionInfo;
            tableauConnectionInfo = config.TableauConnectionInfo;
        }

        #region Public Methods

        /// <summary>
        /// Execute all requested plugins.
        /// </summary>
        public PluginExecutionResult ExecutePlugins(PluginExecutionRequest request)
        {
            string pluginOutputLocation = GetOutputLocation(request.RunId);
            if (!Directory.Exists(pluginOutputLocation))
            {
                Directory.CreateDirectory(pluginOutputLocation);
            }

            // Execute all plugins.
            using (new LogsharkTimer("Executed Plugins", GlobalEventTimingData.Add))
            {
                ICollection<IPluginResponse> pluginResponses = ExecutePlugins(request.PluginsToExecute, request);

                int failures = pluginResponses.Count(pluginResponse => !pluginResponse.SuccessfulExecution);
                Log.InfoFormat("Finished executing plugins! [{0} {1}]", failures, "failure".Pluralize(failures));

                return new PluginExecutionResult(request.PluginsToExecute, pluginResponses, pluginOutputLocation);
            }
        }

        /// <summary>
        /// Get the output location where a Logshark run stores any output artifacts.
        /// </summary>
        /// <param name="runId">The ID of the Logshark run.</param>
        /// <returns>Absolute path to the directory where Logshark output is stored.</returns>
        public static string GetOutputLocation(string runId)
        {
            string outputDirectory = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Output");
            if (!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            return Path.Combine(outputDirectory, runId);
        }

        #endregion Public Methods

        #region Protected Methods

        /// <summary>
        /// Executes multiple plugins.
        /// </summary>
        protected ICollection<IPluginResponse> ExecutePlugins(IEnumerable<Type> pluginsToExecute, PluginExecutionRequest request)
        {
            var pluginResponses = new List<IPluginResponse>();

            foreach (Type plugin in pluginsToExecute.OrderBy(PluginLoader.IsPostExecutionPlugin))
            {
                IPluginResponse pluginResponse = ExecutePlugin(plugin, request, pluginResponses);
                pluginResponses.Add(pluginResponse);
            }

            return pluginResponses;
        }

        /// <summary>
        /// Executes a single plugin.
        /// </summary>
        /// <param name="pluginType">The type of the plugin to execute.</param>
        /// <param name="pluginExecutionRequest">Plugin execution options.</param>
        /// <param name="previousPluginResponses">The set of plugin responses associated with the current run. Used for plugin chaining.</param>
        /// <returns>Response containing state about the success/failure of the plugin's execution.</returns>
        protected IPluginResponse ExecutePlugin(Type pluginType, PluginExecutionRequest pluginExecutionRequest, IEnumerable<IPluginResponse> previousPluginResponses)
        {
            string pluginName = pluginType.Name;

            // Setup plugin for execution.
            IPluginRequest pluginRequest = CreatePluginRequest(pluginType, pluginExecutionRequest);
            var pluginTimer = new LogsharkTimer("Executed Plugin", pluginName, GlobalEventTimingData.Add);

            // Execute plugin.
            IPluginResponse pluginResponse = new PluginResponse(pluginName);
            try
            {
                string outputDatabaseName = GetOutputDatabaseName(pluginName, pluginRequest, pluginExecutionRequest);
                IPlugin plugin = InitializePlugin(pluginType, pluginExecutionRequest.MongoDatabaseName, outputDatabaseName, previousPluginResponses);

                Log.InfoFormat("Execution of {0} plugin started at {1}..", pluginName, DateTime.Now.ToString("h:mm tt", CultureInfo.InvariantCulture));
                pluginResponse = plugin.Execute(pluginRequest);

                // Flush any workbooks, if this was a workbook creation plugin.
                if (plugin is IWorkbookCreationPlugin)
                {
                    string workbookOutputDirectory = GetOutputLocation(pluginExecutionRequest.RunId);
                    IEnumerable<string> workbookFilePaths = WriteWorkbooksToDisk(workbookOutputDirectory, plugin as IWorkbookCreationPlugin, pluginResponse, outputDatabaseName);
                    pluginResponse.WorkbooksOutput.AddRange(workbookFilePaths);
                }

                // Publish any associated workbooks, if requested.
                if (pluginExecutionRequest.PublishingOptions != null && pluginExecutionRequest.PublishingOptions.PublishWorkbooks)
                {
                    var workbookPublisher = new WorkbookPublisher(
                        tableauConnectionInfo,
                        postgresConnectionInfo,
                        pluginExecutionRequest.PublishingOptions,
                        new RestApiRequestor(
                            tableauConnectionInfo.Uri,
                            tableauConnectionInfo.Username,
                            tableauConnectionInfo.Password,
                            tableauConnectionInfo.Site));
                    ICollection<PublishedWorkbookResult> workbooksPublished = workbookPublisher.PublishWorkbooks(pluginResponse);
                    pluginResponse.WorkbooksPublished.AddRange(workbooksPublished);
                }
            }
            catch (PluginInitializationException ex)
            {
                string errorMessage = String.Format("Failed to initialize {0} plugin: {1}", pluginName, ex.Message);
                HandlePluginExecutionFailure(pluginResponse, errorMessage, ex);
            }
            catch (PublishingException ex)
            {
                string errorMessage = String.Format("Failed to publish workbooks: {0}", ex.Message);
                HandlePluginExecutionFailure(pluginResponse, errorMessage, ex);
            }
            catch (Exception ex)
            {
                string errorMessage = String.Format("Encountered uncaught exception while executing plugin '{0}': {1}", pluginName, ex.GetFlattenedMessage());
                HandlePluginExecutionFailure(pluginResponse, errorMessage, ex);
            }
            finally
            {
                pluginTimer.Stop();
                pluginResponse.PluginRunTime = pluginTimer.Elapsed;

                LogExecutionOutcome(pluginResponse);
            }

            return pluginResponse;
        }

        /// <summary>
        /// Instantiates a plugin request for the given plugin type.
        /// </summary>
        /// <param name="pluginType">The type of the plugin.</param>
        /// <param name="pluginExecutionRequest">The options associated with the plugin request.</param>
        /// <returns>IPluginRequest with all appropriate state initialized.</returns>
        protected IPluginRequest CreatePluginRequest(Type pluginType, PluginExecutionRequest pluginExecutionRequest)
        {
            Guid logsetHash = Guid.Parse(pluginExecutionRequest.LogsetHash);
            string outputDirectory = GetOutputLocation(pluginExecutionRequest.RunId);
            var pluginRequest = new PluginRequest(logsetHash, outputDirectory, pluginExecutionRequest.RunId);

            // Append all global and plugin specific arguments to the plugin argument map.
            foreach (string argumentKey in pluginExecutionRequest.PluginArguments.Keys)
            {
                if (argumentKey.StartsWith(pluginType.Name + ".", StringComparison.InvariantCultureIgnoreCase)
                    || argumentKey.StartsWith("Global.", StringComparison.InvariantCultureIgnoreCase))
                {
                    pluginRequest.SetRequestArgument(argumentKey, pluginExecutionRequest.PluginArguments[argumentKey]);
                }
            }

            return pluginRequest;
        }

        /// <summary>
        /// Calculates the output database name for the given plugin.  If no plugin-specific database is requested, defer to the global name.
        /// </summary>
        protected string GetOutputDatabaseName(string pluginName, IPluginRequest pluginRequest, PluginExecutionRequest pluginExecutionRequest)
        {
            string pluginDatabaseNameRequestArgumentKey = String.Format("{0}.DatabaseName", pluginName);
            if (pluginRequest.ContainsRequestArgument(pluginDatabaseNameRequestArgumentKey))
            {
                string requestedPluginDatabaseName = pluginRequest.GetRequestArgument(pluginDatabaseNameRequestArgumentKey).ToString();
                if (!String.IsNullOrWhiteSpace(requestedPluginDatabaseName))
                {
                    Log.InfoFormat("Redirecting output from the {0} plugin to user-requested database '{1}'.", pluginName, requestedPluginDatabaseName);
                    return requestedPluginDatabaseName;
                }
            }

            return pluginExecutionRequest.PostgresDatabaseName;
        }

        /// <summary>
        /// Handles initialization of an IPlugin object for a given plugin type.
        /// </summary>
        protected IPlugin InitializePlugin(Type pluginType, string mongoDatabaseName, string postgresDatabaseName, IEnumerable<IPluginResponse> previousPluginResponses)
        {
            Log.InfoFormat("Initializing {0} plugin..", pluginType.Name);

            try
            {
                // Create plugin object.
                var plugin = (IPlugin)Activator.CreateInstance(pluginType);

                // Set database connections.
                plugin.MongoDatabase = mongoConnectionInfo.GetDatabase(mongoDatabaseName);
                plugin.OutputDatabaseConnectionFactory = postgresConnectionInfo.GetConnectionFactory(postgresDatabaseName);

                if (PluginLoader.IsPostExecutionPlugin(pluginType))
                {
                    IPostExecutionPlugin postExecutionPlugin = (IPostExecutionPlugin)plugin;
                    postExecutionPlugin.PluginResponses = previousPluginResponses;
                    return postExecutionPlugin;
                }
                else
                {
                    return plugin;
                }
            }
            catch (Exception ex)
            {
                throw new PluginInitializationException(ex.Message, ex);
            }
        }

        /// <summary>
        /// Writes all workbooks associated with a workbook creation plugin to the output directory.
        /// </summary>
        /// <param name="outputLocation">The absolute path where the workbooks should be written.</param>
        /// <param name="plugin">The workbook creation plugin that ran.</param>
        /// <param name="response">The response object, containing information about the results of executing the plugin.</param>
        /// <param name="postgresDatabaseName">The name of the Postgres database that the workbook should connect to.</param>
        protected IEnumerable<string> WriteWorkbooksToDisk(string outputLocation, IWorkbookCreationPlugin plugin, IPluginResponse response, string postgresDatabaseName)
        {
            var workbookFilePaths = new List<string>();

            // Make sure we have something to do.
            if (!response.SuccessfulExecution)
            {
                return workbookFilePaths;
            }
            if (response.GeneratedNoData)
            {
                Log.InfoFormat("Skipped saving workbooks for {0} because the plugin did not generate any backing data.", plugin.GetType().Name);
                return workbookFilePaths;
            }

            // Replace connection information in workbook and flush to disk.
            if (plugin.WorkbookNames != null)
            {
                foreach (var workbookName in plugin.WorkbookNames)
                {
                    WorkbookEditor workbookEditor = new WorkbookEditor(workbookName, plugin.GetWorkbookXml(workbookName));
                    workbookEditor.ReplacePostgresConnections(postgresConnectionInfo, postgresDatabaseName);
                    workbookEditor.RemoveThumbnails();

                    string workbookFilePath = workbookEditor.Save(outputLocation);
                    workbookFilePaths.Add(workbookFilePath);
                    Log.InfoFormat("Saved workbook to '{0}'!", workbookFilePath);
                }
            }

            return workbookFilePaths;
        }

        /// <summary>
        /// Handles tasks associated with a failed plugin execution.
        /// </summary>
        protected void HandlePluginExecutionFailure(IPluginResponse pluginResponse, string failureReason, Exception associatedException = null)
        {
            pluginResponse.SetExecutionOutcome(isSuccessful: false, failureReason: failureReason);

            // Log out error and stack, if possible.
            if (!String.IsNullOrWhiteSpace(failureReason))
            {
                Log.Error(failureReason);
            }
            if (associatedException != null)
            {
                Log.DebugFormat(associatedException.ToString());
            }
        }

        /// <summary>
        /// Prints the outcome of executing a plugin.
        /// </summary>
        /// <param name="response">The response from executing the plugin.</param>
        protected void LogExecutionOutcome(IPluginResponse response)
        {
            if (response == null)
            {
                Log.Error("Plugin response is invalid!");
            }
            else if (!response.SuccessfulExecution)
            {
                Log.ErrorFormat("{0} failed to execute successfully.", response.PluginName);
            }
            else
            {
                Log.InfoFormat("{0} execution completed! [{1}]", response.PluginName, response.PluginRunTime.Print());
            }
        }

        #endregion Protected Methods
    }
}