using Logshark.Config;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Logshark.RequestModel.Config
{
    /// <summary>
    /// Encapsulates configuration options for a single artifact processor.
    /// </summary>
    public class LogsharkArtifactProcessorConfiguration
    {
        public ISet<string> DefaultPlugins { get; protected set; }

        public LogsharkArtifactProcessorConfiguration(ArtifactProcessorConfigNode artifactProcessorConfig)
        {
            DefaultPlugins = new HashSet<string>();
            foreach (Plugin plugin in artifactProcessorConfig.DefaultPlugins)
            {
                DefaultPlugins.Add(plugin.Name);
            }
        }

        public override string ToString()
        {
            if (!DefaultPlugins.Any())
            {
                return "[DefaultPlugins: None]";
            }

            return String.Format("[DefaultPlugins: {0}]", String.Join(",", DefaultPlugins));
        }
    }
}
