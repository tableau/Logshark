using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Collections.Generic;
using System.ComponentModel;

namespace Logshark.Plugins.Hyper.Models
{
    public class HyperEndQueryDetails : ISupportInitialize
    {
        [BsonElement("client-session-id")]
        public string ClientSessionId { get; set; }

        [BsonElement("cols")]
        [BsonRepresentation(BsonType.String)]
        public ulong Columns { get; set; }

        [BsonElement("elapsed")]
        public double Elapsed { get; set; }

        [BsonElement("exclusive-execution")]
        public bool ExclusiveExecution { get; set; }

        [BsonElement("lock-acquisition-time")]
        public double LockAcquisitionTime { get; set; }

        [BsonElement("peak-result-buffer-memory-mb")]
        public double PeakResultBufferMemoryMb { get; set; }

        [BsonElement("peak-transaction-memory-mb")]
        public double PeakTransactionMemoryMb { get; set; }

        [BsonElement("plan-cache-hit-count")]
        [BsonRepresentation(BsonType.String)]
        public ulong PlanCacheHitCount { get; set; }

        [BsonElement("plan-cache-status")]
        public string PlanCacheStatus { get; set; }

        [BsonElement("compilation-time")]
        public double QueryCompilationTime { get; set; }

        [BsonElement("execution-time")]
        public double QueryExecutionTime { get; set; }

        [BsonElement("parsing-time")]
        public double QueryParsingTime { get; set; }

        [BsonElement("query-trunc")]
        public string QueryTrunc { get; set; }

        [BsonElement("result-size-mb")]
        public double ResultSizeMb { get; set; }

        [BsonElement("rows")]
        [BsonRepresentation(BsonType.String)]
        public ulong Rows { get; set; }

        [BsonElement("spooling")]
        public bool Spooling { get; set; }

        [BsonElement("statement-id")]
        public string StatementId { get; set; }

        [BsonElement("time-to-schedule")]
        public double TimeToSchedule { get; set; }

        [BsonElement("transaction-id")]
        public string TransactionId { get; set; }

        [BsonElement("transaction-visible-id")]
        public string TransactionVisibleId { get; set; }

        [BsonExtraElements]
        public IDictionary<string, object> ExtraElements { get; set; }

        #region ISupportInitialize Implementation

        public void BeginInit() { }

        public void EndInit()
        {
            // Resolve schema changes
            if (ExtraElements != null)
            {
                if (ExtraElements.ContainsKey("query-compilation-time") && ExtraElements["query-compilation-time"] is double)
                {
                    QueryCompilationTime = (double) ExtraElements["query-compilation-time"];
                }
                if (ExtraElements.ContainsKey("query-execution-time") && ExtraElements["query-execution-time"] is double)
                {
                    QueryExecutionTime = (double) ExtraElements["query-execution-time"];
                }
                if (ExtraElements.ContainsKey("query-parsing-time") && ExtraElements["query-parsing-time"] is double)
                {
                    QueryParsingTime = (double) ExtraElements["query-parsing-time"];
                }
            }
        }

        #endregion ISupportInitialize Implementation
    }
}