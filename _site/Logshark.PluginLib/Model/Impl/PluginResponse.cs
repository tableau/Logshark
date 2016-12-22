using System;
using System.Collections.Generic;

namespace Logshark.PluginLib.Model.Impl
{
    public class PluginResponse : IPluginResponse
    {
        protected readonly IList<string> pluginErrors;
        protected readonly IDictionary<string, string> responseArguments;

        public string PluginName { get; private set; }
        public bool SuccessfulExecution { get; protected set; }
        public string FailureReason { get; protected set; }
        public bool GeneratedNoData { get; set; }
        public IList<string> WorkbooksOutput { get; protected set; }
        public TimeSpan PluginRunTime { get; set; }

        public PluginResponse(string pluginName)
        {
            pluginErrors = new List<string>();
            responseArguments = new Dictionary<string, string>();
            PluginName = pluginName;
            SuccessfulExecution = true;
            WorkbooksOutput = new List<string>();
        }

        public void SetExecutionOutcome(bool isSuccessful, string failureReason = null)
        {
            SuccessfulExecution = isSuccessful;
            if (!isSuccessful && !String.IsNullOrWhiteSpace(failureReason))
            {
                FailureReason = failureReason;
                AppendError(failureReason);
            }
        }

        public void AppendError(string error)
        {
            pluginErrors.Add(error);
        }

        public ICollection<string> GetErrors()
        {
            return pluginErrors;
        }

        public void SetResponseArgument(string key, string value)
        {
            responseArguments[key] = value;
        }

        public string GetResponseArgument(string key)
        {
            if (ContainsReponseArgument(key))
            {
                return responseArguments[key];
            }

            throw new KeyNotFoundException(String.Format("No values found in RequestArguments for key '{0}'", key));
        }

        public ICollection<string> GetResponseArgumentKeys()
        {
            return responseArguments.Keys;
        }

        public bool ContainsReponseArgument(string argumentName)
        {
            return responseArguments.ContainsKey(argumentName);
        }

        public override string ToString()
        {
            return PluginName;
        }
    }
}
