using log4net;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Linq;
using System.Reflection;

namespace Logshark.ConnectionModel.Helpers
{
    /// <summary>
    /// Helper class for executing admin functions against a Mongo database.
    /// </summary>
    public static class MongoAdminHelper
    {
        private const string AdminDatabaseName = "admin";
        private const string ConfigDatabaseName = "config";
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        #region Public Methods

        /// <summary>
        /// Indicates whether the given database exists and has data.
        /// </summary>
        /// <param name="client">The Mongo connection client.</param>
        /// <param name="databaseName">The name of the database to check for existence of.</param>
        /// <returns>True if given database name exists and contains data.</returns>
        public static bool DatabaseExists(IMongoClient client, string databaseName)
        {
            return client.GetDatabase(databaseName).ListCollections().ToList().Count > 0;
        }

        /// <summary>
        /// Drops a given database from the server.
        /// </summary>
        /// <param name="client">The Mongo connection client.</param>
        /// <param name="databaseName">The name of the database to drop.</param>
        public static void DropDatabase(IMongoClient client, string databaseName)
        {
            Log.DebugFormat("Dropping database '{0}'..", databaseName);
            client.DropDatabase(databaseName);
            if (DatabaseExists(client, databaseName))
            {
                throw new MongoException(String.Format("Failed to drop database '{0}'!  Issued drop database command but data still exists.", databaseName));
            }
        }

        /// <summary>
        /// Enables sharding on a Mongo database, if it isn't already enabled.
        /// </summary>
        /// <param name="client">The Mongo connection client.</param>
        /// <param name="databaseName">The name of the database to enable sharding on.</param>
        public static void EnableShardingOnDatabaseIfNotEnabled(IMongoClient client, string databaseName)
        {
            if (databaseName.Equals(AdminDatabaseName, StringComparison.InvariantCultureIgnoreCase) ||
                databaseName.Equals(ConfigDatabaseName, StringComparison.InvariantCultureIgnoreCase))
            {
                return;
            }

            bool isSharded;
            try
            {
                isSharded = IsShardingEnabled(client, databaseName);
            }
            catch (MongoException ex)
            {
                Log.ErrorFormat("Error querying sharding status of database '{0}': {1}", databaseName, ex.Message);
                throw;
            }

            // If this database is already sharded, we're done.
            if (isSharded)
            {
                return;
            }

            Log.DebugFormat("Enabling sharding on database '{0}'..", databaseName);
            EnableShardingOnDatabase(client, databaseName);
        }

        /// <summary>
        /// Enables sharding on a Mongo collection, if it isn't already enabled.
        /// </summary>
        /// <param name="client">The Mongo connection client.</param>
        /// <param name="databaseName">The name of the database to enable sharding on.</param>
        /// <param name="collectionName">The name of the database to enable sharding on.</param>
        public static void EnableShardingOnCollectionIfNotEnabled(IMongoClient client, string databaseName, string collectionName)
        {
            IMongoCollection<BsonDocument> collection = client.GetDatabase(databaseName).GetCollection<BsonDocument>(collectionName);

            bool collectionIsSharded = IsShardingEnabled(client, collection);
            if (!collectionIsSharded)
            {
                Log.DebugFormat("Enabling sharding on collection '{0}'..", collection.CollectionNamespace.FullName);
                EnableShardingOnCollection(client, collection);
            }
        }

        /// <summary>
        /// Indicates whether the server the user is currently connected to is a cluster.
        /// </summary>
        /// <param name="client">The Mongo connection client.</param>
        /// <returns>True if client is connect to a mongos instance.</returns>
        public static bool IsMongoCluster(IMongoClient client)
        {
            IMongoDatabase adminDatabase = GetAdminDatabase(client);
            var command = @"{ isMaster : 1 }";
            BsonDocument status = ExecuteCommand(adminDatabase, command);

            if (!status.Contains("msg"))
            {
                return false;
            }
            return status["msg"].AsString.Equals("isdbgrid", StringComparison.InvariantCultureIgnoreCase);
        }

        /// <summary>
        /// Indicates whether a given database on the server has had sharding enabled.
        /// </summary>
        /// <param name="client">The Mongo connection client.</param>
        /// <param name="databaseName">The name of the database to check.</param>
        /// <returns>True if the given database has sharding enabled.</returns>
        public static bool IsShardingEnabled(IMongoClient client, string databaseName)
        {
            try
            {
                IMongoDatabase configDatabase = GetConfigDatabase(client);
                IMongoCollection<BsonDocument> configDatabasesCollection = configDatabase.GetCollection<BsonDocument>("databases");

                var filter = Builders<BsonDocument>.Filter.Eq("_id", databaseName);
                var queryResult = configDatabasesCollection.Find(filter).ToList();

                if (!queryResult.Any())
                {
                    return false;
                }

                BsonDocument resultDocument = queryResult.First();
                bool isShardingEnabled = resultDocument.GetValue("partitioned", BsonValue.Create(false)).AsBoolean;

                return isShardingEnabled;
            }
            catch (Exception ex)
            {
                Log.ErrorFormat("Failed to query sharding status of database '{0}': {1}", databaseName, ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Indicates whether a given collection on the server has had sharding enabled.
        /// </summary>
        /// <param name="client">The Mongo connection client.</param>
        /// <param name="collection">The collection to check.</param>
        /// <returns>True if the given collection has sharding enabled.</returns>
        public static bool IsShardingEnabled(IMongoClient client, IMongoCollection<BsonDocument> collection)
        {
            try
            {
                IMongoDatabase configDatabase = GetConfigDatabase(client);
                IMongoCollection<BsonDocument> configDatabasesCollection = configDatabase.GetCollection<BsonDocument>("collections");

                var filter = Builders<BsonDocument>.Filter.Eq("_id", collection.CollectionNamespace.FullName);
                var queryResult = configDatabasesCollection.Find(filter).ToList();

                if (!queryResult.Any())
                {
                    return false;
                }

                BsonDocument resultDocument = queryResult.First();
                if (!resultDocument.Contains("dropped"))
                {
                    return false;
                }

                return resultDocument.GetValue("dropped", BsonValue.Create(false)).AsBoolean.Equals(false);
            }
            catch (Exception ex)
            {
                Log.ErrorFormat("Failed to query sharding status of collection '{0}': {1}", collection.CollectionNamespace.CollectionName, ex.Message);
                throw;
            }
        }

        #endregion Public Methods

        #region Private Methods

        private static void EnableShardingOnDatabase(IMongoClient client, string databaseName)
        {
            try
            {
                IMongoDatabase adminDatabase = GetAdminDatabase(client);

                var command = new BsonDocument("enableSharding", databaseName);
                ExecuteCommand(adminDatabase, command);
            }
            catch (MongoException ex)
            {
                Log.ErrorFormat("Error enabling sharding on database '{0}': {1}", databaseName, ex.Message);
            }
        }

        private static void EnableShardingOnCollection(IMongoClient client, IMongoCollection<BsonDocument> collection)
        {
            try
            {
                IMongoDatabase adminDatabase = GetAdminDatabase(client);

                var command = String.Format(@"{{ shardCollection: ""{0}"", key: {{ _id: ""hashed"" }} }}", collection.CollectionNamespace.FullName);
                ExecuteCommand(adminDatabase, command);
            }
            catch (MongoException ex)
            {
                Log.ErrorFormat("Error enabling sharding on collection '{0}': {1}", collection.CollectionNamespace.CollectionName, ex.Message);
            }
        }

        private static BsonDocument ExecuteCommand(IMongoDatabase database, BsonDocument command)
        {
            return database.RunCommand<BsonDocument>(command);
        }

        private static BsonDocument ExecuteCommand(IMongoDatabase database, string command)
        {
            return database.RunCommand<BsonDocument>(command);
        }

        private static IMongoDatabase GetAdminDatabase(IMongoClient client)
        {
            return client.GetDatabase(AdminDatabaseName);
        }

        private static IMongoDatabase GetConfigDatabase(IMongoClient client)
        {
            return client.GetDatabase(ConfigDatabaseName);
        }

        #endregion Private Methods
    }
}