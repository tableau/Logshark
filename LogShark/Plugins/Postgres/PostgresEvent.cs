using CsvHelper.Configuration.Attributes;
using System;

namespace LogShark.Plugins.Postgres
{
    public class PostgresEvent
    {
        public DateTime Timestamp { get; set; }

        public int ProcessId { get; set; }

        public string Severity { get; set; }

        public string Message { get; set; }

        public int? Duration { get; set; }

        public string Worker { get; set; }

        public string FilePath { get; set; }

        public string File { get; set; }

        public int LineNumber { get; set; }

        public string CommandTag { get; set; }
        public string Client { get; set; }
        public string ApplicationName { get; set; }
        public string Username { get; set; }

        public string UserQueryLedToError { get; set; }
    }
}
