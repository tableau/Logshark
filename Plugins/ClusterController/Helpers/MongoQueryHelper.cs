using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;

namespace Logshark.Plugins.ClusterController.Helpers
{
    internal class MongoQueryHelper
    {
        private static readonly FilterDefinitionBuilder<BsonDocument> Query = Builders<BsonDocument>.Filter;

        public static FilterDefinition<BsonDocument> ClusterControllerByFile(IMongoCollection<BsonDocument> collection)
        {
            return Query.Regex("file", new BsonRegularExpression("clustercontroller.*"));
        }

        public static ProjectionDefinition<BsonDocument> IgnoreUnusedClusterControllerFieldsProjection()
        {
            // Include all fields except for the following.
            return Builders<BsonDocument>.Projection.Exclude("file_path");
        }

        public static IAsyncCursor<BsonDocument> GetFsyncLatencyEventsCursor(IMongoCollection<BsonDocument> collection)
        {
            var filter = Query.And(
                    Query.Eq("class", ClusterControllerConstants.FILETXNLOG_CLASS),
                    Query.Regex("message", new BsonRegularExpression("^fsync.*")));
            return collection.Find(filter).ToCursor();
        }

        public static IAsyncCursor<BsonDocument> GetErrorEventsCursor(IMongoCollection<BsonDocument> collection)
        {
            var filter = Query.Or(
                            Query.Eq("sev", "ERROR"),
                            Query.Eq("sev", "FATAL"));
            return collection.Find(filter).ToCursor();
        }

        public static IAsyncCursor<BsonDocument> GetPostgresActions(IMongoCollection<BsonDocument> collection)
        {
            var postgresActionList = new List<string>
            {
                ClusterControllerConstants.POSTGRES_START_AS_MASTER,
                ClusterControllerConstants.POSTGRES_START_AS_SLAVE,
                ClusterControllerConstants.POSTGRES_FAILOVER_AS_MASTER,
                ClusterControllerConstants.POSTGRES_STOP,
                ClusterControllerConstants.POSTGRES_RESTART
            };

            var filter = Query.And(
                            Query.Eq("class", ClusterControllerConstants.POSTGRES_MANAGER_CLASS),
                            Query.In("message", postgresActionList));

            return collection.Find(filter).ToCursor();
        }

        public static IAsyncCursor<BsonDocument> GetDiskIoSamplesCursor(IMongoCollection<BsonDocument> collection)
        {
            var filter = Query.And(
                            Query.Eq("class", ClusterControllerConstants.DISK_IO_MONITOR_CLASS),
                            Query.Eq("sev", "INFO"),
                            Query.Regex("message", new BsonRegularExpression(String.Format("^{0}.*", ClusterControllerConstants.DISK_IO_MONITOR_MESSAGE_PREFIX))));

            return collection.Find(filter).ToCursor();
        }
    }
}