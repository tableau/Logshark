using Logshark.PluginLib.Helpers;
using Logshark.PluginModel.Model;
using ServiceStack.OrmLite;
using System;
using System.Collections.Generic;

namespace Logshark.PluginLib.Persistence
{
    public class ConcurrentBatchPersisterFactory<T> : BasePersisterFactory<T> where T : new()
    {
        public ConcurrentBatchPersisterFactory(IDbConnectionFactory dbConnectionFactory, IPluginRequest pluginRequest, IDictionary<Type, long> persistedRecordJournal = null)
            : base(dbConnectionFactory, pluginRequest, persistedRecordJournal)
        {
        }

        public override IPersister<T> BuildPersister()
        {
            int poolSize = GlobalPluginArgumentHelper.GetPersisterPoolSize(pluginRequest);
            int batchSize = GlobalPluginArgumentHelper.GetPersisterBatchSize(pluginRequest);

            return new ConcurrentBatchPersister<T>(dbConnectionFactory, poolSize, batchSize, persistedRecordJournal);
        }
    }
}