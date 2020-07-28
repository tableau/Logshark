using LogShark.Containers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LogShark.Writers.Sql.Connections.Npgsql
{
    public class NpgsqlRepository : ISqlRepository
    {
        private readonly INpgsqlDataContext _context;
        private readonly NpgsqlTypeProjector _typeProjector;
        private readonly InsertCommandBuffer _insertCommandBuffer;

        public NpgsqlRepository(INpgsqlDataContext context, int batchSize)
        {
            _context = context;
            _typeProjector = new NpgsqlTypeProjector();
            _insertCommandBuffer = new InsertCommandBuffer(_context, batchSize);
        }

        public string DatabaseName => _context.DatabaseName;

        public void RegisterType<T>(DataSetInfo outputInfo)
        {
            _typeProjector.GenerateTypeProjection<T>(outputInfo);
        }

        public async Task CreateDatabaseIfNotExist()
        {
            if (!await DoesDatabaseExist(_context.DatabaseName))
            {
                var commandText = $@"CREATE DATABASE ""{_context.DatabaseName}"" ENCODING 'UTF8'";
                await _context.ExecuteNonQueryToServiceDatabase(commandText);
            }
        }

        private async Task<bool> DoesDatabaseExist(string databaseName)
        {
            var commandText =
                $@"SELECT EXISTS
                (
                    SELECT datname
                    FROM pg_catalog.pg_database
                    WHERE datname = '{databaseName}'
                );";
            return await _context.ExecuteScalarToServiceDatabase<bool>(commandText);
        }

        public async Task CreateColumnsForTypeIfNotExist<T>()
        {
            var typeProjection = _typeProjector.GetTypeProjection<T>();
            foreach (var typePropertyProjection in typeProjection.TypePropertyProjections)
            {
                if (!await DoesColumnExist(typeProjection.Schema, typeProjection.TableName, typePropertyProjection.ColumnName))
                {
                    var commandText =
                        $@"ALTER TABLE ""{typeProjection.Schema}"".""{typeProjection.TableName}""
                        ADD COLUMN ""{typePropertyProjection.ColumnName}"" {typePropertyProjection.NpgsqlTypeName};";
                    await _context.ExecuteNonQuery(commandText);
                }
            }
        }

        public async Task CreateColumnWithForeignKeyIfNotExist<TSource, TTarget>(string sourceColumnName, string targetColumnName)
        {
            var sourceTypeProjection = _typeProjector.GetTypeProjection<TSource>();
            var targetTypeProjection = _typeProjector.GetTypeProjection<TTarget>();
            if (!await DoesColumnExist(sourceTypeProjection.Schema, sourceTypeProjection.TableName, sourceColumnName))
            {
                var commandText =
                    $@"ALTER TABLE ""{sourceTypeProjection.Schema}"".""{sourceTypeProjection.TableName}""
                    ADD COLUMN ""{sourceColumnName}"" INTEGER
                    REFERENCES ""{targetTypeProjection.Schema}"".""{targetTypeProjection.TableName}""(""{targetColumnName}"");";
                await _context.ExecuteNonQuery(commandText);
            }
        }

        public async Task CreateColumnWithPrimaryKeyIfNotExist<T>(string columnName)
        {
            var typeProjection = _typeProjector.GetTypeProjection<T>();
            if (!await DoesColumnExist(typeProjection.Schema, typeProjection.TableName, columnName))
            {
                var commandText =
                    $@"ALTER TABLE ""{typeProjection.Schema}"".""{typeProjection.TableName}""
                    ADD COLUMN ""{columnName}"" SERIAL PRIMARY KEY;";
                await _context.ExecuteNonQuery(commandText);
            }
        }

        private async Task<bool> DoesColumnExist(string schema, string tableName, string columnName)
        {
            var commandText =
                $@"SELECT EXISTS
                (
                    SELECT column_name
                    FROM information_schema.columns
                    WHERE 
                        table_schema = '{schema}' AND
                        table_name = '{tableName}' AND
                        column_name = '{columnName}'
                );";
            return await _context.ExecuteScalar<bool>(commandText);
        }

        public async Task CreateSchemaIfNotExist<T>()
        {
            var typeProjection = _typeProjector.GetTypeProjection<T>();
            var commandText = $@"CREATE SCHEMA IF NOT EXISTS ""{typeProjection.Schema}"";";
            await _context.ExecuteNonQuery(commandText);
        }

        public async Task CreateTableIfNotExist<T>()
        {
            var typeProjection = _typeProjector.GetTypeProjection<T>();
            var commandText = $@"CREATE TABLE IF NOT EXISTS ""{typeProjection.Schema}"".""{typeProjection.TableName}""();";
            await _context.ExecuteNonQuery(commandText);
        }

        public async Task InsertRow<T>(T row, Dictionary<string, object> valueOverrides = null)
        {
            var typeProjection = _typeProjector.GetTypeProjection<T>();
            var values = GenerateValues(row, typeProjection, valueOverrides);
            await _insertCommandBuffer.Insert(
                typeProjection.Schema,
                typeProjection.TableName,
                values);
        }

        public async Task<TReturn> InsertRowWithReturnValue<T, TReturn>(T row, string returnColumnName, Dictionary<string, object> valueOverrides = null, IEnumerable<string> excludeColumns = null)
        {
            var typeProjection = _typeProjector.GetTypeProjection<T>();
            var values = GenerateValues(row, typeProjection, valueOverrides, excludeColumns);

            var columnNames = values.Keys;
            var columnNamesForInsertColumnList = String.Join(", ", columnNames.Select(v => $"\"{v}\""));
            var parameterNames = String.Join(", ", columnNames.Select(v => $"@{v}"));

            var commandText =
                $@"INSERT INTO ""{typeProjection.Schema}"".""{typeProjection.TableName}"" ({columnNamesForInsertColumnList})
                VALUES ({parameterNames})
                RETURNING ""{returnColumnName}"";";
            return await _context.ExecuteScalar<TReturn>(commandText, values);
        }

        private static Dictionary<string, object> GenerateValues<T>(T row, NpgsqlTypeProjection<T> typeProjection, Dictionary<string, object> valueOverrides, IEnumerable<string> excludeColumns = null)
        {
            var values = typeProjection.TypePropertyProjections.ToDictionary(
                tpp => tpp.ColumnName,
                tpp =>
                {
                    switch (tpp.GetPropertyValue(row))
                    {
                        case string valueStr:
                            return valueStr.Replace("\u0000", "");
                        case object value:
                            return value;
                    }
                    return null;
                }
            );

            valueOverrides?.ToList().ForEach(kvp =>
            {
                if (values.ContainsKey(kvp.Key))
                {
                    values[kvp.Key] = kvp.Value;
                }
                else
                {
                    values.Add(kvp.Key, kvp.Value);    
                }
            });

            if (excludeColumns != null)
            {
                foreach (var excludedColumn in excludeColumns)
                {
                    values.Remove(excludedColumn);
                }
            }

            return values;
        }

        public async Task Flush()
        {
            await _insertCommandBuffer.Flush();
        }

        public void Dispose()
        {
        }
    }
}