using Logshark.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Logshark.RequestModel.Config
{
    /// <summary>
    /// Encapsulates configuration options for multiple artifact processors.
    /// </summary>
    public class LogsharkArtifactProcessorOptions
    {
        protected readonly IDictionary<string, LogsharkArtifactProcessorConfiguration> artifactProcessorConfigurations;

        public LogsharkArtifactProcessorOptions(ArtifactProcessorOptions artifactProcessorConfigNodes)
        {
            artifactProcessorConfigurations = new Dictionary<string, LogsharkArtifactProcessorConfiguration>(StringComparer.OrdinalIgnoreCase);

            foreach (ArtifactProcessorConfigNode artifactProcessorConfigNode in artifactProcessorConfigNodes)
            {
                artifactProcessorConfigurations.Add(artifactProcessorConfigNode.Name, new LogsharkArtifactProcessorConfiguration(artifactProcessorConfigNode));
            }
        }

        public LogsharkArtifactProcessorConfiguration LoadConfiguration(Type artifactProcessorType)
        {
            if (!artifactProcessorConfigurations.ContainsKey(artifactProcessorType.Name))
            {
                throw new KeyNotFoundException(String.Format("No artifact processor configuration exists for '{0}'!", artifactProcessorType.Name));
            }

            return artifactProcessorConfigurations[artifactProcessorType.Name];
        }

        public override string ToString()
        {
            if (!artifactProcessorConfigurations.Any())
            {
                return "No artifact processor configurations have been loaded!";
            }

            var optionsStringBuilder = new StringBuilder();
            foreach (var artifactProcessorConfiguration in artifactProcessorConfigurations)
            {
                optionsStringBuilder.AppendFormat("[{0}: {1}]", artifactProcessorConfiguration.Key, artifactProcessorConfiguration.Value);
            }

            return optionsStringBuilder.ToString();
        }
    }
}