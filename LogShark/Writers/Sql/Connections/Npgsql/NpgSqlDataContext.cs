using Microsoft.Extensions.Logging;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using LogShark.Exceptions;
using LogShark.Shared;

namespace LogShark.Writers.Sql.Connections.Npgsql
{
    public class NpgsqlDataContext : INpgsqlDataContext
    {
        private readonly ILogger<NpgsqlDataContext> _logger;
        private readonly string _connectionString;
        private readonly string _connectionStringForServiceDatabase;

        public string DatabaseName { get; }

        public NpgsqlDataContext(DbConnectionStringBuilder connectionStringBuilder, string serviceDbName, ILoggerFactory loggerFactory)
        {
            DatabaseName = connectionStringBuilder.ContainsKey("Database")
                ? connectionStringBuilder["Database"].ToString()
                : throw new LogSharkConfigurationException("Database name cannot be empty! It must be supplied through configuration file or command line when Postgres writer is used");
            
            _connectionString = connectionStringBuilder.ConnectionString;
            _connectionStringForServiceDatabase = string.IsNullOrWhiteSpace(serviceDbName)
                ? _connectionString
                : GetConnectionStringForServiceDatabase(connectionStringBuilder, serviceDbName);

            _logger = loggerFactory.CreateLogger<NpgsqlDataContext>();
        }

        private static string GetConnectionStringForServiceDatabase(DbConnectionStringBuilder connectionStringBuilder, string serviceDbName)
        {
            var cloneConnectionString = new DbConnectionStringBuilder
            {
                ConnectionString = connectionStringBuilder.ConnectionString
            };
            cloneConnectionString.Remove("database");
            cloneConnectionString.Add("database", serviceDbName);
            return cloneConnectionString.ConnectionString;
        }

        public async Task<int> ExecuteNonQuery(
            string commandText,
            Dictionary<string, object> parameters = null,
            [CallerFilePath] string filePath = "",
            [CallerMemberName] string memberName = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            return await ExecuteQueryWithRetries(
                _connectionString,
                async connection => await ExecuteNonQuery(connection, commandText, parameters),
                filePath,
                memberName,
                lineNumber
            );
        }

        public async Task<int> ExecuteNonQueryToServiceDatabase(
            string commandText,
            Dictionary<string, object> parameters = null,
            [CallerFilePath] string filePath = "",
            [CallerMemberName] string memberName = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            return await ExecuteQueryWithRetries(
                _connectionStringForServiceDatabase,
                async connection => await ExecuteNonQuery(connection, commandText, parameters),
                filePath,
                memberName,
                lineNumber
            );
        }

        private static async Task<int> ExecuteNonQuery(
            NpgsqlConnection connection,
            string commandText,
            Dictionary<string, object> parameters)
        {
            int rowsAffected;
            using (var command = connection.CreateCommand())
            {
                command.CommandText = commandText;
                if (parameters != null)
                {
                    foreach (var parameter in parameters)
                    {
                        command.Parameters.AddWithValue(parameter.Key, parameter.Value ?? DBNull.Value);
                    }
                }
                rowsAffected = await command.ExecuteNonQueryAsync();
            }
            return rowsAffected;
        }

        public async Task<T> ExecuteScalar<T>(
            string commandText,
            Dictionary<string, object> parameters = null,
            [CallerFilePath] string filePath = "",
            [CallerMemberName] string memberName = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            return await ExecuteQueryWithRetries(
                _connectionString,
                async connection => await ExecuteScalar<T>(connection, parameters, commandText),
                filePath,
                memberName,
                lineNumber
            );
        }

        public async Task<T> ExecuteScalarToServiceDatabase<T>(
            string commandText,
            Dictionary<string, object> parameters = null,
            [CallerFilePath] string filePath = "",
            [CallerMemberName] string memberName = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            return await ExecuteQueryWithRetries(
                _connectionStringForServiceDatabase,
                async connection => await ExecuteScalar<T>(connection, parameters, commandText),
                filePath,
                memberName,
                lineNumber
            );
        }

        private static async Task<T> ExecuteScalar<T>(
            NpgsqlConnection connection,
            Dictionary<string, object> parameters,
            string commandText)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = commandText;
                if (parameters != null)
                {
                    foreach (var parameter in parameters)
                    {
                        command.Parameters.AddWithValue(parameter.Key, parameter.Value ?? DBNull.Value);
                    }
                }
                var returnObject = await command.ExecuteScalarAsync();
                return (T)returnObject;
            }
        }

        private async Task<T> ExecuteQueryWithRetries<T>(
            string connectionString,
            Func<NpgsqlConnection, Task<T>> queryFunction,
            string filePath,
            string memberName,
            int lineNumber)
        {
            try
            {
                return await Retry.DoWithRetries<NpgsqlException, T>(
                    nameof(NpgsqlDataContext),
                    _logger,
                    async () =>
                    {
                        using (var connection = new NpgsqlConnectionWrapper(connectionString))
                        {
                            return await queryFunction(connection.Connection);
                        }
                    },
                    5,
                    10);
            }
            catch (NpgsqlOperationInProgressException ex)
            {
                LogError(ex, filePath, memberName, lineNumber);
                throw;
            }
            catch (NpgsqlException ex)
            {
                LogError(ex, filePath, memberName, lineNumber);
                throw;
            }
           
        }
        
        private void LogError(Exception ex, string filePath, string memberName, int lineNumber)
        {
            _logger.LogError(ex, $"SQL error occured at {Path.GetFileNameWithoutExtension(filePath)}.{memberName}:{lineNumber}.");
        }
    }
}
