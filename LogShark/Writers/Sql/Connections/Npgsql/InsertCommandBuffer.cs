using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogShark.Writers.Sql.Connections.Npgsql
{
    public class InsertCommandBuffer
    {
        private readonly int _batchSize;
        private readonly INpgsqlDataContext _context;
        private readonly ConcurrentDictionary<(string schema, string tableName), InsertBatchCommand> _insertBatchCommands;

        public InsertCommandBuffer(INpgsqlDataContext context, int batchSize)
        {
            _batchSize = batchSize;
            _context = context;
            _insertBatchCommands = new ConcurrentDictionary<(string schema, string tableName), InsertBatchCommand>();
        }

        public async Task Insert(string schema, string tableName, Dictionary<string, object> values)
        {
            if (!_insertBatchCommands.TryGetValue((schema, tableName), out InsertBatchCommand insertBatchCommand))
            {
                insertBatchCommand = _insertBatchCommands.GetOrAdd((schema, tableName), new InsertBatchCommand(_context, _batchSize, schema, tableName));
            }
            await insertBatchCommand.Insert(values);
        }

        public async Task Flush()
        {
            foreach (var command in _insertBatchCommands)
            {
                await command.Value.Flush();
            }
        }

        private class InsertBatchCommand
        {
            private readonly int _batchSize;
            private string _schema;
            private string _tableName;
            private ConcurrentQueue<Dictionary<string, object>> _batchedValues;
            private readonly INpgsqlDataContext _context;

            public InsertBatchCommand(INpgsqlDataContext context, int batchSize, string schema, string tableName)
            {
                _batchSize = batchSize;
                _context = context;
                _schema = schema;
                _tableName = tableName;
                _batchedValues = new ConcurrentQueue<Dictionary<string, object>>();
            }

            public async Task Insert(Dictionary<string, object> values)
            {
                _batchedValues.Enqueue(values);
                if (_batchedValues.Count >= _batchSize)
                {
                    await Flush();
                }
            }

            public async Task Flush()
            {
                if (_batchedValues.TryPeek(out Dictionary<string, object> peekedRow))
                {
                    var columnNames = peekedRow.Keys;
                    var columnNamesForInsertColumnList = String.Join(", ", columnNames.Select(v => $"\"{v}\""));
                    var commandBuilder = new StringBuilder();
                    commandBuilder.AppendLine($@"INSERT INTO ""{_schema}"".""{_tableName}"" ({columnNamesForInsertColumnList}) VALUES");

                    var parameters = new Dictionary<string, object>();
                    var valuesClauses = new List<string>();
                    while (_batchedValues.TryDequeue(out Dictionary<string, object> retrievedBatchedRow))
                    {
                        var parameterPlaceholders = new List<string>();
                        foreach (var columnName in columnNames)
                        {
                            var parameterName = $"{columnName}_{Guid.NewGuid().ToString("N")}";
                            parameters[parameterName] = retrievedBatchedRow.ContainsKey(columnName) ? retrievedBatchedRow[columnName] ?? DBNull.Value : DBNull.Value;
                            parameterPlaceholders.Add($"@{parameterName}");
                        }
                        valuesClauses.Add($"({String.Join(", ", parameterPlaceholders)})");
                    }
                    if (valuesClauses.Any())
                    {
                        var valuesClause = String.Join($",{Environment.NewLine}", valuesClauses);
                        commandBuilder.AppendLine(valuesClause);
                        await _context.ExecuteNonQuery(commandBuilder.ToString(), parameters);
                    }
                }
            }
        }
    }
}
