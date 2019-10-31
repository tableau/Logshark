using ServiceStack.OrmLite;

namespace Logshark.PluginLib.Persistence.Database
{
    public class ConcurrentBatchDbPersister<T> : BaseConcurrentDbPersister<T> where T : new()
    {
        public ConcurrentBatchDbPersister(IDbConnectionFactory connectionFactory, int persisterPoolSize = PluginLibConstants.DEFAULT_PERSISTER_POOL_SIZE, int maxBatchSize = PluginLibConstants.DEFAULT_PERSISTER_MAX_BATCH_SIZE)
            : base(persisterPoolSize)
        {
            while (insertionThreadPool.Count < persisterPoolSize)
            {
                insertionThreadPool.Add(new BatchDbInsertionThread<T>(connectionFactory.OpenDbConnection(), maxBatchSize));
            }
        }
    }
}