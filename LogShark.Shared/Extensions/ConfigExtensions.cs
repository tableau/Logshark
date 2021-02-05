using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LogShark.Shared.Extensions
{
    public static class ConfigExtensions
    {
        public static T GetValueAndThrowAtNull<T>(this IConfiguration config, string valueKey)
        {
            var value = config.GetSection(valueKey).Get<T>();

            if (value == null)
            {
                throw new ArgumentException($"Value for `{valueKey}` cannot be null in config");
            }

            return value;
        }

        public static string GetStringWithDefaultIfEmpty(this IConfiguration config, string valueKey, string @default)
        {
            var value = config.GetSection(valueKey).Get<string>();

            if (string.IsNullOrEmpty(value))
            {
                value = @default;
            }

            return value;
        }

        public static ISet<string> GetSemicolonSeparatedDistinctStringArray(this IConfiguration config, string valueKey)
        {
            var value = config.GetSection(valueKey).Get<string>();
            return string.IsNullOrWhiteSpace(value)
                ? null
                : value.Split(";").Distinct().ToHashSet();
        }

        public static T GetConfigurationValueOrDefault<T>(this IConfiguration config, string strConfigSection, T defaultValue, ILogger logger = null)
        {
            var configSection = config?.GetSection(strConfigSection);
            var configSectionExists = configSection != null && configSection.Exists();

            if (configSectionExists)
            {
                try
                {
                    return configSection.Get<T>();
                }
                catch (Exception ex)
                {
                    logger?.LogDebug(ex,
                        "Failed to parse value for config key `{incorrectValueKey}` as `{expectedType}`. Using default value `{defaultValue}` instead",
                        strConfigSection,
                        nameof(T),
                        defaultValue
                        );
                    return defaultValue;
                }
            }
            
            logger?.LogDebug(
                "No config section {incorrectValueKey} specified, defaulting to {defaultValue}",
                strConfigSection,
                defaultValue);
            
            return defaultValue;
        }
    }
}