using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using LogShark.Containers;
using LogShark.Exceptions;
using LogShark.Writers.Containers;
using LogShark.Writers.Sql.Connections;
using LogShark.Writers.Sql.Connections.Npgsql;
using LogShark.Writers.Sql.Models;
using Microsoft.Extensions.Logging;
using static Tools.TableauServerRestApi.Containers.PublishWorkbookRequest;

namespace LogShark.Writers.Sql
{
    public class PostgresWriterFactory : IWriterFactory
    {
        private readonly string _runId;
        private readonly LogSharkConfiguration _config;
        private string _dbHost;
        private string _dbPort;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger<PostgresWriterFactory> _logger;
        private const string LogSharkRunIdColumnName = "logshark_run_id";
        private int _logSharkRunId;
        private DataSourceCredentials _dbCreds;

        private readonly ISqlRepository _repository;

        public PostgresWriterFactory(
            string runId,
            LogSharkConfiguration config,
            ILoggerFactory loggerFactory)
        {
            _runId = runId;
            _config = config;
            _loggerFactory = loggerFactory;
            _logger = _loggerFactory.CreateLogger<PostgresWriterFactory>();

            var connectionStringBuilder = GetConnectionStringBuilder();
            _repository = new NpgsqlRepository(new NpgsqlDataContext(connectionStringBuilder, _config.PostgresServiceDatabaseName, loggerFactory), _config.PostgresBatchSize);
        }

        private DbConnectionStringBuilder GetConnectionStringBuilder()
        {
            var connectionStringBuilder = new DbConnectionStringBuilder();
            var isRawConnectionStringSupplied = !String.IsNullOrWhiteSpace(_config.PostgresConnectionString);
            if (isRawConnectionStringSupplied)
            {
                connectionStringBuilder.ConnectionString = _config.PostgresConnectionString;
            }

            var timeoutFromConnectionString = connectionStringBuilder.ContainsKey("Timeout") && int.TryParse((string)connectionStringBuilder["Timeout"], out var parsedTimeout)
                ? (int?) parsedTimeout 
                : null;
            var timeout = _config.PostgresConnectionTimeoutSeconds ?? timeoutFromConnectionString ?? 30;
            connectionStringBuilder.Add("Timeout", timeout);

            var areCredentialsSupplied = false;
            if (!String.IsNullOrWhiteSpace(_config.PostgresUsername))
            {
                areCredentialsSupplied = true;
                connectionStringBuilder.Add("User Id", _config.PostgresUsername);
            }
            if (!String.IsNullOrWhiteSpace(_config.PostgresPassword))
            {
                areCredentialsSupplied = true;
                connectionStringBuilder.Add("Password", _config.PostgresPassword);
            }
            if (!areCredentialsSupplied && !isRawConnectionStringSupplied)
            {
                connectionStringBuilder.Add("Integrated Security", "true");
            }

            var host = _config.PostgresHost;
            var port = _config.PostgresPort;
            if (!String.IsNullOrWhiteSpace(host))
            {
                // Grab any numbers after the last colon in the hostname
                var r = Regex.Match(host, @"(?<host>.*?)(:?(?<port>\d+))?$");
                if (r.Success)
                {
                    host = r.Groups["host"].Value;
                    port = r.Groups["port"].Value;
                }
            }
            connectionStringBuilder.Add("Host", host);
            _dbHost = host;

            if (String.IsNullOrWhiteSpace(port))
            {
                port = "5432";
            }

            connectionStringBuilder.Add("Port", port);
            _dbPort = port;

            if (!String.IsNullOrWhiteSpace(_config.PostgresDatabaseName))
            {
                connectionStringBuilder.Add("Database", _config.PostgresDatabaseName);
            }

            _dbCreds = new DataSourceCredentials(_config.PostgresUsername, _config.PostgresPassword) 
            { 
                EmbedInWorkbook = _config.PostgresEmbedCredentialsOnPublish 
            };

            if (_config.PublishWorkbooks && !areCredentialsSupplied)
            {
                throw new WorkbookPublishingException("Integrated Authentication to a postgres database is not compatible with the Embed Credentials on Publish option, credentials are required");
            }

            return connectionStringBuilder;
        }

        public async Task InitializeDatabase()
        {
            await _repository.CreateDatabaseIfNotExist();
            await InitializeLogSharkRunTable();
        }

        private async Task InitializeLogSharkRunTable()
        {
            _repository.RegisterType<LogSharkRunModel>(null);
            await _repository.CreateTableIfNotExist<LogSharkRunModel>();
            await _repository.CreateColumnWithPrimaryKeyIfNotExist<LogSharkRunModel>(LogSharkRunIdColumnName);
            await _repository.CreateColumnsForTypeIfNotExist<LogSharkRunModel>();
            _logSharkRunId = await _repository.InsertRowWithReturnValue<LogSharkRunModel, int>(new LogSharkRunModel()
            {
                LogSetLocation = _config.LogSetLocation,
                RunId = _runId,
                StartTimestamp = DateTime.UtcNow,
            }, LogSharkRunIdColumnName);
        }

        public IWriter<T> GetWriter<T>(DataSetInfo dataSetInfo)
        {
            _logger.LogDebug("Creating writer for {outputInfo}", dataSetInfo);
            var sqlWriter = new SqlWriter<T>(_repository, dataSetInfo, _loggerFactory.CreateLogger<SqlWriter<T>>());
            sqlWriter.InitializeTable(LogSharkRunIdColumnName, _logSharkRunId).Wait();
            return sqlWriter;
        }

        public void Dispose()
        {
            _repository.Dispose();
        }

        public IWorkbookGenerator GetWorkbookGenerator()
        {
            return new SqlWorkbookGenerator(
                _runId,
                _config,
                _repository.DatabaseName,
                _dbHost,
                _dbPort,
                _dbCreds.Username,
                _loggerFactory);
        }

        public IWorkbookPublisher GetWorkbookPublisher(PublisherSettings publisherSettings)
        {
            return new WorkbookPublisher(publisherSettings, _dbCreds, _loggerFactory);
        }
    }
}