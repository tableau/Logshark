using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;

namespace Logshark.Plugins.ClusterController.Helpers
{
    internal class Queries
    {
        private static readonly FilterDefinitionBuilder<BsonDocument> Query = Builders<BsonDocument>.Filter;

        public static FilterDefinition<BsonDocument> ClusterControllerByFile(IMongoCollection<BsonDocument> collection)
        {
            return Query.Regex("file", new BsonRegularExpression("clustercontroller.*"));
        }

        public static FilterDefinition<BsonDocument> GetFsyncLatencyEvents()
        {
            return Query.And(Query.Eq("class", ClusterControllerConstants.FILETXNLOG_CLASS),
                             Query.Regex("message", new BsonRegularExpression("^fsync.*")));
        }

        public static FilterDefinition<BsonDocument> GetErrorEvents()
        {
            return Query.Or(Query.Eq("sev", "ERROR"),
                            Query.Eq("sev", "FATAL"));
        }

        public static FilterDefinition<BsonDocument> GetPostgresActions()
        {
            var postgresActionList = new List<string>
            {
                ClusterControllerConstants.POSTGRES_START_AS_MASTER,
                ClusterControllerConstants.POSTGRES_START_AS_SLAVE,
                ClusterControllerConstants.POSTGRES_FAILOVER_AS_MASTER,
                ClusterControllerConstants.POSTGRES_STOP,
                ClusterControllerConstants.POSTGRES_RESTART
            };

            return Query.And(Query.Eq("class", ClusterControllerConstants.POSTGRES_MANAGER_CLASS),
                             Query.In("message", postgresActionList));
        }

        public static FilterDefinition<BsonDocument> GetDiskIoSamples()
        {
            return Query.And(Query.Eq("class", ClusterControllerConstants.DISK_IO_MONITOR_CLASS),
                             Query.Eq("sev", "INFO"),
                             Query.Regex("message", new BsonRegularExpression(String.Format("^{0}.*", ClusterControllerConstants.DISK_IO_MONITOR_MESSAGE_PREFIX))));
        }
    }
}