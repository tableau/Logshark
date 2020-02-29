using CsvHelper.Configuration.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace LogShark.Plugins.Postgres
{
    public class PostgresCsvMapping
    {
        [Index(0)]
        public DateTime Timestamp { get; set; }

        [Index(1)]
        public string Username { get; set; }

        [Index(2)]
        public string DatabaseName { get; set; }

        [Index(3)]
        public int Pid { get; set; }

        [Index(4)]
        public string Client { get; set; }

        [Index(5)]
        public string SessionId { get; set; }

        [Index(6)]
        public string PerSessionLineNumber { get; set; }

        [Index(7)]
        public string CommandTag { get; set; }

        [Index(8)]
        public string SessionStartTime { get; set; }

        [Index(9)]
        public string VirtualTransactionId { get; set; }

        [Index(10)]
        public string RegularTransactionId { get; set; }

        [Index(11)]
        public string Sev { get; set; }

        [Index(12)]
        public string SqlstateCode { get; set; }

        [Index(13)]
        public string Message { get; set; }

        [Index(14)]
        public string MessageDetail { get; set; }

        [Index(15)]
        public string Hint { get; set; }

        [Index(16)]
        public string InternalQueryLedToError { get; set; }

        [Index(17)]
        public string InternalQueryErrorPosition { get; set; }

        [Index(18)]
        public string ErrorContext { get; set; }

        [Index(19)]
        public string UserQueryLedToError { get; set; }

        [Index(20)]
        public string UserQueryErrorPosition { get; set; }

        [Index(21)]
        public string ErrorLocationInPostgresSource { get; set; }

        [Index(22)]
        public string ApplicationName { get; set; }
    }
}
