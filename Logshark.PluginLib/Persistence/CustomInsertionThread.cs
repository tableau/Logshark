using System.Data;

namespace Logshark.PluginLib.Persistence
{
    internal class CustomInsertionThread<T> : BaseInsertionThread<T> where T : new()
    {
        protected ConcurrentCustomPersister<T>.InsertionMethod invokeInsertionMethod;

        public CustomInsertionThread(IDbConnection dbConnection, ConcurrentCustomPersister<T>.InsertionMethod customInsertionMethod)
        {
            DbConnection = dbConnection;
            invokeInsertionMethod = customInsertionMethod;
        }
    
        protected override void Insert(T item)
        {
            InsertionResult result = invokeInsertionMethod(DbConnection, item);
            ItemsPersisted += result.SuccessfulInserts;
        }
    }
}