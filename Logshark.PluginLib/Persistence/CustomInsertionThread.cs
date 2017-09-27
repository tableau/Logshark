using Logshark.PluginModel.Model;
using System.Data;

namespace Logshark.PluginLib.Persistence
{
    internal class CustomInsertionThread<T> : BaseInsertionThread<T> where T : new()
    {
        protected readonly IPluginRequest pluginRequest;
        protected readonly ConcurrentCustomPersister<T>.InsertionMethod invokeInsertionMethod;

        public CustomInsertionThread(IPluginRequest pluginRequest, IDbConnection dbConnection, ConcurrentCustomPersister<T>.InsertionMethod customInsertionMethod)
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