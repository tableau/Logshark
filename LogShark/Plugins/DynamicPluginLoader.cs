using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.Extensions.Logging;

namespace LogShark.Plugins
{
    internal class PluginAssemblyLoadContext : AssemblyLoadContext
    {
        private readonly AssemblyDependencyResolver _resolver;

        // Non-collectible: plugins load once for the CLI run. Collectible ALCs add GC overhead that isn't needed here.
        public PluginAssemblyLoadContext(string pluginPath) : base(isCollectible: false)
        {
            _resolver = new AssemblyDependencyResolver(pluginPath);
        }

        protected override Assembly Load(AssemblyName assemblyName)
        {
            // Let the host resolve shared assemblies (LogShark.Shared, Microsoft.Extensions.*, etc.)
            var hostAssembly = AssemblyLoadContext.Default.Assemblies
                .FirstOrDefault(a => a.GetName().Name == assemblyName.Name);
            if (hostAssembly != null)
                return null; // returning null defers to Default context

            var assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
            if (assemblyPath != null)
                return LoadFromAssemblyPath(assemblyPath);

            return null;
        }

        protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
        {
            var libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
            if (libraryPath != null)
                return LoadUnmanagedDllFromPath(libraryPath);

            return IntPtr.Zero;
        }
    }

    internal class DynamicPluginLoader
    {
        private readonly ILogger _logger;

        public DynamicPluginLoader(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<DynamicPluginLoader>();
        }

        public Dictionary<string, (string PluginName, Type PluginType)> DiscoverPlugins(string directoryPath)
        {
            var discovered = new Dictionary<string, (string, Type)>();

            if (string.IsNullOrWhiteSpace(directoryPath) || !Directory.Exists(directoryPath))
            {
                if (!string.IsNullOrWhiteSpace(directoryPath))
                    _logger.LogWarning("Dynamic plugins directory does not exist: {directoryPath}", directoryPath);
                return discovered;
            }

            var dllFiles = Directory.GetFiles(directoryPath, "*.dll", SearchOption.AllDirectories);
            _logger.LogInformation("Scanning {count} DLLs in dynamic plugins directory: {directoryPath}", dllFiles.Length, directoryPath);

            foreach (var dllPath in dllFiles)
            {
                try
                {
                    var loadContext = new PluginAssemblyLoadContext(dllPath);
                    var assembly = loadContext.LoadFromAssemblyPath(Path.GetFullPath(dllPath));

                    Type[] types;
                    try
                    {
                        types = assembly.GetTypes();
                    }
                    catch (ReflectionTypeLoadException ex)
                    {
                        _logger.LogWarning(ex, "Some types could not be loaded from {dllPath}, salvaging valid types", dllPath);
                        types = ex.Types.Where(t => t != null).ToArray();
                    }

                    foreach (var type in types)
                    {
                        if (!typeof(IPlugin).IsAssignableFrom(type) || type.IsInterface || type.IsAbstract)
                            continue;

                        var pluginName = type.Name.EndsWith("Plugin")
                            ? type.Name.Substring(0, type.Name.LastIndexOf("Plugin", StringComparison.Ordinal))
                            : type.Name;

                        var key = pluginName.ToLower();
                        if (discovered.ContainsKey(key))
                        {
                            _logger.LogWarning("Duplicate dynamic plugin name '{pluginName}' in {dllPath}, skipping", pluginName, dllPath);
                            continue;
                        }

                        discovered[key] = (pluginName, type);
                        _logger.LogInformation("Discovered dynamic plugin: {pluginName} from {dllPath}", pluginName, Path.GetFileName(dllPath));
                    }
                }
                catch (BadImageFormatException)
                {
                    _logger.LogDebug("Skipping non-.NET or incompatible assembly: {dllPath}", dllPath);
                }
                catch (FileLoadException ex)
                {
                    _logger.LogWarning(ex, "Failed to load assembly: {dllPath}", dllPath);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Unexpected error loading assembly: {dllPath}", dllPath);
                }
            }

            return discovered;
        }
    }
}
