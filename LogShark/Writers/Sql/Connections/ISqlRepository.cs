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
        Task CreateTableIfNotExist<T>(DataSetInfo dataSetInfo);
        Task CreateColumnsForTypeIfNotExist<T>(DataSetInfo dataSetInfo);
        Task CreateColumnWithPrimaryKeyIfNotExist<T>(DataSetInfo dataSetInfo, string columnName);
        Task CreateColumnWithForeignKeyIfNotExist<TSource, TTarget>(DataSetInfo sourceDataSetInfo, DataSetInfo targetDataSetInfo, string sourceColumnName, string targetColumnName);
        Task CreateSchemaIfNotExist<T>(DataSetInfo dataSetInfo);
        Task InsertRow<T>(DataSetInfo dataSetInfo, T row, Dictionary<string, object> valueOverrides = null);
        Task<TReturn> InsertRowWithReturnValue<T, TReturn>(DataSetInfo dataSetInfo, T row, string returnColumnName, Dictionary<string, object> valueOverrides = null, IEnumerable<string> excludeColumns = null);
        Task Flush();
        void RegisterType<T>(DataSetInfo outputInfo);
    }
}
