using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

namespace LogShark.Tests.Plugins.Helpers
{
    public static class ConfigGenerator
    {
        public static IConfiguration GetConfigWithASingleValue(string key, string value)
        {
            var keepingConfig = new Dictionary<string, string>
            {
                [key] = value
            };

            return new ConfigurationBuilder()
                .AddInMemoryCollection(keepingConfig)
                .Build();
        }

        public static IConfiguration GetConfigFromDictionary(Dictionary<string,string> config)
        {
            return new ConfigurationBuilder()
                .AddInMemoryCollection(config)
                .Build();
        }
    }
}