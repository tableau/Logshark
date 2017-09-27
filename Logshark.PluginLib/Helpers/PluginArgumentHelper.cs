using Logshark.PluginModel.Model;
using System;

namespace Logshark.PluginLib.Helpers
{
    /// <summary>
    /// Handles lookup and conversion of custom plugin arguments.
    /// </summary>
    public static class PluginArgumentHelper
    {
        /// <summary>
        /// Looks up a custom plugin argument and returns its value, cast as an integer. Throws FormatException if cast fails.
        /// </summary>
        /// <param name="key">Key to lookup in pluginRequest.</param>
        /// <param name="pluginRequest">IPluginRequest object.</param>
        /// <returns>Custom plugin argument specified by key, cast as an integer.</returns>
        public static int GetAsInt(string key, IPluginRequest pluginRequest)
        {
            string value = GetRequestArgument(key, pluginRequest);
            try
            {
                return Int32.Parse(value);
            }
            catch
            {
                throw new FormatException(String.Format("Unable to parse value {0} for key {1} as integer.", value, key));
            }
        }

        /// <summary>
        /// Looks up a custom plugin argument and returns its value, cast as an integer. Throws FormatException if cast fails.
        /// </summary>
        /// <param name="key">Key to lookup in pluginRequest.</param>
        /// <param name="pluginRequest">IPluginRequest object.</param>
        /// <returns>Custom plugin argument specified by key, cast as an integer.</returns>
        public static double GetAsDouble(string key, IPluginRequest pluginRequest)
        {
            string value = GetRequestArgument(key, pluginRequest);
            try
            {
                return Double.Parse(value);
            }
            catch
            {
                throw new FormatException(String.Format("Unable to parse value {0} for key {1} as double.", value, key));
            }
        }

        /// <summary>
        /// Looks up a custom plugin argument and returns its value, cast as a boolean. Throws FormatException if cast fails.
        /// </summary>
        /// <param name="key">Key to lookup in pluginRequest.</param>
        /// <param name="pluginRequest">IPluginRequest object.</param>
        /// <returns>Custom plugin argument specified by key, cast as a boolean.</returns>
        public static bool GetAsBoolean(string key, IPluginRequest pluginRequest)
        {
            string value = GetRequestArgument(key, pluginRequest).ToLowerInvariant();
            try
            {
                return Boolean.Parse(value);
            }
            catch
            {
                throw new FormatException(String.Format("Unable to parse value {0} for key {1} as bool.", value, key));
            }
        }

        /// <summary>
        /// Looks up a custom plugin argument and returns its value, cast as a string. Throws FormatException if cast fails.
        /// </summary>
        /// <param name="key">Key to lookup in pluginRequest.</param>
        /// <param name="pluginRequest">IPluginRequest object.</param>
        /// <returns>Custom plugin argument specified by key, cast as a string.</returns>
        public static string GetAsString(string key, IPluginRequest pluginRequest)
        {
            return GetRequestArgument(key, pluginRequest);
        }

        private static string GetRequestArgument(string key, IPluginRequest pluginRequest)
        {
            return pluginRequest.GetRequestArgument(key).ToString();
        }
    }
}