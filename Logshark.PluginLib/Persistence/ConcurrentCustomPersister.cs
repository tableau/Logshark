using ServiceStack.OrmLite;
using System;
using System.Collections.Generic;
using System.Data;

namespace Logshark.PluginLib.Persistence
{
    public class ConcurrentCustomPersister<T> : BaseConcurrentPersister<T> where T : new()
    {
        public delegate InsertionResult InsertionMethod(IDbConnection connection, T item);

        public ConcurrentCustomPersister(IDbConnectionFactory connectionFactory, InsertionMethod customInsertionMethod, int poolSize = PluginLibConstants.DEFAULT_PERSISTER_POOL_SIZE, IDictionary<Type,long> recordsPersisted = null) 
            : base (recordsPersisted)
        {
            while (insertionThreadPool.Count < poolSize)
            {
                insertionThreadPool.Add(new CustomInsertionThread<T>(connectionFactory.OpenDbConnection(), customInsertionMethod));
            }
        }
    }
}