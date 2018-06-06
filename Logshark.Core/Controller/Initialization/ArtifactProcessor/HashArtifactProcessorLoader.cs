using log4net;
using Logshark.ArtifactProcessorModel;
using Logshark.Common.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Logshark.Core.Controller.Initialization.ArtifactProcessor
{
    internal class HashArtifactProcessorLoader : ArtifactProcessorLoader
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public IArtifactProcessor LoadArtifactProcessor(string artifactProcessorType)
        {
            if (String.IsNullOrWhiteSpace(artifactProcessorType))
            {
                throw new ArgumentException("Must specify an artifact processor type to load!", "artifactProcessorType");
            }

            ICollection<IArtifactProcessor> compatibleProcessors = LoadCompatibleArtifactProcessors(artifactProcessorType);

            return SelectSingleCompatibleArtifactProcessor(compatibleProcessors);
        }

        protected ICollection<IArtifactProcessor> LoadCompatibleArtifactProcessors(string artifactProcessorType)
        {
            ICollection<IArtifactProcessor> availableProcessors = LoadAllArtifactProcessors();
            if (availableProcessors.Count > 0)
            {
                string loadedProcessorString = String.Join(", ", availableProcessors.Select(processor => processor.GetType().Name).AsEnumerable());
                Log.InfoFormat("Loaded {0} artifact {1}: {2}", availableProcessors.Count, "processor".Pluralize(availableProcessors.Count), loadedProcessorString);
            }

            var compatibleProcessors = new List<IArtifactProcessor>();
            foreach (IArtifactProcessor processor in availableProcessors.Where(processor => processor.GetType().Name.Equals(artifactProcessorType)))
            {
                Log.InfoFormat("Found compatible artifact processor: {0}", processor.GetType().Name);
                compatibleProcessors.Add(processor);
            }

            return compatibleProcessors;
        }
    }
}