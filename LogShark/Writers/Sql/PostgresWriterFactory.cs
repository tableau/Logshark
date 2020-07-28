using System.Data.Common;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using LogShark.Containers;
using LogShark.Exceptions;
using LogShark.Writers.Containers;
using LogShark.Writers.Sql.Connections;
using LogShark.Writers.Sql.Connections.Npgsql;
using Microsoft.Extensions.Logging;
using static Tools.TableauServerRestApi.Containers.PublishWorkbookRequest;

namespace LogShark.Writers.Sql
{
    public class PostgresWriterFactory<TRunSummary> : IWriterFactory
    {
        private readonly string _runId;
        private readonly LogSharkConfiguration _config;
        private DataSourceCredentials _dbCreds;
        private string _dbHost;
        private string _dbPort;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger _logger;
        private int _runSummaryId;

        private readonly ISqlRepository _repository;
        private readonly TRunSummary _runSummaryRecord;
        private readonly string _runSummaryIdColumnName;

        public PostgresWriterFactory(
            string runId,
            LogSharkConfiguration config,
            ILoggerFactory loggerFactory,
            TRunSummary runSummaryRecord,
            string runSummaryIdColumnName)
        {
            _runId = runId;
            _config = config;
            _loggerFactory = loggerFactory;
            _logger = _loggerFactory.CreateLogger<PostgresWriterFactory<TRunSummary>>();

            _runSummaryRecord = runSummaryRecord;
            _runSummaryIdColumnName = runSummaryIdColumnName;

            var connectionStringBuilder = GetConnectionStringBuilder();
            _repository = new NpgsqlRepository(new NpgsqlDataContext(connectionStringBuilder, _config.PostgresServiceDatabaseName, loggerFactory), _config.PostgresBatchSize);
            _repository.RegisterType<TRunSummary>(null);
        }

        private DbConnectionStringBuilder GetConnectionStringBuilder()
        {
            var connectionStringBuilder = new DbConnectionStringBuilder();
            var isRawConnectionStringSupplied = !string.IsNullOrWhiteSpace(_config.PostgresConnectionString);
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
            if (!string.IsNullOrWhiteSpace(_config.PostgresUsername))
            {
                areCredentialsSupplied = true;
                connectionStringBuilder.Add("User Id", _config.PostgresUsername);
            }
            if (!string.IsNullOrWhiteSpace(_config.PostgresPassword))
            {
                areCredentialsSupplied = true;
                connectionStringBuilder.Add("Password", _config.PostgresPassword);
            }
            if (!areCredentialsSupplied && !isRawConnectionStringSupplied)
            {
                connectionStringBuilder.Add("Integrated Security", "true");
            }
            string host = _config.PostgresHost;
            if (string.IsNullOrEmpty(host))
            {
                if (connectionStringBuilder.ContainsKey("Host"))
                {
                    host = (string)connectionStringBuilder["Host"];
                } else
                {
                    _logger.LogError("Error building connection string: No host was specified.");
                }
            }

            var port = _config.PostgresPort;
            if (!string.IsNullOrWhiteSpace(host))
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

            if (string.IsNullOrWhiteSpace(port))
            {
                port = "5432";
            }

            connectionStringBuilder.Add("Port", port);
            _dbPort = port;

            if (!string.IsNullOrWhiteSpace(_config.PostgresDatabaseName))
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
            if (!_config.PostgresSkipDatabaseVerificationAndInitialization)
            {
                await _repository.CreateDatabaseIfNotExist();
                await InitializeRunSummaryTable(_runSummaryIdColumnName);
            }

            await WriteRunSummary();
        }

        private async Task InitializeRunSummaryTable(string runSummaryIdColumnName)
        {
            await _repository.CreateTableIfNotExist<TRunSummary>();
            await _repository.CreateColumnWithPrimaryKeyIfNotExist<TRunSummary>(runSummaryIdColumnName);
            await _repository.CreateColumnsForTypeIfNotExist<TRunSummary>();
        }

        private async Task WriteRunSummary()
        {
            _runSummaryId = await _repository.InsertRowWithReturnValue<TRunSummary, int>(_runSummaryRecord, _runSummaryIdColumnName, null, new [] { _runSummaryIdColumnName });
        }

        public IWriter<T> GetWriter<T>(DataSetInfo dataSetInfo)
        {
            _logger.LogDebug("Creating writer for {outputInfo}", dataSetInfo);
            var sqlWriter = new SqlWriter<T>(_repository, dataSetInfo, _loggerFactory.CreateLogger<SqlWriter<T>>());
            sqlWriter.InitializeTable(_runSummaryIdColumnName, _runSummaryId, _config.PostgresSkipDatabaseVerificationAndInitialization).Wait();

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