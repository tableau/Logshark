namespace Logshark.PluginLib
{
    public static class PluginLibConstants
    {
        // The Postgres error code for a failed insertion due to a duplicate key value violating a unique constraint.
        public static readonly string POSTGRES_ERROR_CODE_UNIQUE_VIOLATION = "23505";

        // The Postgres error code for a failed batch insert due to a deadlock occurring.
        public static readonly string POSTGRES_ERROR_CODE_DEADLOCK_DETECTED = "40P01";

        //Default pool size when spinning up Postgres persisters.
        public const int DEFAULT_PERSISTER_POOL_SIZE = 10;

        //Default batch size when spinning up Postgres persisters.
        public const int DEFAULT_PERSISTER_MAX_BATCH_SIZE = 100;

        public const int DEFAULT_PROGRESS_MONITOR_POLLING_INTERVAL_SECONDS = 20;

        public const string DEFAULT_PERSISTER_STATUS_WRITER_PROGRESS_MESSAGE = "{ItemsPersisted} {PersistedType} items have been persisted to the database.";

        public const string DEFAULT_TASK_STATUS_WRITER_PROGRESS_MESSAGE = "{TasksCompleted} {TaskType} tasks have been completed.";

        public const string DEFAULT_PERSISTER_STATUS_WRITER_PROGRESS_MESSAGE_WITH_TOTAL = "{ItemsPersisted}/{ItemsExpected} {PersistedType} items have been persisted to the database. [{PercentComplete}]";

        public const string DEFAULT_TASK_STATUS_WRITER_PROGRESS_MESSAGE_WITH_TOTAL = "{TasksCompleted}/{TotalTasks} {TaskType} tasks have been completed. [{PercentComplete}]";
    }
}