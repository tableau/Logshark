using MongoDB.Driver;
using System;
using System.Collections.Generic;

namespace Logshark.PluginLib.Model.Impl
{
    public class PluginRequest : IPluginRequest
    {
        public IMongoDatabase MongoDatabase { get; private set; }
        public Guid LogsetHash { get; private set; }
        public string OutputDirectory { get; private set; }

        private readonly IDictionary<string, object> requestArguments;

        public PluginRequest(IMongoDatabase mongoDatabase, Guid logsetHash, string outputDirectory)
        {
            MongoDatabase = mongoDatabase;
            LogsetHash = logsetHash;
            OutputDirectory = outputDirectory;
            requestArguments = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        }

        public void SetRequestArgument(string argumentName, object argumentValue)
        {
            requestArguments[argumentName] = argumentValue;
        }

        public object GetRequestArgument(string argumentName)
        {
            if (requestArguments.ContainsKey(argumentName))
            {
                return requestArguments[argumentName];
            }

            throw new KeyNotFoundException(String.Format("No values found in RequestArguments for key '{0}'", argumentName));
        }

        public ICollection<string> GetRequestArgumentKeys()
        {
            return requestArguments.Keys;
        }

        public bool ContainsRequestArgument(string argumentName)
        {
            return requestArguments.ContainsKey(argumentName);
        }
    }
}
