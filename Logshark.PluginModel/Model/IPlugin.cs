using MongoDB.Driver;
using System.Collections.Generic;

namespace Logshark.PluginModel.Model
{
    public interface IPlugin
    {
        ISet<string> CollectionDependencies { get; }

        IMongoDatabase MongoDatabase { get; }
        IExtractPersisterFactory ExtractFactory { get; }

        IPluginResponse Execute();
    }
}