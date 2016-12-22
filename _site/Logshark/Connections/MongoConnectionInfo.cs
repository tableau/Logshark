using log4net;
using Logshark.Config;
using Logshark.Helpers;
using Logshark.Mongo;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Logshark.Connections
{
    public enum MongoConnectionType { Undetermined, SingleNode, ShardedCluster }

    public class MongoConnectionInfo
    {
        // Connection endpoint options.
        public ICollection<MongoServerAddress> Servers { get; protected set; }

        public string Username { get; protected set; }
        public string Password { get; protected set; }

        // Connection management options.
        public int PoolSize { get; protected set; }

        public int Timeout { get; protected set; }
        public int InsertionRetries { get; protected set; }
        public MongoConnectionType ConnectionType { get; protected set; }

        // We only ever want to have a single instance to avoid having multiple connection pools.
        protected static MongoClient client;

        private static readonly int WaitQueueMultiplier = 10;

        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public MongoConnectionInfo(MongoConnection mongoConfig)
        {
            Servers = new List<MongoServerAddress>();
            foreach (MongoServer mongoServer in mongoConfig.Servers)
            {
                Servers.Add(new MongoServerAddress(mongoServer.Server, mongoServer.Port));
            }
            Username = mongoConfig.User.Username;
            Password = mongoConfig.User.Password;
            PoolSize = mongoConfig.PoolSize;
            Timeout = mongoConfig.Timeout;
            InsertionRetries = mongoConfig.InsertionRetries;
            ConnectionType = MongoConnectionType.Undetermined;
        }

        public MongoConnectionInfo(ICollection<MongoServerAddress> servers, string username, string password, int poolSize, int timeout, int insertionRetries)
        {
            Servers = servers;
            Username = username;
            Password = password;
            PoolSize = poolSize;
            Timeout = timeout;
            InsertionRetries = insertionRetries;
            ConnectionType = MongoConnectionType.Undetermined;
        }

        #region Public Methods

        public IMongoClient GetClient()
        {
            if (client == null)
            {
                MongoUrl mongoUrl = BuildMongoUrl();
                try
                {
                    client = new MongoClient(mongoUrl);
                    Log.DebugFormat("Created MongoDB client for {0}", this);
                    ConnectionType = GetConnectionType();
                }
                catch (TimeoutException ex)
                {
                    throw MongoExceptionHelper.GetMongoException(ex);
                }
                catch (MongoException ex)
                {
                    throw MongoExceptionHelper.GetMongoException(ex);
                }
            }

            return client;
        }

        public IMongoDatabase GetDatabase(string name)
        {
            var mongoClient = GetClient();
            IMongoDatabase database = mongoClient.GetDatabase(name);

            // If we are using a connection to a sharded database, we need to explicitly enable sharding on the db.
            if (ConnectionType == MongoConnectionType.ShardedCluster)
            {
                MongoAdminUtil.EnableShardingOnDatabaseIfNotEnabled(mongoClient, name);
            }

            return database;
        }

        public override string ToString()
        {
            var serverStringBuilder = new StringBuilder();
            foreach (var mongoServerAddress in Servers)
            {
                serverStringBuilder.Append(String.Concat(mongoServerAddress.Host, ":", mongoServerAddress.Port, " "));
            }
            string serverString = serverStringBuilder.ToString();

            var optionsString = String.Format("PoolSize='{0}', Timeout='{1}', InsertionRetries='{2}', ConnectionType='{3}'", PoolSize, Timeout, InsertionRetries, ConnectionType);
            return String.Format(@"{0}[{1}]", serverString, optionsString);
        }

        #endregion Public Methods

        #region Protected Methods

        protected MongoConnectionType GetConnectionType()
        {
            bool isCluster;
            try
            {
                isCluster = MongoAdminUtil.IsMongoCluster(client);
            }
            catch (MongoException ex)
            {
                Log.DebugFormat("Unable to determine if current MongoDB connection is a sharded cluster: {0}", ex.Message);
                return MongoConnectionType.Undetermined;
            }

            if (isCluster)
            {
                return MongoConnectionType.ShardedCluster;
            }

            return MongoConnectionType.SingleNode;
        }

        protected MongoUrl BuildMongoUrl()
        {
            var urlBuilder = new MongoUrlBuilder
            {
                ConnectionMode = ConnectionMode.Automatic,
                ConnectTimeout = TimeSpan.FromSeconds(Timeout),
                MaxConnectionPoolSize = PoolSize,
                ReadPreference = ReadPreference.PrimaryPreferred,
                WaitQueueMultiple = WaitQueueMultiplier
            };

            if (Servers.Count == 1)
            {
                urlBuilder.Server = Servers.First();
            }
            else
            {
                urlBuilder.Servers = Servers;
            }

            // Set username & password only if username is specified.
            if (!String.IsNullOrWhiteSpace(Username))
            {
                urlBuilder.Username = Username;
                urlBuilder.Password = Password;
            }

            return urlBuilder.ToMongoUrl();
        }

        protected void LogConnectionException(MongoException ex)
        {
            if (ex.InnerException is TimeoutException)
            {
                Log.DebugFormat("Encountered System.TimeoutException: {0}", ex.InnerException.Message);
            }

            Log.ErrorFormat("Unable to create client connection to MongoDB: {0}", ex.Message);
        }

        #endregion Protected Methods
    }
}