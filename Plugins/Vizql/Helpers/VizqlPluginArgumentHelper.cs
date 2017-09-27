using Logshark.PluginLib.Helpers;
using Logshark.PluginModel.Model;
using System;

namespace Logshark.Plugins.Vizql.Helpers
{
    internal static class VizqlPluginArgumentHelper
    {
        public static int GetMaxQueryLength(IPluginRequest pluginRequest, string maxQueryLengthArgumentKey, int defaultIfNotFound)
        {
            if (pluginRequest.ContainsRequestArgument(maxQueryLengthArgumentKey))
            {
                try
                {
                    return PluginArgumentHelper.GetAsInt(maxQueryLengthArgumentKey, pluginRequest);
                }
                catch (FormatException)
                {
                    return defaultIfNotFound;
                }
            }

            return defaultIfNotFound;
        }
    }
}
