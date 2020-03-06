using LogShark.Containers;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace LogShark.Writers.Sql.Connections
{
    public interface ISqlRepository : IDisposable
    {
        string DatabaseName { get; }

        Task CreateDatabaseIfNotExist();
        Task CreateTableIfNotExist<T>();
        Task CreateColumnsForTypeIfNotExist<T>();
        Task CreateColumnWithPrimaryKeyIfNotExist<T>(string columnName);
        Task CreateColumnWithForeignKeyIfNotExist<TSource, TTarget>(string sourceColumnName, string targetColumnName);
        Task CreateSchemaIfNotExist<T>();
        Task InsertRow<T>(T row, Dictionary<string, object> additionalValues = null);
        Task<TReturn> InsertRowWithReturnValue<T, TReturn>(T row, string returnColumnName, Dictionary<string, object> additionalValues = null);
        Task Flush();
        void RegisterType<T>(DataSetInfo outputInfo);
    }
}
