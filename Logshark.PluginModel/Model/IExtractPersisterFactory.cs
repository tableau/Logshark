using System;

namespace Logshark.PluginModel.Model
{
    public interface IExtractPersisterFactory
    {
        IPersister<T> CreateExtract<T>(Action<T> insertionCallback = null) where T : new();

        IPersister<T> CreateExtract<T>(string extractFilename, Action<T> insertionCallback = null) where T : new();
    }
}