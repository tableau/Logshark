using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace LogShark.Plugins
{
    internal class PluginFactory
    {
        private readonly LogSharkConfiguration _config;
        private readonly Lazy<Dictionary<string, (string PluginName, Type PluginType)>> _knownPlugins;

        public PluginFactory(LogSharkConfiguration config)
        {
            _config = config;
            _knownPlugins = new Lazy<Dictionary<string, (string, Type)>>(DiscoverPlugins);
        }

        private Dictionary<string, (string, Type)> DiscoverPlugins()
        {
            var knownPlugins = new Dictionary<string, (string, Type)>();
            var assembly = _config.UsePluginsFromLogSharkAssembly
                ? Assembly.GetExecutingAssembly()
                : Assembly.GetEntryAssembly();
            foreach (var typeInfo in assembly?.DefinedTypes ?? Enumerable.Empty<TypeInfo>())
            {
                if (!typeInfo.ImplementedInterfaces.Contains(typeof(IPlugin)))
                {
                    continue;
                }

                var pluginName = typeInfo.Name.EndsWith("Plugin")
                    ? typeInfo.Name.Substring(0, typeInfo.Name.LastIndexOf("Plugin", StringComparison.Ordinal))
                    : typeInfo.Name;

                knownPlugins[pluginName.ToLower()] = (pluginName, typeInfo.AsType());
            }
            return knownPlugins;
        }

        public IPlugin CreatePlugin(string pluginName)
        {
            var (_, pluginType) = _knownPlugins.Value[pluginName.ToLower()];
            var plugin = Activator.CreateInstance(pluginType) as IPlugin;

            return plugin;
        }

        public IEnumerable<string> GetKnownPluginNames()
        {
            var knownPluginNames = _knownPlugins.Value.Values.Select(x => x.PluginName);
            return knownPluginNames;
        }
    }
}
