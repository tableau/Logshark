using System;
using System.Collections.Generic;
using LogShark.Shared;
using LogShark.Writers;

namespace LogShark.Plugins
{
    public interface IPluginManager : IDisposable
    {
        bool IsValidPluginConfiguration(out IEnumerable<string> badPluginNames);
        IEnumerable<IPlugin> CreatePlugins(IWriterFactory writerFactory, IProcessingNotificationsCollector processingNotificationsCollector);
        IEnumerable<IPlugin> GetPlugins();
        IEnumerable<LogType> GetRequiredLogTypes();
        PluginsExecutionResults SendCompleteProcessingSignalToPlugins(bool runAborted = false);
        IEnumerable<string> GetKnownPluginNames();
    }
}