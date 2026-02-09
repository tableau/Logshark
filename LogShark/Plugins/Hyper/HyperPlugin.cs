using System;
using System.Collections.Generic;
using System.Linq;
using LogShark.Containers;
using LogShark.Plugins.Hyper.Model;
using LogShark.Shared;
using LogShark.Shared.LogReading.Containers;
using LogShark.Writers;
using LogShark.Writers.Containers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LogShark.Plugins.Hyper
{
    public class HyperPlugin : IPlugin
    {
        private static readonly List<LogType> ConsumedLogTypesInternal = new List<LogType> { LogType.Hyper };
        public IList<LogType> ConsumedLogTypes => ConsumedLogTypesInternal;

        public string Name => "Hyper";

        private static readonly DataSetInfo HyperErrorOutputInfo = new DataSetInfo("Hyper", "HyperErrors");
        private static readonly DataSetInfo HyperQueryOutputInfo = new DataSetInfo("Hyper", "HyperQueries");

        private IWriter<HyperError> _hyperErrorWriter;
        private IWriter<HyperEvent> _hyperQueryWriter;
        private IProcessingNotificationsCollector _processingNotificationsCollector;

        private readonly HashSet<string> _eventTypes = new HashSet<string>() { "query-end", "query-end-cancelled", "query-end-canceled", "connection-startup-begin", "connection-startup-end", "cancel-request-received", "connection-close-request",
            "dbregistry-load", "dbregistry-release", "query-result-sent", "tcp-ip-client-allowed", "tcp-ip-client-rejected", "query-plan-slow", "query-plan-spooling", "query-plan-cancelled",
            "startup-info", "resource-stats", "log-rate-limit-reached", "asio-continuation-slow","query-running-info" };


        public void Configure(IWriterFactory writerFactory, IConfiguration pluginConfig, IProcessingNotificationsCollector processingNotificationsCollector, ILoggerFactory loggerFactory)
        {
            _hyperErrorWriter = writerFactory.GetWriter<HyperError>(HyperErrorOutputInfo);
            _hyperQueryWriter = writerFactory.GetWriter<HyperEvent>(HyperQueryOutputInfo);
            _processingNotificationsCollector = processingNotificationsCollector;
        }

        public void ProcessLogLine(LogLine logLine, LogType logType)
        {
            if (!(logLine.LineContents is NativeJsonLogsBaseEvent baseEvent))
            {
                var errorMessage = $"Was not able to cast line contents as {nameof(NativeJsonLogsBaseEvent)}";
                _processingNotificationsCollector.ReportError(errorMessage, logLine, nameof(HyperPlugin));
                return;
            }

            try
            {

                WriteHyperErrorIfMatch(logLine, baseEvent);
                WriteHyperQueryIfMatch(logLine, baseEvent);
            }
            catch (Exception)
            {
                _processingNotificationsCollector.ReportError("Failed to parse line", logLine, nameof(HyperPlugin));
            }
        }

        private void WriteHyperErrorIfMatch(LogLine logLine, NativeJsonLogsBaseEvent jsonEvent)
        {
            if (jsonEvent.Severity != "error" && jsonEvent.Severity != "fatal")
            {
                return;
            }
            
            var valueString = GetHyperErrorValueString(jsonEvent.EventPayload);
            if (valueString == null)
            {
                _processingNotificationsCollector.ReportError("Failed to parse event payload message", logLine, nameof(HyperPlugin));
                return;
            }
            
            var hyperError = new HyperError()
            {
                FileName = logLine.LogFileInfo.FileName,
                FilePath = logLine.LogFileInfo.FilePath,
                Key = jsonEvent.EventType,
                Line = logLine.LineNumber,
                ProcessId = jsonEvent.ProcessId,
                RequestId = jsonEvent.RequestId,
                SessionId = jsonEvent.SessionId,
                Severity = jsonEvent.Severity,
                Site = jsonEvent.Site,
                ThreadId = jsonEvent.ThreadId,
                Timestamp = jsonEvent.Timestamp,
                User = jsonEvent.Username,
                Value = valueString,
                Worker = logLine.LogFileInfo.Worker,
            };
                
            _hyperErrorWriter.AddLine(hyperError);
        }

        private static string GetHyperErrorValueString(JToken value)
        {
            if (value == null)
            {
                return null;
            }
            
            var valueStrings = value
                .ToObject<Dictionary<string, object>>()
                .Select(kvp => $"{kvp.Key}: {kvp.Value}");
            return string.Join(Environment.NewLine, valueStrings);
        }

        private void WriteHyperQueryIfMatch(LogLine logLine, NativeJsonLogsBaseEvent jsonEvent)
        {
            if (!_eventTypes.Contains(jsonEvent.EventType))
            {
                return;
            }

            var payload = jsonEvent.EventPayload;

            if (jsonEvent.EventType == "log-rate-limit-reached")
            {
                //sub key.. currently only capture events for this one known subkey
                if (payload["key"]?.ToString() != "number-network-threads-low")
                {
                    return;
                }
            }

            var hyperQuery = new HyperEvent()
            {
                FileName = logLine.LogFileInfo.FileName,
                FilePath = logLine.LogFileInfo.FilePath,
                Line = logLine.LineNumber,

                Key = jsonEvent.EventType,
                ProcessId = jsonEvent.ProcessId,
                RequestId = jsonEvent.RequestId,
                SessionId = jsonEvent.SessionId,
                Severity = jsonEvent.Severity,
                Site = jsonEvent.Site,
                ThreadId = jsonEvent.ThreadId,
                Timestamp = jsonEvent.Timestamp,
                User = jsonEvent.Username,
                Worker = logLine.LogFileInfo.Worker,

                // log-rate-limit-reached
                SubKey = payload["key"]?.ToString(),
                CurrentCount = payload["current-count"]?.ToObject<long>() ?? default(long?),
                RemainingIntervalSeconds = payload["remaining-interval-seconds"]?.ToObject<double>() ?? default(double?),

                // asio-continuation-slow
                Source = payload["source"]?.ToString(),

                // *-end, *-release
                Elapsed = payload["elapsed"]?.ToObject<double>()
                    ?? payload["current-elapsed"]?.ToObject<double>()
                    ?? default(double?),

                // query-end, query-end-cancelled, query-running-info

                ClientSessionId = jsonEvent.ContextMetrics?.ClientSessionId ?? payload["client-session-id"]?.ToString(),
                ClientRequestId = jsonEvent.ContextMetrics?.ClientRequestId,
               
               /* CopyDataSize = payload["copydata-size"]?.ToObject<long>() ?? default(long?),
                CopyDataTime = payload["copydata-time"]?.ToObject<double>() ?? default(double?),
                ExclusiveExecution = payload["ExclusiveExecution"]?.ToObject<bool>() ?? default(bool?),
                LockAcquisitionTime = payload["lock-acquisition-time"]?.ToObject<double>() ?? default(double?),
               PeakTransactionMemoryMb = payload["peak-transaction-memory-mb"]?.ToObject<double>() ?? default(double?),*/
                Columns = payload["cols"]?.ToObject<double>() ?? default(double?),
                PeakResultBufferMemoryMb = payload["peak-result-buffer-memory-mb"]?.ToObject<double>() ?? default(double?),
                PeakResultBufferDiskMb = payload["peak-result-buffer-Disk-mb"]?.ToObject<double>() ?? default(double?),
                CommitTime = payload["commit-time"]?.ToObject<double>() ?? default(double?),
                QuerySettingsActive = payload["query-settings-active"]?.ToObject<bool>() ?? default(bool?),
                QueryHash = payload["query-hash"]?.ToString(),
                
                PlanCacheHitCount = payload["plan-cache-hit-count"]?.ToObject<double>() ?? default(double?),
                PlanCacheStatus = payload["plan-cache-status"]?.ToString(),
               //pre-execution
                PreExecParsingTime = payload["pre-execution"]?["parsing-time"]?.ToObject<double>() ?? default(double),
                PreExecCompilationTime = payload["pre-execution"]?["compilation-time"]?.ToObject<double>() ?? default(double),
                PreExecElapsed = payload["pre-execution"]?["elapsed"]?.ToObject<double>() ?? default(double),
                PreExecWaitTimeDBLock = payload["pre-execution"]?["wait-time-database-lock"]?.ToObject<double>() ?? default(double),
                PreExecProcessedRows = payload["pre-execution"]?["processed-rows"]?.ToObject<double>() ?? default(double),
                PreExecProcessedRowsBYOL = payload["pre-execution"]?["processed-rows-byol"]?.ToObject<double>() ?? default(double),
                PreExecProcessedRowsFileBYOL = payload["pre-execution"]?["processed-rows-file-byol"]?.ToObject<double>() ?? default(double),
                PreExecResultSpoolingNumBuiltChunks = payload["pre-execution"]?["result-spooling-number-built-chunks"]?.ToObject<double>() ?? default(double),
                PreExecThreadTime = payload["pre-execution"]?["threads"]?["thread-time"]?.ToObject<double>() ?? default(double),
                PreExecThreadsCPUTime = payload["pre-execution"]?["threads"]?["cpu-time"]?.ToObject<double>() ?? default(double),
                PreExecThreadsWaitTime = payload["pre-execution"]?["threads"]?["wait-time"]?.ToObject<double>() ?? default(double),
                PreExecPeakTransactionMemMb = payload["pre-execution"]?["peak-transaction-memory-mb"]?.ToObject<double>() ?? default(double),
                //execution
                ExecElapsed = payload["execution"]?["elapsed"]?.ToObject<double>() ?? default(double),
                ExecWaitTimeObjLock = payload["execution"]?["wait-time-objects-lock"]?.ToObject<double>() ?? default(double),
                ExecyWaitTimeDBLock = payload["execution"]?["wait-time-database-lock"]?.ToObject<double>() ?? default(double),
                ExecProcessedRows = payload["execution"]?["processed-rows"]?.ToObject<double>() ?? default(double),
                ExecProcessedRowsBYOL = payload["execution"]?["processed-rows-byol"]?.ToObject<double>() ?? default(double),
                ExecProcessedRowsFileBYOL = payload["execution"]?["processed-rows-file-byol"]?.ToObject<double>() ?? default(double),
                ExecResultSpoolingNumBuiltChunks = payload["execution"]?["result-spooling-number-built-chunks"]?.ToObject<double>() ?? default(double),
                ExecThreadTime = payload["execution"]?["threads"]?["thread-time"]?.ToObject<double>() ?? default(double),
                ExecThreadsCpuTime = payload["execution"]?["threads"]?["cpu-time"]?.ToObject<double>() ?? default(double),
                ExecThreadsWaitTime = payload["execution"]?["threads"]?["wait-time"]?.ToObject<double>() ?? default(double),
                ExecThreadsWaitTimeBuffBackPressure = payload["execution"]?["threads"]?["wait-time-write-buffer-backpressure"]?.ToObject<double>() ?? default(double),
                ExecPeakTransactionMemMb = payload["execution"]?["peak-transaction-memory-mb"]?.ToObject<double>() ?? default(double),
                //adaptive-compilation
                ExecAdaptiveCompilationTime = payload["execution"]?["adaptive-compilation"]?["compilation-time"]?.ToObject<double>() ?? default(double),
                ExecAdaptiveCompilationOptExpected = payload["execution"]?["adaptive-compilation"]?["optimized"]?["expected"]?.ToObject<double>() ?? default(double),
                ExecAdaptiveCompilationOptActual = payload["execution"]?["adaptive-compilation"]?["optimized"]?["actual"]?.ToObject<double>() ?? default(double),
                




                QueryExecutionTime =
                    payload["execution-time"]?.ToObject<double>()
                    ?? payload["query-execution-time"]?.ToObject<double>()
                    ?? default(double?),

                QueryTrunc = payload["query-trunc"]?.ToString(),
                ResultSizeMb = payload["result-size-mb"]?.ToObject<double>() ?? default(double?),
                Rows = payload["rows"]?.ToObject<double>() ?? default(double?),
                Spooling = payload["spooling"]?.ToObject<bool>() ?? default(bool?),
                
                Statement = payload["statement"]?.ToString(),
                TimeToSchedule = payload["time-to-schedule"]?.ToObject<double>() ?? default(double?),
                TransactionId = payload["transaction-id"]?.ToString(),
                StatementId = payload["statement-id"]?.ToString(),
                /*TransactionVisibleId = payload["transaction-visible-id"]?.ToString(),
                ExecThreadsCpuTime = payload["exec-threads"]?["cpu-time"]?.ToObject<double>() ?? default(double?),
                ExecThreadsWaitTime = payload["exec-threads"]?["wait-time"]?.ToObject<double>() ?? default(double?),
               ExecThreadsTotalTime = payload["exec-threads"]?["total-time"]?.ToObject<double>() ?? default(double?),*/
                StorageAccessTime = payload["execution"]?["storage"]?["access-time"]?.ToObject<double>() ?? default(double?),
                StorageAccessCount = payload["execution"]?["storage"]?["access-count"]?.ToObject<long>() ?? default(long?),
                StorageAccessBytes = payload["execution"]?["storage"]?["access-bytes"]?.ToObject<long>() ?? default(long?),
                StorageWorkerBlockedTime = payload["execution"]?["storage"]?["worker-blocked-time"]?.ToObject<double>() ?? default(double?),
                StorageWorkerBlockedCount = payload["execution"]?["storage"]?["worker-blocked-count"]?.ToObject<long>() ?? default(long?),
                StorageCacheHitCount = payload["execution"]?["storage"]?["cache-hit-count"]?.ToObject<long>() ?? default(long?),


                StorageCacheHitBytes = payload["execution"]?["storage"]?["cache-hit-bytes"]?.ToObject<long>() ?? default(long?),
                StorageCacheBytesSaved = payload["execution"]?["storage"]?["cache-bytes-saved"]?.ToObject<long>() ?? default(long?),




                // connection-startup-begin
                DbUser = payload["db-user"]?.ToString(),
                Options = payload["options"]?.ToString(),

                // connection-startup-end
                ElapsedInterpretOptions = payload["elapsed-interpret-options"]?.ToObject<double>() ?? default(double?),
                ElapsedCheckUser = payload["elapsed-check-user"]?.ToObject<double>() ?? default(double?),
                ElapsedCheckAuthentication = payload["elapsed-check-authentication"]?.ToObject<double>() ?? default(double?),
                HaveCred = payload["have-cred"]?.ToObject<bool>() ?? default(bool?),
                CredName = payload["cred-name"]?.ToString(),

                // cancel-request-received
                Id = payload["id"]?.ToString(Formatting.None), // some fields have a json object as an id, others have an int, this is a stop gap for our current parsing routine

                Secret = payload["secret"]?.ToObject<long>() ?? default(long?),

                // connection-close-request
                Reason = payload["reason"]?.ToString(),

                // dbregistry-*
                NewRefCount = payload["new-ref-count"]?.ToObject<long>() ?? default(long?),
                Error = payload["error"]?.ToString(),

                // dbregistry-load
                CanonicalPath = payload["canonical-path"]?.ToString(),
                PathGiven = payload["path-given"]?.ToString(),
                ElapseRegistryInsert = payload["elapsed-registry-insert"]?.ToObject<double>() ?? default(double?),
                AlreadyLoaded = payload["already-loaded"]?.ToObject<bool>() ?? default(bool?),
                Reopen = payload["reopen"]?.ToObject<bool>() ?? default(bool?),
                LoadSuccess = payload["load-success"]?.ToObject<bool>() ?? default(bool?),
                DatabaseUuid = payload["database-uuid"]?.ToString(),

                // dbregistry-release
                Saved = payload["saved"]?.ToObject<bool>() ?? default(bool?),
                FailedOnLoad = payload["failed-on-load"]?.ToObject<bool>() ?? default(bool?),
                WasUnloaded = payload["was-unloaded"]?.ToObject<bool>() ?? default(bool?),
                WasDropped = payload["was-dropped"]?.ToObject<bool>() ?? default(bool?),
                ElapsedSave = payload["elapsed-save"]?.ToObject<double>() ?? default(double?),
                Closed = payload["closed"]?.ToObject<bool>() ?? default(bool?),
                ElapsedRegistryClose = payload["elapsed-registry-close"]?.ToObject<double>() ?? default(double?),

                // query-result-sent
                Success = payload["success"]?.ToObject<bool>() ?? default(bool?),
                TimeSinceQueryEnd = payload["time-since-query-end"]?.ToObject<double>() ?? default(double?),
                TransferredVolumeMb = payload["transferred-volume-mb"]?.ToObject<double>() ?? default(double?),

                // tcp-ip-client-allowed, tcp-ip-client-rejected
                RemoteAddress = payload["remote-address"]?.ToString(),

                // query-plan-slow, query-plan-spooling, query-plan-cancelled
                Plan = payload["plan"]?.ToString(),

                // startup-info
                CommandLine = payload["command-line"]?.ToString(),
                ServerVersion = payload["server-version"]?.ToString(),
                BuildVersion = payload["build-version"]?.ToString(),
                BuildType = payload["build-type"]?.ToString(),
                BuildCpuFeatures = payload["build-cpu-features"]?.ToString(),
                NetworkThreads = payload["network-threads"]?.ToObject<long>() ?? default(long?),
                ParentPid = payload["parent-pid"]?.ToObject<long>() ?? default(long?),
                MinProtocolVersion = payload["min-protocol-version"]?.ToObject<long>() ?? default(long?),
                MaxProtocolVersion = payload["max-protocol-version"]?.ToObject<long>() ?? default(long?),

                // resource-stats
                VirtualTotalMb = payload["memory"]?["virtual"]?["total-mb"]?.ToObject<long>() ?? default(long?),
                VirtualSystemMb = payload["memory"]?["virtual"]?["system-mb"]?.ToObject<long>() ?? default(long?),
                VirtualProcessMb = payload["memory"]?["virtual"]?["process-mb"]?.ToObject<long>() ?? default(long?),
                PhysicalTotalMb = payload["memory"]?["physical"]?["total-mb"]?.ToObject<long>() ?? default(long?),
                PhysicalSystemMb = payload["memory"]?["physical"]?["system-mb"]?.ToObject<long>() ?? default(long?),
                PhysicalProcessMb = payload["memory"]?["physical"]?["process-mb"]?.ToObject<long>() ?? default(long?),
                GlobalCurrentMb = payload["mem-tracker"]?["global"]?["current-mb"]?.ToObject<long>() ?? default(long?),
                GlobalPeakMb = payload["mem-tracker"]?["global"]?["peak-mb"]?.ToObject<long>() ?? default(long?),
                GlobalNetworkReadbufferCurrentMb = payload["mem-tracker"]?["global_network_readbuffer"]?["current-mb"]?.ToObject<long>() ?? default(long?),
                GlobalNetworkReadbufferPeakMb = payload["mem-tracker"]?["global_network_readbuffer"]?["peak-mb"]?.ToObject<long>() ?? default(long?),
                GlobalNetworkWriteBufferCurrentMb = payload["mem-tracker"]?["global_network_writebuffer"]?["current-mb"]?.ToObject<long>() ?? default(long?),
                GlobalNetworkWriteBufferPeakMb = payload["mem-tracker"]?["global_network_writebuffer"]?["peak-mb"]?.ToObject<long>() ?? default(long?),
                GlobalStringpoolCurrentMb = payload["mem-tracker"]?["global_stringpool"]?["current-mb"]?.ToObject<long>() ?? default(long?),
                GlobalStringpoolPeakMb = payload["mem-tracker"]?["global_stringpool"]?["peak-mb"]?.ToObject<long>() ?? default(long?),
                GlobalTransactionsCurrentMb = payload["mem-tracker"]?["global_transactions"]?["current-mb"]?.ToObject<long>() ?? default(long?),
                GlobalTransactionsPeakMb = payload["mem-tracker"]?["global_transactions"]?["peak-mb"]?.ToObject<long>() ?? default(long?),
                GlobalLockedCurrentMb = payload["mem-tracker"]?["global_locked"]?["current-mb"]?.ToObject<long>() ?? default(long?),
                GlobalLockedPeakMb = payload["mem-tracker"]?["global_locked"]?["peak-mb"]?.ToObject<long>() ?? default(long?),
                GlobalTupleDataCurrentMb = payload["mem-tracker"]?["global_tuple_data"]?["current-mb"]?.ToObject<long>() ?? default(long?),
                GlobalTupleDataPeakMb = payload["mem-tracker"]?["global_tuple_data"]?["peak-mb"]?.ToObject<long>() ?? default(long?),
                GlobalPlanCacheCurrentMb = payload["mem-tracker"]?["global_plan_cache"]?["current-mb"]?.ToObject<long>() ?? default(long?),
                GlobalPlanCachePeakMb = payload["mem-tracker"]?["global_plan_cache"]?["peak-mb"]?.ToObject<long>() ?? default(long?),
                GlobalExternalTableCacheCurrentMb = payload["mem-tracker"]?["global_external_table_cache"]?["current-mb"]?.ToObject<long>() ?? default(long?),
                GlobalExternalTableCachePeakMb = payload["mem-tracker"]?["global_external_table_cache"]?["peak-mb"]?.ToObject<long>() ?? default(long?),
                GlobalDiskNetworkReadbufferCurrentMb = payload["mem-tracker"]?["global_disk_network_readbuffer"]?["current-mb"]?.ToObject<long>() ?? default(long?),
                GlobalDiskNetworkReadbufferPeakMb = payload["mem-tracker"]?["global_disk_network_readbuffer"]?["peak-mb"]?.ToObject<long>() ?? default(long?),
                GlobalDiskNetworkWritebufferCurrentMb = payload["mem-tracker"]?["global_disk_network_writebuffer"]?["current-mb"]?.ToObject<long>() ?? default(long?),
                GlobalDiskNetworkWritebufferPeakMb = payload["mem-tracker"]?["global_disk_network_writebuffer"]?["peak-mb"]?.ToObject<long>() ?? default(long?),
                GlobalDiskStringpoolCurrentMb = payload["mem-tracker"]?["global_disk_stringpool"]?["current-mb"]?.ToObject<long>() ?? default(long?),
                GlobalDiskStringpoolPeakMb = payload["mem-tracker"]?["global_disk_stringpool"]?["peak-mb"]?.ToObject<long>() ?? default(long?),
                GlobalDiskTransactionCurrentMb = payload["mem-tracker"]?["global_disk_transaction"]?["current-mb"]?.ToObject<long>() ?? default(long?),
                GlobalDiskTransactionPeakMb = payload["mem-tracker"]?["global_disk_transaction"]?["peak-mb"]?.ToObject<long>() ?? default(long?),
            };

            _hyperQueryWriter.AddLine(hyperQuery);
        }

        public SinglePluginExecutionResults CompleteProcessing()
        {
            var writersLineCounts = new List<WriterLineCounts>
            {
                _hyperErrorWriter.Close(),
                _hyperQueryWriter.Close()
            };
            return new SinglePluginExecutionResults(writersLineCounts);
        }

        public void Dispose()
        {
            _hyperErrorWriter?.Dispose();
            _hyperQueryWriter?.Dispose();
        }
    }
}
