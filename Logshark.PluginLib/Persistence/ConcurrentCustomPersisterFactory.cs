using Logshark.PluginModel.Model;
using ServiceStack.OrmLite;
using System;
using System.Collections.Generic;

namespace Logshark.PluginLib.Persistence
{
    public class ConcurrentCustomPersisterFactory<T> : BasePersisterFactory<T> where T : new()
    {
        protected readonly ConcurrentCustomPersister<T>.InsertionMethod insertionMethod;

        public ConcurrentCustomPersisterFactory(IDbConnectionFactory dbConnectionFactory,
                                                IPluginRequest pluginRequest,
                                                ConcurrentCustomPersister<T>.InsertionMethod insertionMethod,
                                                IDictionary<Type, long> persistedRecordJournal = null)
            : base(dbConnectionFactory, pluginRequest, persistedRecordJournal)
        {
            this.insertionMethod = insertionMethod;
        }

        public override IPersister<T> BuildPersister()
        {
            return new ConcurrentCustomPersister<T>(dbConnectionFactory, pluginRequest, insertionMethod, persistedRecordJournal);
        }
    }
}