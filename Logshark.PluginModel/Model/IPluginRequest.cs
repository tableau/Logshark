using MongoDB.Driver;
using System;

namespace Logshark.PluginModel.Model
{
    public interface IPluginRequest
    {
        string OutputDirectory { get; }
        string TempDirectory { get; }
        string LogDirectory { get; }

        Guid LogsetHash { get; }
        string RunId { get; }

        IMongoDatabase MongoDatabase { get; }

        void SetRequestArgument(string key, object value);
        object GetRequestArgument(string key);
        bool ContainsRequestArgument(string key);
    }
}