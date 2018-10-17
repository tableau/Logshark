using log4net;
using Logshark.PluginLib.Logging;
using Logshark.PluginLib.Persistence.Extract;
using Logshark.PluginLib.StatusWriter;
using Logshark.PluginModel.Model;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Logshark.PluginLib.Model.Impl
{
    /// <summary>
    /// The base plugin from which all other plugins should inherit.
    /// </summary>
    public abstract class BasePlugin : IPlugin
    {
        protected readonly IPluginRequest pluginRequest;

        public abstract ISet<string> CollectionDependencies { get; }

        public IMongoDatabase MongoDatabase { get; private set; }
        public IExtractPersisterFactory ExtractFactory { get; private set; }

        public ILog Log { get; private set; }

        protected BasePlugin()
        {
        }

        protected BasePlugin(IPluginRequest pluginRequest)
        {
            this.pluginRequest = pluginRequest;

            MongoDatabase = pluginRequest.MongoDatabase;

            Type pluginType = GetType();

            Log = PluginLogFactory.GetLogger(pluginType);
            ExtractFactory = new ExtractPersisterFactory(pluginRequest.OutputDirectory, Log, pluginRequest.TempDirectory, pluginRequest.LogDirectory);
        }

        /// <summary>
        /// The method plugin creators will need to override to create a plugin.
        /// </summary>
        /// <returns>PluginResponse indicating if this plugin executed successfully.</returns>
        public abstract IPluginResponse Execute();

        /// <summary>
        /// Helper method for plugin authors to be able to easily new up a PluginResponse for the derived plugin type.
        /// </summary>
        /// <returns>PluginResponse for the derived plugin type.</returns>
        protected PluginResponse CreatePluginResponse()
        {
            return new PluginResponse(GetType().Name);
        }

        protected PersisterStatusWriter<T> GetPersisterStatusWriter<T>(IPersister<T> persister, long? expectedTotalPersistedItems = null) where T : new()
        {
            string progressFormatMessage;
            if (expectedTotalPersistedItems.HasValue)
            {
                progressFormatMessage = PluginLibConstants.DEFAULT_PERSISTER_STATUS_WRITER_PROGRESS_MESSAGE_WITH_TOTAL;
            }
            else
            {
                progressFormatMessage = PluginLibConstants.DEFAULT_PERSISTER_STATUS_WRITER_PROGRESS_MESSAGE;
            }

            return new PersisterStatusWriter<T>(persister, Log, progressFormatMessage, PluginLibConstants.DEFAULT_PROGRESS_MONITOR_POLLING_INTERVAL_SECONDS, expectedTotalPersistedItems);
        }

        protected TaskStatusWriter GetTaskStatusWriter(ICollection<Task> tasks, string taskType, long? expectedTotalTasks = null)
        {
            string progressFormatMessage;
            if (expectedTotalTasks.HasValue)
            {
                progressFormatMessage = PluginLibConstants.DEFAULT_TASK_STATUS_WRITER_PROGRESS_MESSAGE_WITH_TOTAL.Replace("{TaskType}", taskType);
            }
            else
            {
                progressFormatMessage = PluginLibConstants.DEFAULT_PERSISTER_STATUS_WRITER_PROGRESS_MESSAGE.Replace("{TaskType}", taskType);
            }

            return new TaskStatusWriter(tasks, Log, progressFormatMessage, PluginLibConstants.DEFAULT_PROGRESS_MONITOR_POLLING_INTERVAL_SECONDS, expectedTotalTasks);
        }
    }
}