using Logshark.PluginModel.Model;
using ServiceStack.OrmLite;
using System;
using System.Collections.Generic;

namespace Logshark.PluginLib.Persistence
{
    public abstract class BasePersisterFactory<T> : IPersisterFactory<T> where T : new()
    {
        protected readonly IDbConnectionFactory dbConnectionFactory;
        protected readonly IPluginRequest pluginRequest;
        protected readonly IDictionary<Type, long> persistedRecordJournal;

        protected BasePersisterFactory(IDbConnectionFactory dbConnectionFactory, IPluginRequest pluginRequest, IDictionary<Type, long> persistedRecordJournal = null)
        {
            if (dbConnectionFactory == null)
            {
                throw new ArgumentNullException("dbConnectionFactory");
            }
            if (pluginRequest == null)
            {
                throw new ArgumentNullException("pluginRequest");
            }

            this.dbConnectionFactory = dbConnectionFactory;
            this.pluginRequest = pluginRequest;
            this.persistedRecordJournal = persistedRecordJournal ?? new Dictionary<Type, long>();
        }

        public abstract IPersister<T> BuildPersister();
    }
}
