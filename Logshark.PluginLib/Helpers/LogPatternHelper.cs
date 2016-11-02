using log4net.Layout;
using Logshark.PluginLib.Logging;

namespace Logshark.PluginLib.Helpers
{
    internal static class LogPatternHelper
    {
        internal static PatternLayout GetConsolePatternLayout()
        {
            PatternLayout patternLayout = new PatternLayout
            {
                ConversionPattern = LoggingConstants.ConsolePattern
            };
            patternLayout.ActivateOptions();
            return patternLayout;
        }

        internal static PatternLayout GetFilePatternLayout()
        {
            PatternLayout patternLayout = new PatternLayout
            {
                ConversionPattern = LoggingConstants.FilePattern
            };
            patternLayout.ActivateOptions();
            return patternLayout;
        }
    }
}