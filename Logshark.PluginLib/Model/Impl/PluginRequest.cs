using Logshark.PluginModel.Model;
using MongoDB.Driver;
using System;
using System.Collections.Generic;

namespace Logshark.PluginLib.Model.Impl
{
    public class PluginRequest : IPluginRequest
    {
        protected readonly IDictionary<string, object> requestArguments;

        public Guid LogsetHash { get; private set; }

        public string OutputDirectory { get; private set; }
        public string TempDirectory { get; private set; }
        public string LogDirectory { get; private set; }

        public string RunId { get; private set; }
        public IMongoDatabase MongoDatabase { get; private set; }

        public PluginRequest(Guid logsetHash, string outputDirectory, string tempDirectory, string logDirectory, string runId, IMongoDatabase mongoDatabase)
        {
            LogsetHash = logsetHash;
            OutputDirectory = outputDirectory;
            TempDirectory = tempDirectory;
            LogDirectory = logDirectory;
            RunId = runId;
            MongoDatabase = mongoDatabase;

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

        public bool ContainsRequestArgument(string argumentName)
        {
            return requestArguments.ContainsKey(argumentName);
        }
    }
}