using System;
using System.Collections.Generic;

namespace Logshark.PluginModel.Model
{
    public interface IPluginRequest
    {
        string OutputDirectory { get; }
        Guid LogsetHash { get; }
        string RunId { get; }

        void SetRequestArgument(string key, object value);

        object GetRequestArgument(string key);

        ICollection<string> GetRequestArgumentKeys();

        IDictionary<string, object> GetRequestArguments();

        bool ContainsRequestArgument(string key);
    }
}