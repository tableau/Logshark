using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using LogShark.Shared.HostExtensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LogShark.HostExtensions
{
    public static class HostExtensionLoader
    {
        public const string AssemblyFileName = "LogShark.HostExtension.dll";

        // Keyed by extension assembly path; a single Resolving handler consults all of them
        private static readonly ConcurrentDictionary<string, AssemblyDependencyResolver> DependencyResolvers = new(StringComparer.OrdinalIgnoreCase);
        private static readonly Lazy<bool> ResolvingHandlerRegistration = new(() =>
        {
            AssemblyLoadContext.Default.Resolving += ResolveExtensionDependency;
            return true;
        });

        // Absent extension => null (public build). Present but unusable => HostExtensionLoadException, so an
        // internal build never silently runs without its extension.
        // Loaded into the Default ALC (unlike dynamic plugins) so the extension shares the
        // LogShark.Shared contract types with the host; an isolated ALC would make the cast fail.
        public static ILogSharkHostExtension Load(string directory, ILogger logger)
        {
            var assemblyPath = Path.GetFullPath(Path.Combine(directory, AssemblyFileName));
            if (!File.Exists(assemblyPath))
            {
                logger.LogDebug("No host extension found at {extensionPath}", assemblyPath);
                return null;
            }

            try
            {
                RegisterDependencyResolution(assemblyPath);

                var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(assemblyPath);
                var extensionTypes = assembly.GetTypes()
                    .Where(t => t.IsPublic && t.IsClass && !t.IsAbstract && typeof(ILogSharkHostExtension).IsAssignableFrom(t))
                    .ToList();
                if (extensionTypes.Count != 1)
                {
                    var found = extensionTypes.Count == 0 ? "none" : string.Join(", ", extensionTypes.Select(t => t.FullName));
                    throw new HostExtensionLoadException($"Failed to load host extension '{assemblyPath}': expected exactly one public implementation of {nameof(ILogSharkHostExtension)}, found {found}");
                }

                var extensionType = extensionTypes[0];
                var extension = (ILogSharkHostExtension) Activator.CreateInstance(extensionType);
                logger.LogInformation("Loaded host extension {extensionType} from {extensionPath}", extensionType.FullName, assemblyPath);
                return extension;
            }
            catch (HostExtensionLoadException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new HostExtensionLoadException($"Failed to load host extension '{assemblyPath}': {DescribeCause(ex)}", ex);
            }
        }

        public static IArtifactPublisher CreateArtifactPublisher(ILogSharkHostExtension extension, IConfiguration configuration, ILoggerFactory loggerFactory)
        {
            if (extension == null)
            {
                return new NullArtifactPublisher();
            }

            try
            {
                return extension.CreateArtifactPublisher(configuration, loggerFactory) ?? new NullArtifactPublisher();
            }
            catch (Exception ex)
            {
                throw new HostExtensionLoadException($"Host extension {extension.GetType().FullName} failed to create its artifact publisher: {DescribeCause(ex)}", ex);
            }
        }

        private static string DescribeCause(Exception ex)
        {
            switch (ex)
            {
                case TargetInvocationException { InnerException: not null } tie:
                    return DescribeCause(tie.InnerException);
                case ReflectionTypeLoadException rtle:
                    var loaderErrors = rtle.LoaderExceptions.Where(e => e != null).Select(e => $"{e.GetType().Name}: {e.Message}").Distinct();
                    return $"{ex.GetType().Name}: {string.Join("; ", loaderErrors)}";
                default:
                    return $"{ex.GetType().Name}: {ex.Message}";
            }
        }

        private static void RegisterDependencyResolution(string assemblyPath)
        {
            _ = ResolvingHandlerRegistration.Value;
            DependencyResolvers.GetOrAdd(assemblyPath, path =>
            {
                try
                {
                    return new AssemblyDependencyResolver(path);
                }
                catch (InvalidOperationException)
                {
                    // No usable deps.json next to the extension; fall back to probing its directory
                    return null;
                }
            });
        }

        private static Assembly ResolveExtensionDependency(AssemblyLoadContext context, AssemblyName name)
        {
            foreach (var (extensionPath, resolver) in DependencyResolvers)
            {
                var path = resolver?.ResolveAssemblyToPath(name);
                if (path == null)
                {
                    var candidate = Path.Combine(Path.GetDirectoryName(extensionPath)!, name.Name + ".dll");
                    path = File.Exists(candidate) ? candidate : null;
                }
                if (path != null)
                {
                    return context.LoadFromAssemblyPath(path);
                }
            }
            return null;
        }
    }
}
