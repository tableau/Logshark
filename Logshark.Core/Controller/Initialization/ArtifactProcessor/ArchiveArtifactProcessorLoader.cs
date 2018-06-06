using log4net;
using Logshark.ArtifactProcessorModel;
using Logshark.Common.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Logshark.Core.Controller.Initialization.ArtifactProcessor
{
    internal class ArchiveArtifactProcessorLoader : ArtifactProcessorLoader
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Loads the appropriate compatible artifact processor for a given logset.
        /// </summary>
        /// <param name="rootLogDirectory">The absolute path of the root log data directory.</param>
        /// <returns>Artifact processor type that can be used to service the given logset.</returns>
        public IArtifactProcessor LoadArtifactProcessor(string rootLogDirectory)
        {
            ICollection<IArtifactProcessor> compatibleProcessors = LoadCompatibleArtifactProcessors(rootLogDirectory);

            return SelectSingleCompatibleArtifactProcessor(compatibleProcessors);
        }

        protected ICollection<IArtifactProcessor> LoadCompatibleArtifactProcessors(string rootLogDirectory)
        {
            ICollection<IArtifactProcessor> availableProcessors = LoadAllArtifactProcessors();
            if (availableProcessors.Count == 0)
            {
                Log.Warn("No artifact processors found!");
            }
            else
            {
                string loadedProcessorString = String.Join(", ", availableProcessors.Select(processor => processor.GetType().Name).AsEnumerable());
                Log.InfoFormat("Loaded {0} artifact {1}: {2}", availableProcessors.Count, "processor".Pluralize(availableProcessors.Count), loadedProcessorString);
            }

            var compatibleProcessors = new List<IArtifactProcessor>();
            foreach (IArtifactProcessor processor in availableProcessors.Where(processor => processor.CanProcess(rootLogDirectory)))
            {
                Log.Info("Found matching artifact processor: " + processor.GetType().Name);
                compatibleProcessors.Add(processor);
            }

            return compatibleProcessors;
        }
    }
}