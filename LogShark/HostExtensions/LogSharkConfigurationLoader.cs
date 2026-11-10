using System.IO;
using Microsoft.Extensions.Configuration;

namespace LogShark.HostExtensions
{
    public static class LogSharkConfigurationLoader
    {
        // Overlay is added first so values in the main (or user-supplied --config) file always win
        public static IConfigurationRoot Build(string basePath, string configFile, string overlayPath)
        {
            var builder = new ConfigurationBuilder().SetBasePath(basePath);
            if (!string.IsNullOrWhiteSpace(overlayPath))
            {
                // Only an extension supplies an overlay, so a missing one means a broken internal install;
                // continuing would fail later with an unhandled error from the extension's missing settings.
                var fullOverlayPath = Path.GetFullPath(Path.Combine(basePath, overlayPath));
                if (!File.Exists(fullOverlayPath))
                {
                    throw new HostExtensionLoadException($"Host extension config overlay '{fullOverlayPath}' was not found; the host extension installation is incomplete");
                }
                builder.AddJsonFile(fullOverlayPath, optional: false, reloadOnChange: false);
            }
            builder.AddJsonFile(configFile, optional: false, reloadOnChange: false);
            return builder.Build();
        }
    }
}
