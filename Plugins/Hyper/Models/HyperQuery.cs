using MongoDB.Bson.Serialization.Attributes;
using Tableau.ExtractApi.DataAttributes;

namespace Logshark.Plugins.Hyper.Models
{
    [BsonIgnoreExtraElements]
    public class HyperQuery : BaseHyperEvent
    {
        [BsonElement("v")]
        [ExtractIgnore]
        public HyperEndQueryDetails Value { get; set; }

        // Unfortunately we have to duplicate all of the fields here to map to the nested representation,
        // as MongoDB requires the model to be hierarchical for deserialization, whilst OrmLite requires the
        // model to be flat for persistence. Ugh!

        // Also, Postgres has no native unsigned integer types, so we have to coerce the underlying ulong properties 
        // on the model to decimal so that they are properly persisted as Postgres type "numeric".

        public string ClientSessionId { get { return Value.ClientSessionId; } }

        public decimal Columns { get { return Value.Columns; } }
        public double Elapsed { get { return Value.Elapsed; } }
        public bool ExclusiveExecution { get { return Value.ExclusiveExecution; } }
        public double LockAcquisitionTime { get { return Value.LockAcquisitionTime; } }
        public double PeakResultBufferMemoryMb { get { return Value.PeakResultBufferMemoryMb; } }
        public double PeakTransactionMemoryMb { get { return Value.PeakTransactionMemoryMb; } }
        public decimal PlanCacheHitCount { get { return Value.PlanCacheHitCount; } }
        public string PlanCacheStatus { get { return Value.PlanCacheStatus; } }
        public double QueryCompilationTime { get { return Value.QueryCompilationTime; } }
        public double QueryExecutionTime { get { return Value.QueryExecutionTime; } }
        public double QueryParsingTime { get { return Value.QueryParsingTime; } }
        public string QueryTrunc { get { return Value.QueryTrunc; } }
        public double ResultSizeMb { get { return Value.ResultSizeMb; } }
        public decimal Rows { get { return Value.Rows; } }
        public bool Spooling { get { return Value.Spooling; } }
        public string StatementId { get { return Value.StatementId; } }
        public double TimeToSchedule { get { return Value.TimeToSchedule; } }
        public string TransactionId { get { return Value.TransactionId; } }
        public string TransactionVisibleId { get { return Value.TransactionVisibleId; } }
    }
}