using Logshark.PluginLib.Helpers;
using Logshark.PluginModel.Model;
using ServiceStack.OrmLite;
using System;
using System.Collections.Generic;
using System.Data;

namespace Logshark.PluginLib.Persistence
{
    public class ConcurrentCustomPersister<T> : BaseConcurrentPersister<T> where T : new()
    {
        public delegate InsertionResult InsertionMethod(IPluginRequest pluginRequest, IDbConnection connection, T item);

        public ConcurrentCustomPersister(IPluginRequest pluginRequest, IDbConnectionFactory connectionFactory, InsertionMethod customInsertionMethod, IDictionary<Type, long> recordsPersisted = null)
            : base(recordsPersisted)
        {
            int poolSize = GlobalPluginArgumentHelper.GetPersisterPoolSize(pluginRequest);

            while (insertionThreadPool.Count < poolSize)
            {
                insertionThreadPool.Add(new CustomInsertionThread<T>(pluginRequest, connectionFactory.OpenDbConnection(), customInsertionMethod));
            }
        }
    }
}