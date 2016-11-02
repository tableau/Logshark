using log4net;
using Logshark.Config;
using Logshark.Exceptions;
using Npgsql;
using ServiceStack.OrmLite;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Logshark.Connections
{
    public class PostgresConnectionInfo
    {
        // Npgsql Command Timeout, in seconds.
        private const int CommandTimeoutSeconds = 60;

        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        protected IDictionary<string, OrmLiteConnectionFactory> connectionFactories;

        // Connection endpoint options.
        public string Hostname { get; set; }

        public int Port { get; set; }
        public string DefaultDatabase { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }

        public PostgresConnectionInfo(PostgresConnection postgresConfig)
        {
            connectionFactories = new Dictionary<string, OrmLiteConnectionFactory>();

            Hostname = postgresConfig.Server.Server;
            Port = postgresConfig.Server.Port;
            DefaultDatabase = postgresConfig.Database.Name;
            Username = postgresConfig.User.Username;
            Password = postgresConfig.User.Password;
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
                CommandTimeout = CommandTimeoutSeconds
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
                    var checkIfDbExistsText = String.Format("SELECT 1 FROM pg_database WHERE datname='{0}'", databaseName);
                    Log.DebugFormat("Querying if Postgres database '{0}' exists..", databaseName);
                    using (var cmd = new NpgsqlCommand(checkIfDbExistsText, connection))
                    {
                        dbExists = cmd.ExecuteScalar() != null;
                    }

                    // If database doesn't exist, create it.
                    if (!dbExists)
                    {
                        Log.DebugFormat("Postgres database '{0}' does not exist. Creating it..", databaseName);
                        var createDbText = String.Format("CREATE DATABASE \"{0}\" " +
                                                         "WITH OWNER = \"{1}\" " +
                                                         "ENCODING = 'UTF8' " +
                                                         "CONNECTION LIMIT = -1;", databaseName, Username);

                        using (var cmd = new NpgsqlCommand(createDbText, connection))
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
                finally
                {
                    connection.Close();
                }
            }
        }

        #endregion Protected Methods
    }
}