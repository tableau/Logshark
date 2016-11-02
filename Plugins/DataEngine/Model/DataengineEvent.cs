using System;
using ServiceStack.DataAnnotations;

namespace Logshark.Plugins.DataEngine.Model
{
    internal class DataengineEvent
    {
        [PrimaryKey]
        [AutoIncrement]
        public int Id { get; set; }

        public Guid LogsetHash { get; set; }

        [Index(Unique=true)]
        public Guid EventHash { get; set; }

        [Index]
        public int SessionGuid { get; set; }

        public int ThreadId { get; set; }
        public DateTime Timestamp { get; set; }
        public string EventType { get; set; }
        public string Query { get; set; }
        public int? Columns { get; set; }
        public long? MemoryBudget { get; set; }
        public double? ElapsedTime { get; set; }
        public double? CompilationTime { get; set; }
        public double? ExecutionTime { get; set; }
        public int Worker { get; set; }
        public bool Success { get; set; }
        public string Error { get; set; }
        public int LineNumber { get; set; }
    }
}