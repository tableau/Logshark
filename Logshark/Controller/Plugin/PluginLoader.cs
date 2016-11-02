using log4net;
using Logshark.Extensions;
using Logshark.PluginLib.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Logshark.Controller.Plugin
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
        /// Loads all requested plugins.
        /// </summary>
        /// <returns>Set of loaded plugins.</returns>
        public ISet<Type> LoadPlugins()
        {
            if (pluginLoadingOption == PluginLoadingOption.None)
            {
                Log.Info("No plugins loaded due to user request.");
                return new HashSet<Type>();
            }

            // Load everything for product type.
            ISet<Type> plugins = LoadAllPlugins();

            // Filter down, if user requested it.
            if (pluginLoadingOption == PluginLoadingOption.Default)
            {
                plugins = FilterPluginsByName(plugins, request.Configuration.DefaultPlugins);
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
            Log.Info("Available Logshark plugins:");
            ISet<Type> allPlugins = LoadAllPlugins();
            string pluginInfo = GetPluginInfoMessage(allPlugins);
            Log.Info(pluginInfo);
        }

        /// <summary>
        /// Retrieves the collection dependencies for a given set of plugins.
        /// </summary>
        /// <param name="pluginTypes">The plugins to retrieve collection dependencies for.</param>
        /// <param name="logsetType">The product type.</param>
        /// <returns>All collection dependencies for the given plugins, filtered by product type if known.</returns>
        public static ISet<string> GetCollectionDependencies(IEnumerable<Type> pluginTypes, LogsetType logsetType)
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
                    if (logsetType == LogsetType.Unknown)
                    {
                        collectionDependencies.Add(collectionDependency.ToLowerInvariant());
                    }

                    bool isDesktopLogset = logsetType == LogsetType.Desktop;
                    bool isServerLogset = logsetType == LogsetType.Server;

                    if ((isDesktopLogset && IsDesktopPlugin(pluginType)) ||
                        (isServerLogset && IsServerPlugin(pluginType)))
                    {
                        collectionDependencies.Add(collectionDependency.ToLowerInvariant());
                    }
                }
            }

            return collectionDependencies;
        }

        /// <summary>
        /// Indicates whether a given plugin type is a desktop plugin.
        /// </summary>
        /// <param name="pluginType">The plugin type to check.</param>
        /// <returns>True if the plugin type is a desktop plugin.</returns>
        public static bool IsDesktopPlugin(Type pluginType)
        {
            return pluginType.Implements(typeof(IDesktopPlugin));
        }

        /// <summary>
        /// Indicates whether a given plugin type is a server plugin.
        /// </summary>
        /// <param name="pluginType">The plugin type to check.</param>
        /// <returns>True if the plugin type is a server plugin.</returns>
        public static bool IsServerPlugin(Type pluginType)
        {
            return pluginType.Implements(typeof(IServerPlugin));
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

        /// <summary>
        /// Indicates whether a given plugin type matches the product type of the logset.
        /// </summary>
        /// <param name="pluginType">The plugin type to check.</param>
        /// <param name="logsetType">The product type of the logset.</param>
        /// <returns>True if plugin type matches the logset's product type.</returns>
        public static bool IsPluginMatchingProductType(Type pluginType, LogsetType logsetType)
        {
            bool isDesktopLogset = logsetType == LogsetType.Desktop;
            if (isDesktopLogset && IsDesktopPlugin(pluginType))
            {
                return true;
            }

            bool isServerLogset = logsetType == LogsetType.Server;
            if (isServerLogset && IsServerPlugin(pluginType))
            {
                return true;
            }

            return false;
        }

        #endregion Public Methods

        #region Protected Methods

        /// <summary>
        /// Loads all plugins in the Plugins directory.
        /// </summary>
        /// <returns>All plugins present in assemblies within the plugins directory.</returns>
        protected static ISet<Type> LoadAllPlugins()
        {
            ISet<Type> plugins = new HashSet<Type>();

            Log.Debug("Loading requested Logshark plugins..");
            string pluginsDirectory = GetPluginsDirectory();

            foreach (string pluginDll in Directory.GetFiles(pluginsDirectory, "*.dll"))
            {
                IList<Type> pluginTypes = LoadPluginsFromAssembly(pluginDll).ToList();
                foreach (var pluginType in pluginTypes)
                {
                    plugins.Add(pluginType);
                }
            }

            return plugins;
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

                var desktopPluginNames = plugins.Where(plugin => IsDesktopPlugin(plugin) && !IsPostExecutionPlugin(plugin)).Select(plugin => plugin.Name).ToList();
                if (desktopPluginNames.Count > 0)
                {
                    Log.InfoFormat("Loaded requested Tableau Desktop Logshark {0}: {1}", "plugin".Pluralize(pluginNames.Count), String.Join(", ", desktopPluginNames));
                }

                var serverPluginNames = plugins.Where(plugin => IsServerPlugin(plugin) && !IsPostExecutionPlugin(plugin)).Select(plugin => plugin.Name).ToList();
                if (serverPluginNames.Count > 0)
                {
                    Log.InfoFormat("Loaded requested Tableau Server Logshark {0}: {1}", "plugin".Pluralize(pluginNames.Count), String.Join(", ", serverPluginNames));
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
        /// <param name="plugins">The list of plugins to build the message for.</param>
        /// <returns>String containing status about the given plugins.</returns>
        protected static string GetPluginInfoMessage(IEnumerable<Type> plugins)
        {
            StringBuilder pluginInfo = new StringBuilder();
            foreach (Type plugin in plugins.OrderBy(plugin => plugin.Name))
            {
                string productType = String.Join(", ", GetProductTypesForPlugin(plugin));
                IEnumerable<string> collectionDependencies = GetCollectionDependencies(new List<Type>() { plugin }, LogsetType.Unknown);
                string executionTiming = "";
                if (IsPostExecutionPlugin(plugin))
                {
                    executionTiming = "Post-execution";
                }
                else if (IsStandardPlugin(plugin))
                {
                    executionTiming = "Standard";
                }
                pluginInfo.AppendLine("----------------------------------------------------------------------");
                pluginInfo.AppendFormat("|    {0, -64}|\n", plugin.Name);
                pluginInfo.AppendLine("----------------------------------------------------------------------");
                pluginInfo.AppendFormat("|        Type: {0, -54}|\n", productType);
                pluginInfo.AppendFormat("|        Timing: {0, -52}|\n", executionTiming);
                pluginInfo.AppendLine("|        Collection Dependencies:                                    |");
                foreach (var dependency in collectionDependencies)
                {
                    pluginInfo.AppendFormat("|            {0, -56}|\n", dependency);
                }

                pluginInfo.AppendLine("----------------------------------------------------------------------\n\n");
            }

            return pluginInfo.ToString();
        }

        /// <summary>
        /// Retrieves the product types for a given plugin.
        /// </summary>
        /// <param name="plugin">The plugin to check the types of.</param>
        /// <returns>The product types for the given plugin.</returns>
        protected static IEnumerable<LogsetType> GetProductTypesForPlugin(Type plugin)
        {
            var pluginProductTypes = new List<LogsetType>();

            if (IsDesktopPlugin(plugin))
            {
                pluginProductTypes.Add(LogsetType.Desktop);
            }
            if (IsServerPlugin(plugin))
            {
                pluginProductTypes.Add(LogsetType.Server);
            }

            return pluginProductTypes;
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