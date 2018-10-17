using log4net;
using Logshark.Config;
using Logshark.ConnectionModel.Exceptions;
using Npgsql;
using ServiceStack.OrmLite;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Logshark.ConnectionModel.Postgres
{
    public class PostgresConnectionInfo
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        protected IDictionary<string, OrmLiteConnectionFactory> connectionFactories;

        // Connection endpoint options.
        public string Hostname { get; set; }

        public int Port { get; set; }
        public string DefaultDatabase { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }

        // Performance tuning options
        public int CommandTimeoutSeconds { get; set; }
        public int TcpKeepAliveSeconds { get; set; }
        public int WriteBufferSizeBytes { get; set; }

        public PostgresConnectionInfo(PostgresConnection postgresConfig)
        {
            connectionFactories = new Dictionary<string, OrmLiteConnectionFactory>();

            Hostname = postgresConfig.Server.Server;
            Port = postgresConfig.Server.Port;
            DefaultDatabase = postgresConfig.Database.Name;
            Username = postgresConfig.User.Username;
            Password = postgresConfig.User.Password;

            CommandTimeoutSeconds = postgresConfig.CommandTimeout;
            TcpKeepAliveSeconds = postgresConfig.TcpKeepalive;
            WriteBufferSizeBytes = postgresConfig.WriteBufferSize;
        }

        #region Public Methods

        /// <summary>
        /// Creates the factory object we can use to create an ORM connection to Postgres.
        /// </summary>
        /// <param name="databaseName">The name of the database instance to connect to.</param>
        /// <returns>OrmLiteConnectionFactory object that can be used for ORM operations against Postgres.</returns>
        public OrmLiteConnectionFactory GetConnectionFactory(string databaseName)
        {
            if (!connectionFactories.ContainsKey(databaseName))
            {
                CreateDatabaseIfNotExists(databaseName);
                string connectionString = GetConnectionString(databaseName);
                connectionFactories.Add(databaseName, new OrmLiteConnectionFactory(connectionString, true, PostgreSqlDialect.Provider));
            }

            return connectionFactories[databaseName];
        }

        public override string ToString()
        {
            return String.Format(@"{0}:{1}", Hostname, Port);
        }

        #endregion Public Methods

        #region Protected Methods

        /// <summary>
        /// Retrieves the connection string for the Postgres database.
        /// </summary>
        /// <param name="databaseName">The name of the database instance to connect to.</param>
        /// <returns>Connection string to database.</returns>
        protected string GetConnectionString(string databaseName)
        {
            var connectionStringBuilder = new NpgsqlConnectionStringBuilder
            {
                Host = Hostname,
                Port = Port,
                Database = databaseName,
                Username = Username,
                Password = Password,
                CommandTimeout = CommandTimeoutSeconds,
                TcpKeepAliveTime = TcpKeepAliveSeconds * 1000,
                WriteBufferSize = WriteBufferSizeBytes
            };

            return connectionStringBuilder.ToString();
        }

        protected NpgsqlConnection OpenConnection(string databaseName)
        {
            var connectionString = GetConnectionString(databaseName);

            Log.DebugFormat(@"Attempting to open connection to Postgres database '{0}\{1}' using user account '{2}'..", this, databaseName, Username);
            var connection = new NpgsqlConnection(connectionString);
            try
            {
                connection.Open();
                Log.DebugFormat(@"Successfully connected to {0}\{1}", this, databaseName);
                return connection;
            }
            catch (NpgsqlException ex)
            {
                throw new DatabaseInitializationException(String.Format("Failed to open connection to {0}: {1}", this, ex.Message), ex);
            }
        }

        protected void CreateDatabaseIfNotExists(string databaseName)
        {
            using (NpgsqlConnection connection = OpenConnection(DefaultDatabase))
            {
                // Ensure target database exists.
                try
                {
                    bool dbExists;

                    Log.DebugFormat("Querying if Postgres database '{0}' exists..", databaseName);
                    var checkIfDbExistsQuery = "SELECT 1 FROM pg_database WHERE datname = @database_name";
                    using (var cmd = new NpgsqlCommand(checkIfDbExistsQuery, connection))
                    {
                        cmd.Parameters.AddWithValue("database_name", databaseName);
                        dbExists = cmd.ExecuteScalar() != null;
                    }

                    // If database doesn't exist, create it.
                    if (!dbExists)
                    {
                        Log.DebugFormat("Postgres database '{0}' does not exist. Creating it..", databaseName);

                        // Postgres does not allow parameterized DDL for creating DBs, so we have to build this one by hand.
                        var createDbQuery = String.Format(@"CREATE DATABASE ""{0}"" OWNER ""{1}"" ENCODING 'UTF8' CONNECTION LIMIT -1", databaseName, Username);

                        using (var cmd = new NpgsqlCommand(createDbQuery, connection))
                        {
                            cmd.ExecuteNonQuery();
                        }

                        Log.InfoFormat("Created Postgres database '{0}' on '{1}'.", databaseName, this);
                    }
                }
                catch (NpgsqlException ex)
                {
                    throw new DatabaseInitializationException(String.Format("Failed to create database '{0}': {1}", databaseName, ex.Message), ex);
                }
            }
        }

        #endregion Protected Methods
    }
}