using System;
using System.Collections.Generic;
using System.Text;

namespace LogShark.Plugins.Hyper.Model
{
    public class HyperEvent
    {
        // Log File Shared
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public int Line { get; set; }

        // Log Line Shared
        public string Key { get; set; }
        public int ProcessId { get; set; }
        public string RequestId { get; set; }
        public string SessionId { get; set; }
        public string Severity { get; set; }
        public string Site { get; set; }
        public string ThreadId { get; set; }
        public DateTime Timestamp { get; set; }
        public string User { get; set; }
        public string Worker { get; set; }

        // *-end, *-release
        public double? Elapsed { get; set; }

        // query-end, query-end-cancelled
        public string ClientSessionId { get; set; }
        public double? Columns { get; set; }
        public bool? ExclusiveExecution { get; set; }
        public double? LockAcquisitionTime { get; set; }
        public double? PeakResultBufferMemoryMb { get; set; }
        public double? PeakTransactionMemoryMb { get; set; }
        public double? PlanCacheHitCount { get; set; }
        public string PlanCacheStatus { get; set; }
        public double? QueryCompilationTime { get; set; }
        public double? QueryExecutionTime { get; set; }
        public double? QueryParsingTime { get; set; }
        public string QueryTrunc { get; set; }
        public double? ResultSizeMb { get; set; }
        public double? Rows { get; set; }
        public bool? Spooling { get; set; }
        public string StatementId { get; set; }
        public double? TimeToSchedule { get; set; }
        public string TransactionId { get; set; }
        public string TransactionVisibleId { get; set; }
        public double? CopyDataTime { get; set; }
        public int? CopyDataSize { get; set; }
        public double? ExecThreadsCpuTime { get; set; }
        public double? ExecThreadsWaitTime { get; set; }
        public double? ExecThreadsTotalTime { get; set; }
        public double? StorageAccessTime { get; set; }
        public int? StorageAccessCount { get; set; }
        public long? StorageAccessBytes { get; set; }
        public double? StorageWriteTime { get; set; }
        public int? StorageWriteCount { get; set; }
        public long? StorageWriteBytes { get; set; }

        // connection-startup-begin
        public string DbUser { get; set; }
        public string Options { get; set; }

        // connection-startup-end
        public double? ElapsedInterpretOptions { get; set; }
        public double? ElapsedCheckUser { get; set; }
        public double? ElapsedCheckAuthentication { get; set; }
        public bool? HaveCred { get; set; }
        public string CredName { get; set; }

        // cancel-request-received
        public int? Id { get; set; } 
        public long? Secret { get; set; } 

        // connection-close-request
        public string Reason { get; set; }

        // dbregistry-*
        public int? NewRefCount { get; set; }
        public string Error { get; set; }

        // dbregistry-load
        public string CanonicalPath { get; set; }
        public string PathGiven { get; set; }
        public double? ElapseRegistryInsert { get; set; }
        public bool? AlreadyLoaded { get; set; }
        public bool? Reopen { get; set; }
        public bool? LoadSuccess { get; set; }
        public string DatabaseUuid { get; set; }

        // dbregistry-release
        public bool? Saved { get; set; }
        public bool? FailedOnLoad { get; set; }
        public bool? WasUnloaded{ get; set; }
        public bool? WasDropped { get; set; }
        public double? ElapsedSave { get; set; }
        public bool? Closed { get; set; }
        public double? ElapsedRegistryClose { get; set; }

        // query-result-sent
        public bool? Success { get; set; }
        public double? TimeSinceQueryEnd { get; set; }
        public double? TransferredVolumeMb { get; set; }

        // tcp-ip-client-allowed, tcp-ip-client-rejected
        public string RemoteAddress { get; set; }

        // query-plan-slow, query-plan-spooling, query-plan-cancelled
        public string Plan { get; set; }

        // startup-info
        public string CommandLine { get; set; }
        public string ServerVersion { get; set; }
        public string BuildVersion { get; set; }
        public string BuildType { get; set; }
        public string BuildCpuFeatures { get; set; }
        public int? NetworkThreads { get; set; }
        public int? ParentPid { get; set; }
        public int? MinProtocolVersion { get; set; }
        public int? MaxProtocolVersion { get; set; }

        // resource-stats
        public long? VirtualTotalMb { get; set; }
        public long? VirtualSystemMb { get; set; }
        public long? VirtualProcessMb { get; set; }
        public long? PhysicalTotalMb { get; set; }
        public long? PhysicalSystemMb { get; set; }
        public long? PhysicalProcessMb { get; set; }
        public long? GlobalCurrentMb { get; set; }
        public long? GlobalPeakMb { get; set; }
        public long? GlobalNetworkReadbufferCurrentMb { get; set; }
        public long? GlobalNetworkReadbufferPeakMb { get; set; }
        public long? GlobalNetworkWriteBufferCurrentMb { get; set; }
        public long? GlobalNetworkWriteBufferPeakMb { get; set; }
        public long? GlobalStringpoolCurrentMb { get; set; }
        public long? GlobalStringpoolPeakMb { get; set; }
        public long? GlobalTransactionsCurrentMb { get; set; }
        public long? GlobalTransactionsPeakMb { get; set; }
        public long? GlobalLockedCurrentMb { get; set; }
        public long? GlobalLockedPeakMb { get; set; }
        public long? GlobalTupleDataCurrentMb { get; set; }
        public long? GlobalTupleDataPeakMb { get; set; }
        public long? GlobalPlanCacheCurrentMb { get; set; }
        public long? GlobalPlanCachePeakMb { get; set; }
        public long? GlobalExternalTableCacheCurrentMb { get; set; }
        public long? GlobalExternalTableCachePeakMb { get; set; }
        public long? GlobalDiskNetworkReadbufferCurrentMb { get; set; }
        public long? GlobalDiskNetworkReadbufferPeakMb { get; set; }
        public long? GlobalDiskNetworkWritebufferCurrentMb { get; set; }
        public long? GlobalDiskNetworkWritebufferPeakMb { get; set; }
        public long? GlobalDiskStringpoolCurrentMb { get; set; }
        public long? GlobalDiskStringpoolPeakMb { get; set; }
        public long? GlobalDiskTransactionCurrentMb { get; set; }
        public long? GlobalDiskTransactionPeakMb { get; set; }
    }
}
