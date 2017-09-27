using log4net;
using Logshark.ArtifactProcessorModel;
using Logshark.Common.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Logshark.Core.Controller.ArtifactProcessor
{
    internal static class ArtifactProcessorLoader
    {
        private static readonly string ArtifactProcessorDirectoryName = "ArtifactProcessors";
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Loads all artifact processors in the ArtifactProcessors directory.
        /// </summary>
        /// <returns>All artifact processors present in assemblies within the artifact processors directory.</returns>
        public static ISet<IArtifactProcessor> LoadAllArtifactProcessors()
        {
            ISet<IArtifactProcessor> artifactProcessors = new HashSet<IArtifactProcessor>();

            string artifactProcessorsDirectory = GetArtifactProcessorsDirectory();
            foreach (string artifactProcessorDll in Directory.GetFiles(artifactProcessorsDirectory, "*.dll"))
            {
                IList<Type> artifactProcessorTypes = LoadArtifactProcessorsFromAssembly(artifactProcessorDll).ToList();
                foreach (var artifactProcessorType in artifactProcessorTypes)
                {
                    var artifactProcessorInstance = (IArtifactProcessor)Activator.CreateInstance(artifactProcessorType);
                    artifactProcessors.Add(artifactProcessorInstance);
                }
            }

            return artifactProcessors;
        }

        /// <summary>
        /// Return the full path to the ArtifactProcessors directory.
        /// </summary>
        /// <returns>Full path to the ArtifactProcessors directory.</returns>
        private static string GetArtifactProcessorsDirectory()
        {
            return Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), ArtifactProcessorDirectoryName);
        }

        /// <summary>
        /// Loads all types that implement IArtifactProcessor from a given assembly.
        /// </summary>
        /// <param name="assemblyPath">The path to the assembly.</param>
        /// <returns>Collection of all concrete types present within the assembly that implement IArtifactProcessor.</returns>
        private static IEnumerable<Type> LoadArtifactProcessorsFromAssembly(string assemblyPath)
        {
            try
            {
                Assembly pluginAssembly = Assembly.LoadFile(assemblyPath);
                return pluginAssembly.GetTypes().Where(type => !type.IsAbstract && type.Implements(typeof(IArtifactProcessor)));
            }
            catch (Exception ex)
            {
                Log.ErrorFormat("Failed to load assembly '{0}': {1}", assemblyPath, ex.Message);
                return new List<Type>();
            }
        }
    }
}