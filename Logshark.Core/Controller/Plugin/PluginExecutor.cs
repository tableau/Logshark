using log4net;
using Logshark.Common.Extensions;
using Logshark.Core.Controller.Workbook;
using Logshark.Core.Exceptions;
using Logshark.PluginLib.Model;
using Logshark.PluginLib.Model.Impl;
using Logshark.PluginModel.Model;
using Logshark.RequestModel;
using MongoDB.Driver;
using ServiceStack.OrmLite;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Logshark.Core.Controller.Plugin
{
    /// <summary>
    ///  Handles execution of plugins.
    /// </summary>
    internal class PluginExecutor
    {
        protected readonly LogsharkRequest logsharkRequest;
        protected readonly IMongoDatabase mongoDatabase;
        protected readonly OrmLiteConnectionFactory outputDatabaseConnectionFactory;
        protected readonly WorkbookPublisher workbookPublisher;

        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public PluginExecutor(LogsharkRequest request)
        {
            logsharkRequest = request;
            mongoDatabase = request.Configuration.MongoConnectionInfo.GetDatabase(logsharkRequest.RunContext.MongoDatabaseName);
            outputDatabaseConnectionFactory = request.Configuration.PostgresConnectionInfo.GetConnectionFactory(logsharkRequest.PostgresDatabaseName);
            workbookPublisher = new WorkbookPublisher(logsharkRequest);
        }

        #region Public Methods

        /// <summary>
        /// Execute all plugins requested by the LogsharkRequest.
        /// </summary>
        public void ExecutePlugins()
        {
            string workbookOutputLocation = GetOutputLocation(logsharkRequest.RunId);
            if (!Directory.Exists(workbookOutputLocation))
            {
                Directory.CreateDirectory(workbookOutputLocation);
            }

            // Execute all plugins.
            var pluginTimer = logsharkRequest.RunContext.CreateTimer("Executed Plugins");
            ICollection<Type> pluginsToExecute = logsharkRequest.RunContext.PluginTypesToExecute;
            ExecuteStandardPlugins(pluginsToExecute);
            ExecutePostExecutionPlugins(pluginsToExecute);
            pluginTimer.Stop();

            int failures = logsharkRequest.RunContext.PluginResponses.Count(pluginResponse => !pluginResponse.SuccessfulExecution);
            Log.InfoFormat("Finished executing plugins! [{0} {1}]", failures, "failure".Pluralize(failures));
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
        /// Executes all standard plugins requested by the LogsharkRequest.
        /// </summary>
        protected void ExecuteStandardPlugins(IEnumerable<Type> pluginsToExecute)
        {
            foreach (Type pluginType in pluginsToExecute.Where(PluginLoader.IsStandardPlugin))
            {
                ExecutePlugin(pluginType);
            }
        }

        /// <summary>
        /// Executes all post-execution plugins requested by the LogsharkRequest.
        /// </summary>
        protected void ExecutePostExecutionPlugins(IEnumerable<Type> pluginsToExecute)
        {
            foreach (Type pluginType in pluginsToExecute.Where(PluginLoader.IsPostExecutionPlugin))
            {
                ExecutePlugin(pluginType);
            }
        }

        /// <summary>
        /// Executes a single plugin.
        /// </summary>
        /// <param name="pluginType">The type of the plugin to execute.</param>
        /// <returns>Response containing state about the success/failure of the plugin's execution.</returns>
        protected IPluginResponse ExecutePlugin(Type pluginType)
        {
            string pluginName = pluginType.Name;

            // Setup plugin for execution.
            IPluginRequest request = CreatePluginRequest(pluginType);
            var pluginTimer = logsharkRequest.RunContext.CreateTimer("Executed Plugin", pluginName);

            // Execute plugin.
            IPluginResponse pluginResponse = new PluginResponse(pluginName);
            try
            {
                IPlugin plugin = InitializePlugin(pluginType);

                Log.InfoFormat("Execution of {0} plugin started at {1}..", pluginName, DateTime.Now.ToString("h:mm tt", CultureInfo.InvariantCulture));
                pluginResponse = plugin.Execute(request);

                // Flush any workbooks, if this was a workbook creation plugin.
                if (plugin is IWorkbookCreationPlugin)
                {
                    WriteWorkbooksToDisk(plugin as IWorkbookCreationPlugin, pluginResponse);
                }

                // Publish any associated workbooks, if requested.
                if (logsharkRequest.PublishWorkbooks)
                {
                    workbookPublisher.PublishWorkbooks(pluginResponse);
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

                logsharkRequest.RunContext.RegisterPluginResponse(pluginResponse);
                PrintExecutionOutcome(pluginResponse);
            }

            return pluginResponse;
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
        protected void PrintExecutionOutcome(IPluginResponse response)
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

        /// <summary>
        /// Handles initialization of an IPlugin object for a given plugin type.
        /// </summary>
        /// <param name="pluginType">The type of the plugin.</param>
        /// <returns>Initialized IPlugin object for the given plugin type.</returns>
        protected IPlugin InitializePlugin(Type pluginType)
        {
            Log.InfoFormat("Initializing {0} plugin..", pluginType.Name);

            try
            {
                // Create plugin object.
                var plugin = (IPlugin)Activator.CreateInstance(pluginType);

                // Set database connections.
                plugin.MongoDatabase = mongoDatabase;
                plugin.OutputDatabaseConnectionFactory = outputDatabaseConnectionFactory;

                if (PluginLoader.IsPostExecutionPlugin(pluginType))
                {
                    IPostExecutionPlugin postExecutionPlugin = (IPostExecutionPlugin)plugin;
                    postExecutionPlugin.PluginResponses = logsharkRequest.RunContext.PluginResponses;
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
        /// Instantiates a plugin request for the given plugin type.
        /// </summary>
        /// <param name="pluginType">The type of the plugin.</param>
        /// <returns>PluginRequest with all appropriate state initialized.</returns>
        protected IPluginRequest CreatePluginRequest(Type pluginType)
        {
            // In this method we will append argument specific to each plugin type.
            Guid logsetHash = Guid.Parse(logsharkRequest.RunContext.LogsetHash);
            string outputDirectory = GetOutputLocation(logsharkRequest.RunId);
            var pluginRequest = new PluginRequest(mongoDatabase, logsetHash, outputDirectory);

            // Append all global and plugin specific arguments to the plugin argument map.
            foreach (string argumentKey in logsharkRequest.PluginCustomArguments.Keys)
            {
                if (argumentKey.StartsWith(pluginType.Name + ".", StringComparison.InvariantCultureIgnoreCase)
                    || argumentKey.StartsWith("Global.", StringComparison.InvariantCultureIgnoreCase))
                {
                    pluginRequest.SetRequestArgument(argumentKey, logsharkRequest.PluginCustomArguments[argumentKey]);
                }
            }

            return pluginRequest;
        }

        /// <summary>
        /// Writes all workbooks associated with a workbook creation plugin to the output directory.
        /// </summary>
        /// <param name="plugin">The workbook creation plugin that ran.</param>
        /// <param name="response">The response object, containing information about the results of executing the plugin.</param>
        protected void WriteWorkbooksToDisk(IWorkbookCreationPlugin plugin, IPluginResponse response)
        {
            // Replace connection information in workbook and flush to disk.
            if (response.SuccessfulExecution)
            {
                ICollection<string> workbookNames = plugin.WorkbookNames;

                if (response.GeneratedNoData)
                {
                    Log.InfoFormat("Skipped saving {0} {1} because the plugin did not generate any backing data.", plugin.GetType().Name, "workbook".Pluralize(workbookNames.Count));
                    return;
                }

                foreach (var workbookName in workbookNames)
                {
                    WorkbookEditor workbookEditor = new WorkbookEditor(workbookName, plugin.GetWorkbookXml(workbookName));
                    workbookEditor.ReplacePostgresConnections(logsharkRequest.Configuration.PostgresConnectionInfo, logsharkRequest.PostgresDatabaseName);
                    workbookEditor.RemoveThumbnails();

                    string workbookFilePath = workbookEditor.Save(GetOutputLocation(logsharkRequest.RunId));
                    response.WorkbooksOutput.Add(workbookFilePath);
                    Log.InfoFormat("Saved workbook to '{0}'!", workbookFilePath);
                }
            }
        }

        #endregion Protected Methods
    }
}