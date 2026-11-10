using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LogShark.Shared.HostExtensions
{
    /// <summary>
    /// Optional extension discovered by the CLI at startup (see HostExtensionLoader in LogShark).
    /// Implementations must have a public parameterless constructor.
    /// </summary>
    public interface ILogSharkHostExtension
    {
        /// <summary>Absolute path of a JSON config file layered beneath the main config, or null.</summary>
        string ConfigOverlayPath { get; }

        IArtifactPublisher CreateArtifactPublisher(IConfiguration configuration, ILoggerFactory loggerFactory);
    }
}
