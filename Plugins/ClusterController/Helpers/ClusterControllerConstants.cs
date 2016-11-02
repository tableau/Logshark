namespace Logshark.Plugins.ClusterController.Helpers
{
    public class ClusterControllerConstants
    {
        public const string POSTGRES_START_AS_MASTER = "Starting Postgres on the current node as master";
        public const string POSTGRES_FAILOVER_AS_MASTER = "Failing over Postgres on this node to become master";
        public const string POSTGRES_START_AS_SLAVE = "Starting Postgres on this node as slave";
        public const string POSTGRES_STOP = "PostgresManager stop";
        public const string POSTGRES_RESTART = "PostgresManager restart";

        public const string DISK_IO_MONITOR_CLASS = "com.tableausoftware.cluster.storage.DiskIOMonitor";
        public const string DISK_IO_MONITOR_MESSAGE_PREFIX = "disk I/O 1min avg > ";
        public const string FILETXNLOG_CLASS = "org.apache.zookeeper.server.persistence.FileTxnLog";
        public const string POSTGRES_MANAGER_CLASS = "com.tableausoftware.cluster.postgres.PostgresManager";
    }
}
