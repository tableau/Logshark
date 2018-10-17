using Logshark.PluginModel.Model;
using System.Data;

namespace Logshark.PluginLib.Persistence.Database
{
    internal class CustomDbInsertionThread<T> : BaseDbInsertionThread<T> where T : new()
    {
        protected readonly IPluginRequest pluginRequest;
        protected readonly ConcurrentCustomDbPersister<T>.InsertionMethod invokeInsertionMethod;

        public CustomDbInsertionThread(IPluginRequest pluginRequest, IDbConnection dbConnection, ConcurrentCustomDbPersister<T>.InsertionMethod customInsertionMethod)
        {
            this.pluginRequest = pluginRequest;
            DbConnection = dbConnection;
            invokeInsertionMethod = customInsertionMethod;
        }

        protected override void Insert(T item)
        {
            InsertionResult result = invokeInsertionMethod(pluginRequest, DbConnection, item);
            ItemsPersisted += result.SuccessfulInserts;
        }
    }
}