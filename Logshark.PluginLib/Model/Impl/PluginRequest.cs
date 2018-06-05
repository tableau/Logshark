using Logshark.PluginModel.Model;
using System;
using System.Collections.Generic;

namespace Logshark.PluginLib.Model.Impl
{
    public class PluginRequest : IPluginRequest
    {
        public Guid LogsetHash { get; private set; }
        public string OutputDirectory { get; private set; }
        public string RunId { get; private set; }

        private readonly IDictionary<string, object> requestArguments;

        public PluginRequest(Guid logsetHash, string outputDirectory, string runId)
        {
            LogsetHash = logsetHash;
            OutputDirectory = outputDirectory;
            RunId = runId;
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

        public IDictionary<string, object> GetRequestArguments()
        {
            return new SortedDictionary<string, object>(requestArguments);
        }

        public bool ContainsRequestArgument(string argumentName)
        {
            return requestArguments.ContainsKey(argumentName);
        }
    }
}