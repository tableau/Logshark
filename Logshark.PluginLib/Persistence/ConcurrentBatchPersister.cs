using ServiceStack.OrmLite;
using System;
using System.Collections.Generic;

namespace Logshark.PluginLib.Persistence
{
    public class ConcurrentBatchPersister<T> : BaseConcurrentPersister<T> where T : new()
    {
        public ConcurrentBatchPersister(IDbConnectionFactory connectionFactory, int poolSize = PluginLibConstants.DEFAULT_PERSISTER_POOL_SIZE, int maxBatchSize = PluginLibConstants.DEFAULT_PERSISTER_MAX_BATCH_SIZE, IDictionary<Type,long> recordsPersisted = null) 
            : base (recordsPersisted)
        {
            while (insertionThreadPool.Count < poolSize)
            {
                insertionThreadPool.Add(new BatchInsertionThread<T>(connectionFactory.OpenDbConnection(), maxBatchSize));
            }
        }
    }
}