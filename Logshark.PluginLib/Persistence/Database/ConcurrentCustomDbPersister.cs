using Logshark.PluginModel.Model;
using ServiceStack.OrmLite;
using System.Data;

namespace Logshark.PluginLib.Persistence.Database
{
    public class ConcurrentCustomDbPersister<T> : BaseConcurrentDbPersister<T> where T : new()
    {
        public delegate InsertionResult InsertionMethod(IPluginRequest pluginRequest, IDbConnection connection, T item);

        public ConcurrentCustomDbPersister(IDbConnectionFactory connectionFactory, IPluginRequest pluginRequest, InsertionMethod customInsertionMethod, int persisterPoolSize = PluginLibConstants.DEFAULT_PERSISTER_POOL_SIZE)
            : base(persisterPoolSize)
        {
            while (insertionThreadPool.Count < persisterPoolSize)
            {
                insertionThreadPool.Add(new CustomDbInsertionThread<T>(pluginRequest, connectionFactory.OpenDbConnection(), customInsertionMethod));
            }
        }
    }
}