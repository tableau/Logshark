using log4net;
using Logshark.ArtifactProcessorModel;
using Logshark.Common.Extensions;
using Logshark.Core.Exceptions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Logshark.Core.Controller.Initialization.ArtifactProcessor
{
    internal class ArtifactProcessorLoader
    {
        protected const string ArtifactProcessorDirectoryName = "ArtifactProcessors";

        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        #region Public Methods

        /// <summary>
        /// Loads all artifact processors in the ArtifactProcessors directory.
        /// </summary>
        /// <returns>All artifact processors present in assemblies within the artifact processors directory.</returns>
        public ISet<IArtifactProcessor> LoadAllArtifactProcessors()
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

        #endregion Public Methods

        #region Protected Methods

        /// <summary>
        /// Return the full path to the ArtifactProcessors directory.
        /// </summary>
        /// <returns>Full path to the ArtifactProcessors directory.</returns>
        protected static string GetArtifactProcessorsDirectory()
        {
            return Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), ArtifactProcessorDirectoryName);
        }

        /// <summary>
        /// Loads all types that implement IArtifactProcessor from a given assembly.
        /// </summary>
        /// <param name="assemblyPath">The path to the assembly.</param>
        /// <returns>Collection of all concrete types present within the assembly that implement IArtifactProcessor.</returns>
        protected static IEnumerable<Type> LoadArtifactProcessorsFromAssembly(string assemblyPath)
        {
            try
            {
                Assembly pluginAssembly = Assembly.LoadFrom(assemblyPath);
                return pluginAssembly.GetTypes().Where(type => !type.IsAbstract && type.Implements(typeof(IArtifactProcessor)));
            }
            catch (Exception ex)
            {
                Log.ErrorFormat("Failed to load assembly '{0}': {1}", assemblyPath, ex.Message);
                return new List<Type>();
            }
        }

        /// <summary>
        /// Returns exactly one artifact processor instance from a pool of compatible candidates.  If there are zero or multiple matches, an appropriate exception is thrown.
        /// This is a bit weird but we have to do this because currently the framework does not support either processing an artifact with multiple processors or allowing a user to select a particular artifact processor.
        /// </summary>
        /// <param name="compatibleProcessors">A collection of artifact processors that are compatible with the current logset payload.</param>
        /// <returns>A compatible artifact processor, if one (and only one) exists.</returns>
        protected static IArtifactProcessor SelectSingleCompatibleArtifactProcessor(ICollection<IArtifactProcessor> compatibleProcessors)
        {
            if (compatibleProcessors == null || compatibleProcessors.Count == 0)
            {
                throw new InvalidLogsetException("No compatible artifact processor found for payload! Is this a valid logset?");
            }
            else if (compatibleProcessors.Count > 1)
            {
                throw new ArtifactProcessorInitializationException(String.Format("Multiple artifact processors match payload: {0}", String.Join(", ", compatibleProcessors)));
            }

            return compatibleProcessors.First();
        }

        #endregion Protected Methods
    }
}