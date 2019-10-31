using Logshark.RequestModel;
using Logshark.RequestModel.Config;
using System.Collections.Generic;

namespace Logshark.Core.Controller.Initialization
{
    public class RunInitializationRequest
    {
        public LogsharkRequestTarget Target { get; protected set; }

        public string RunId { get; protected set; }

        public ISet<string> RequestedPlugins { get; protected set; }

        // If enabled, every available collection will be included in the initialized collection dependency tree, instead of only the minimal amount to satisfy the requested plugins.
        public bool ParseFullLogset { get; protected set; }

        public LogsharkArtifactProcessorOptions ArtifactProcessorOptions { get; protected set; }

        public RunInitializationRequest(LogsharkRequestTarget target, string runId, ISet<string> requestedPlugins, bool parseFullLogset, LogsharkArtifactProcessorOptions artifactProcessorOptions)
        {
            Target = target;
            RunId = runId;
            RequestedPlugins = requestedPlugins;
            ParseFullLogset = parseFullLogset;
            ArtifactProcessorOptions = artifactProcessorOptions;
        }
    }
}