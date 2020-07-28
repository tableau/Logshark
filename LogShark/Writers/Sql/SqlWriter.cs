using LogShark.Containers;
using LogShark.Writers.Sql.Connections;
using LogShark.Writers.Sql.Models;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LogShark.Writers.Sql
{
    public class SqlWriter<T> : BaseWriter<T>
    {
        private readonly ISqlRepository _repository;
        private Dictionary<string, object> _valueOverrides;
        
        public SqlWriter(ISqlRepository repository, DataSetInfo dataSetInfo, ILogger logger)
        : base(dataSetInfo, logger, nameof(SqlWriter<T>))
        {
            _repository = repository;
            _repository.RegisterType<T>(dataSetInfo);
        }

        public async Task InitializeTable(string logSharkRunIdColumnName, int logSharkRunId, bool skipDbVerifyAndInit)
        {
            _valueOverrides = new Dictionary<string, object>
            {
                [logSharkRunIdColumnName] = logSharkRunId
            };

            if (skipDbVerifyAndInit)
            {
                return;
            }

            await _repository.CreateSchemaIfNotExist<T>();
            await _repository.CreateTableIfNotExist<T>();
            await _repository.CreateColumnWithForeignKeyIfNotExist<T, LogSharkRunModel>(logSharkRunIdColumnName, logSharkRunIdColumnName);
            await _repository.CreateColumnsForTypeIfNotExist<T>();
        }

        protected override void InsertNonNullLineLogic(T objectToWrite)
        {
            _repository.InsertRow(objectToWrite, _valueOverrides).Wait();
        }

        protected override void CloseLogic()
        {
            _repository.Flush().Wait();
        }

        public override void Dispose()
        {
            base.Dispose();
            _repository?.Dispose();
        }
    }
}
