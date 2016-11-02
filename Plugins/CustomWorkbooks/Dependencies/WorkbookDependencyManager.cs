using log4net;
using Logshark.PluginLib.Logging;
using Logshark.PluginLib.Model;
using Logshark.Plugins.CustomWorkbooks.Config;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Logshark.Plugins.CustomWorkbooks.Dependencies
{
    public class WorkbookDependencyManager
    {
        protected readonly IEnumerable<IPluginResponse> pluginResponses;
        protected readonly IEnumerable<WorkbookDependencyMapping> workbookDependencyMappings;

        protected readonly string workbookDirectory;
        protected readonly string workbookConfigFilename;

        private static readonly ILog Log = PluginLogFactory.GetLogger(Assembly.GetExecutingAssembly(),
                                                              MethodBase.GetCurrentMethod());

        public WorkbookDependencyManager(IEnumerable<IPluginResponse> pluginResponses, string workbookDirectory, string workbookConfigFilename)
        {
            this.pluginResponses = pluginResponses;
            this.workbookDirectory = workbookDirectory;
            this.workbookConfigFilename = workbookConfigFilename;
            workbookDependencyMappings = LoadWorkbookDependencyMappings();
        }

        /// <summary>
        /// Gets a list of all embedded workbooks whose plugin dependencies have been satisfied by this Logshark run.
        /// </summary>
        public ICollection<string> GetValidWorkbooks()
        {
            Log.InfoFormat("Generating custom workbooks which met required dependencies..");

            ISet<string> validWorkbooks = new HashSet<string>();

            foreach (var workbookDependencyMapping in workbookDependencyMappings)
            {
                if (!WorkbookExists(workbookDependencyMapping.WorkbookName))
                {
                    Log.DebugFormat("Skipping generation of workbook '{0}' because the file cannot be found.", workbookDependencyMapping.WorkbookName);
                }
                else if (DependentPluginsExecutedSuccessfully(workbookDependencyMapping.PluginDependencies))
                {
                    if (DependentPluginsProcessedDataSuccessfully(workbookDependencyMapping.PluginDependencies))
                    {
                        validWorkbooks.Add(workbookDependencyMapping.WorkbookName);
                    }
                    else
                    {
                        string pluginDependencies = String.Join(", ", workbookDependencyMapping.PluginDependencies);
                        Log.DebugFormat("Skipping generation of workbook '{0}' because the following plugins did not generate any data: {1}", workbookDependencyMapping.WorkbookName, pluginDependencies);
                    }
                }
                else
                {
                    string missingPluginDependencies = String.Join(", ", GetMissingPluginDependencies(workbookDependencyMapping));
                    Log.DebugFormat("Skipping generation of workbook '{0}' because the following plugins did not successfully execute: {1}", workbookDependencyMapping.WorkbookName, missingPluginDependencies);
                }
            }

            return validWorkbooks;
        }

        /// <summary>
        /// Retrieves a collection of workbook depedency mappings from the specified config file.
        /// </summary>
        protected IEnumerable<WorkbookDependencyMapping> LoadWorkbookDependencyMappings()
        {
            try
            {
                string configFilePath = Path.Combine(workbookDirectory, workbookConfigFilename);
                Log.InfoFormat("Loading custom workbook dependency mappings from '{0}'..", configFilePath);
                return ConfigReader.LoadWorkbookDependencyMappings(configFilePath);
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("Failed to read config file: {0}", ex.Message), ex);
            }
        }

        /// <summary>
        /// Indicates whether all of a given set of plugins executed successfully.
        /// </summary>
        /// <param name="pluginNames">The plugins to check for successful execution.</param>
        /// <returns>True if all specified plugins executed successfully.</returns>
        protected bool DependentPluginsExecutedSuccessfully(IEnumerable<string> pluginNames)
        {
            return pluginNames.All(DependentPluginExecutedSuccessfully);
        }

        /// <summary>
        /// Indicates whether a given plugin executed successfully.
        /// </summary>
        /// <param name="pluginName">The plugin to check for successful execution.</param>
        /// <returns>True if the specified plugin executed successfully.</returns>
        protected bool DependentPluginExecutedSuccessfully(string pluginName)
        {
            return pluginResponses.Any(response => response.PluginName.Equals(pluginName, StringComparison.InvariantCultureIgnoreCase) && response.SuccessfulExecution);
        }

        /// <summary>
        /// Indicates whether all of a given set of plugins successfully processed any data.
        /// </summary>
        /// <param name="pluginNames">The plugins to check for successful data processing.</param>
        /// <returns>True if all specified plugins successfully processed any data.</returns>
        protected bool DependentPluginsProcessedDataSuccessfully(IEnumerable<string> pluginNames)
        {
            return pluginNames.All(DependentPluginProcessedDataSuccessfully);
        }

        /// <summary>
        /// Indicates whether a given plugin successfully processed any data.
        /// </summary>
        /// <param name="pluginName">The plugin to check for successful data processing..</param>
        /// <returns>True if the specified plugin successfully processed any data.</returns>
        protected bool DependentPluginProcessedDataSuccessfully(string pluginName)
        {
            return pluginResponses.Any(response => response.PluginName.Equals(pluginName, StringComparison.InvariantCultureIgnoreCase) && !response.GeneratedNoData);
        }

        /// <summary>
        /// Retrieves a list of all missing plugin dependencies that a workbook requires.
        /// </summary>
        protected IEnumerable<string> GetMissingPluginDependencies(WorkbookDependencyMapping workbookDependencyMapping)
        {
            return workbookDependencyMapping.PluginDependencies.Where(pluginName => !DependentPluginExecutedSuccessfully(pluginName));
        }

        /// <summary>
        /// Indicates whether a given workbook name exists in the expected local location.
        /// </summary>
        protected bool WorkbookExists(string workbookName)
        {
            return File.Exists(Path.Combine(workbookDirectory, workbookName));
        }
    }
}