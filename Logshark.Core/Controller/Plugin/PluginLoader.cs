using log4net;
using Logshark.ArtifactProcessorModel;
using Logshark.Common.Extensions;
using Logshark.Core.Controller.ArtifactProcessor;
using Logshark.PluginModel.Model;
using Logshark.RequestModel;
using Logshark.RequestModel.Config;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Logshark.Core.Controller.Plugin
{
    /// <summary>
    /// Handles loading of plugins from a user's Plugins directory.
    /// </summary>
    internal class PluginLoader
    {
        protected enum PluginLoadingOption
        {
            Default,
            All,
            None,
            Named
        };

        protected LogsharkRequest request;
        protected ISet<string> requestedPluginsToExecute;
        protected PluginLoadingOption pluginLoadingOption;

        protected static readonly string PluginDirectoryName = "Plugins";
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public PluginLoader(LogsharkRequest request)
        {
            this.request = request;
            requestedPluginsToExecute = request.PluginsToExecute;
            pluginLoadingOption = GetPluginLoadingOption();
        }

        #region Public Methods

        /// <summary>
        /// Loads all requested plugins for a given artifact processor.
        /// </summary>
        /// <returns>Set of loaded plugins.</returns>
        public ISet<Type> LoadPlugins(IArtifactProcessor artifactProcessor)
        {
            if (pluginLoadingOption == PluginLoadingOption.None)
            {
                Log.Info("No plugins loaded due to user request.");
                return new HashSet<Type>();
            }

            // Load everything for the given artifact type.
            ISet<Type> plugins = LoadSupportedPlugins(artifactProcessor);

            // Filter down, if user requested it.
            if (pluginLoadingOption == PluginLoadingOption.Default)
            {
                plugins = FilterPluginsByConfiguredDefaults(artifactProcessor, plugins);
            }
            else if (pluginLoadingOption == PluginLoadingOption.Named)
            {
                plugins = FilterPluginsByName(plugins, request.PluginsToExecute);
            }

            DisplayPluginLoadingMessage(plugins);
            return plugins;
        }

        /// <summary>
        /// Print a status message about all available plugins.
        /// </summary>
        public static void PrintAvailablePlugins()
        {
            ISet<IArtifactProcessor> allArtifactProcessors = ArtifactProcessorLoader.LoadAllArtifactProcessors();
            foreach (IArtifactProcessor artifactProcessor in allArtifactProcessors)
            {
                ISet<Type> allPlugins = LoadSupportedPlugins(artifactProcessor);
                string pluginInfo = GetPluginInfoMessage(artifactProcessor.ArtifactType, allPlugins);
                Log.Info(pluginInfo);
            }
        }

        /// <summary>
        /// Retrieves the collection dependencies for a given set of plugins.
        /// </summary>
        /// <param name="pluginTypes">The plugins to retrieve collection dependencies for.</param>
        /// <returns>All collection dependencies for the given plugins, filtered by product type if known.</returns>
        public static ISet<string> GetCollectionDependencies(IEnumerable<Type> pluginTypes)
        {
            ISet<string> collectionDependencies = new SortedSet<string>();

            foreach (var pluginType in pluginTypes)
            {
                IPlugin plugin = Activator.CreateInstance(pluginType) as IPlugin;
                if (plugin == null)
                {
                    continue;
                }

                foreach (var collectionDependency in plugin.CollectionDependencies)
                {
                    {
                        collectionDependencies.Add(collectionDependency.ToLowerInvariant());
                    }
                }
            }

            return collectionDependencies;
        }

        /// <summary>
        /// Indicates whether a given plugin type is a standard plugin (not a post-execution plugin).
        /// </summary>
        /// <param name="pluginType">The plugin type to check.</param>
        /// <returns>True if the plugin type is a standard plugin.</returns>
        public static bool IsStandardPlugin(Type pluginType)
        {
            return !IsPostExecutionPlugin(pluginType);
        }

        /// <summary>
        /// Indicates whether a given plugin type is a post-execution plugin.
        /// </summary>
        /// <param name="pluginType">The plugin type to check.</param>
        /// <returns>True if the plugin type is a post-execution plugin.</returns>
        public static bool IsPostExecutionPlugin(Type pluginType)
        {
            return pluginType.Implements(typeof(IPostExecutionPlugin));
        }

        #endregion Public Methods

        #region Protected Methods

        /// <summary>
        /// Loads all plugins in the Plugins directory which are supported by a given artifact processor.
        /// </summary>
        /// <returns>All plugins present in assemblies within the plugins directory supported by the given artifact processor.</returns>
        protected static ISet<Type> LoadSupportedPlugins(IArtifactProcessor artifactProcessor)
        {
            ISet<Type> plugins = new HashSet<Type>();

            Log.DebugFormat("Loading all available Logshark plugins for artifact processor {0}..", artifactProcessor.ArtifactType);
            string pluginsDirectory = GetPluginsDirectory();

            foreach (string pluginDll in Directory.GetFiles(pluginsDirectory, "*.dll"))
            {
                IList<Type> pluginTypes = LoadPluginsFromAssembly(pluginDll).ToList();
                foreach (var pluginType in pluginTypes)
                {
                    if (IsImplementationOfSupportedPluginInterface(pluginType, artifactProcessor.SupportedPluginInterfaces))
                    {
                        plugins.Add(pluginType);
                    }
                }
            }

            return plugins;
        }

        /// <summary>
        /// Determines whether a given plugin type implements an interface from a list of supported plugin interfaces.
        /// </summary>
        protected static bool IsImplementationOfSupportedPluginInterface(Type pluginType, ISet<Type> supportedPluginInterfaces)
        {
            foreach (Type supportedPluginInterface in supportedPluginInterfaces)
            {
                if (pluginType.Implements(supportedPluginInterface))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Filters a collection of plugins by the defaults specified for that Artifact Processor in the LogsharkConfiguration.
        /// </summary>
        /// <param name="artifactProcessor">The artifact processor to load the defaults for.</param>
        /// <param name="plugins">The set of plugins to filter.</param>
        /// <returns>Set of plugins filtered down to only those that are specified as defaults for the given artifact processor.</returns>
        protected ISet<Type> FilterPluginsByConfiguredDefaults(IArtifactProcessor artifactProcessor, ISet<Type> plugins)
        {
            try
            {
                LogsharkArtifactProcessorConfiguration artifactProcessorConfiguration = request.Configuration.ArtifactProcessorOptions.LoadConfiguration(artifactProcessor.GetType());
                return FilterPluginsByName(plugins, artifactProcessorConfiguration.DefaultPlugins);
            }
            catch (KeyNotFoundException)
            {
                // If the user has not specified a default configuration for the given artifact processor, just default to running everything.
                return plugins;
            }
        }

        /// <summary>
        /// Filters a collection of plugins by name.
        /// </summary>
        /// <param name="plugins">The plugins to be filtered.</param>
        /// <param name="pluginNamesToKeep">A set of plugin assembly names to keep.</param>
        /// <returns>Set of plugins filtered by name(s).</returns>
        protected ISet<Type> FilterPluginsByName(ISet<Type> plugins, ICollection<string> pluginNamesToKeep)
        {
            ISet<Type> filteredPlugins = new HashSet<Type>();

            foreach (string pluginNameToKeep in pluginNamesToKeep)
            {
                foreach (var plugin in plugins)
                {
                    string pluginName = plugin.FullName.Replace(plugin.Namespace, "").TrimStart('.');
                    if (pluginNameToKeep.Equals(pluginName, StringComparison.InvariantCultureIgnoreCase))
                    {
                        filteredPlugins.Add(plugin);
                    }
                }
            }

            return filteredPlugins;
        }

        /// <summary>
        /// Display a message about plugin loading status to the user.
        /// </summary>
        /// <param name="plugins">The plugins being loaded.</param>
        protected static void DisplayPluginLoadingMessage(ICollection<Type> plugins)
        {
            // Display results to user.
            if (plugins.Count > 0)
            {
                var pluginNames = plugins.Select(plugin => plugin.Name).ToList();

                var standardPluginNames = plugins.Where(plugin => !IsPostExecutionPlugin(plugin)).Select(plugin => plugin.Name).ToList();
                if (standardPluginNames.Count > 0)
                {
                    Log.InfoFormat("Loaded requested Logshark {0}: {1}", "plugin".Pluralize(pluginNames.Count), String.Join(", ", standardPluginNames));
                }

                var postExecutionPluginNames = plugins.Where(IsPostExecutionPlugin).Select(plugin => plugin.Name).ToList();
                if (postExecutionPluginNames.Count > 0)
                {
                    Log.InfoFormat("Loaded requested post-execution Logshark {0}: {1}", "plugin".Pluralize(pluginNames.Count), String.Join(", ", postExecutionPluginNames));
                }
            }
            else
            {
                Log.Warn("No Logshark plugins loaded.");
            }
        }

        /// <summary>
        /// Determines the plugin loading option for this request.
        /// </summary>
        /// <returns>Plugin loading option for this request.</returns>
        protected PluginLoadingOption GetPluginLoadingOption()
        {
            // An empty set is considered the same as a request for "default".
            if (requestedPluginsToExecute.Count == 0)
            {
                return PluginLoadingOption.Default;
            }

            switch (requestedPluginsToExecute.First().ToLowerInvariant())
            {
                case "default":
                    return PluginLoadingOption.Default;

                case "all":
                    return PluginLoadingOption.All;

                case "none":
                    return PluginLoadingOption.None;

                default:
                    return PluginLoadingOption.Named;
            }
        }

        /// <summary>
        /// Loads all types that implement IPlugin or IPostExecutionPlugin from a given assembly.
        /// </summary>
        /// <param name="assemblyPath">The path to the assembly.</param>
        /// <returns>Collection of all types present within the assembly that implement IPlugin or IPostExecutionPlugin.</returns>
        protected static IEnumerable<Type> LoadPluginsFromAssembly(string assemblyPath)
        {
            try
            {
                Assembly pluginAssembly = Assembly.LoadFile(assemblyPath);
                return pluginAssembly.GetTypes().Where(type => !type.IsAbstract && (type.Implements(typeof(IPlugin)) || type.Implements(typeof(IPostExecutionPlugin))));
            }
            catch (Exception ex)
            {
                Log.ErrorFormat("Failed to load assembly '{0}': {1}", assemblyPath, ex.Message);
                return new List<Type>();
            }
        }

        /// <summary>
        /// Builds an informational message about a given set of plugins.
        /// </summary>
        protected static string GetPluginInfoMessage(string artifactTypeName, IEnumerable<Type> plugins)
        {
            StringBuilder pluginInfo = new StringBuilder();

            pluginInfo.AppendFormat("Available {0} Plugins:", artifactTypeName);
            pluginInfo.AppendLine();

            foreach (Type plugin in plugins.OrderBy(plugin => plugin.Name))
            {
                string pluginTiming = "";
                if (IsPostExecutionPlugin(plugin))
                {
                    pluginTiming = "Post-execution";
                }
                else if (IsStandardPlugin(plugin))
                {
                    pluginTiming = "Standard";
                }
                pluginInfo.AppendFormat(" - {0} ({1})", plugin.Name, pluginTiming);
                pluginInfo.AppendLine();
            }

            return pluginInfo.ToString();
        }

        /// <summary>
        /// Return the full path to the Plugins directory.
        /// </summary>
        /// <returns>Full path to the Plugins directory.</returns>
        protected static string GetPluginsDirectory()
        {
            return Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), PluginDirectoryName);
        }

        #endregion Protected Methods
    }
}