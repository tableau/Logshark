using System;
using System.Collections.Generic;

namespace Logshark.PluginLib.Model
{
    public interface IPluginRequest
    {
        string OutputDirectory { get; }
        Guid LogsetHash { get; }

        void SetRequestArgument(string key, object value);
        object GetRequestArgument(string key);
        ICollection<string> GetRequestArgumentKeys();
        bool ContainsRequestArgument(string key);
    }
}
