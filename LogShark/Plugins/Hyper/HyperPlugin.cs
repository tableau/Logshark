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


        public void Configure(IPluginWriterProvider writerProvider, IConfiguration pluginConfig, IProcessingNotificationsCollector processingNotificationsCollector, ILoggerFactory loggerFactory)
        {
            _hyperErrorWriter = writerProvider.GetWriter<HyperError>(HyperErrorOutputInfo);
            _hyperQueryWriter = writerProvider.GetWriter<HyperEvent>(HyperQueryOutputInfo);
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
                if (payload.GetSafe("key")?.ToString() != "number-network-threads-low")
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
                SubKey = payload.GetSafe("key")?.ToString(),
                CurrentCount = payload.GetSafe("current-count")?.ToLongSafe(),
                RemainingIntervalSeconds = payload.GetSafe("remaining-interval-seconds")?.ToDoubleSafe(),

                // asio-continuation-slow
                Source = payload.GetSafe("source")?.ToString(),

                // *-end, *-release
                Elapsed = payload.GetSafe("elapsed")?.ToDoubleSafe()
                    ?? payload.GetSafe("current-elapsed")?.ToDoubleSafe(),

                // query-end, query-end-cancelled, query-running-info

                ClientSessionId = jsonEvent.ContextMetrics?.ClientSessionId ?? payload.GetSafe("client-session-id")?.ToString(),
                ClientRequestId = jsonEvent.ContextMetrics?.ClientRequestId,

                Columns = payload.GetSafe("cols")?.ToDoubleSafe(),
                PeakResultBufferMemoryMb = payload.GetSafe("peak-result-buffer-memory-mb")?.ToDoubleSafe(),
                PeakResultBufferDiskMb = payload.GetSafe("peak-result-buffer-Disk-mb")?.ToDoubleSafe(),
                CommitTime = payload.GetSafe("commit-time")?.ToDoubleSafe(),
                QuerySettingsActive = payload.GetSafe("query-settings-active")?.ToBoolSafe(),
                QueryHash = payload.GetSafe("query-hash")?.ToString(),

                PlanCacheHitCount = payload.GetSafe("plan-cache-hit-count")?.ToDoubleSafe(),
                PlanCacheStatus = payload.GetSafe("plan-cache-status")?.ToString(),
                //pre-execution
                PreExecParsingTime = payload.GetSafe("pre-execution")?.GetSafe("parsing-time")?.ToDoubleSafe(),
                PreExecCompilationTime = payload.GetSafe("pre-execution")?.GetSafe("compilation-time")?.ToDoubleSafe(),
                PreExecElapsed = payload.GetSafe("pre-execution")?.GetSafe("elapsed")?.ToDoubleSafe(),
                PreExecWaitTimeDBLock = payload.GetSafe("pre-execution")?.GetSafe("wait-time-database-lock")?.ToDoubleSafe(),

                PreExecProcessedRowsTotal = payload.GetSafe("pre-execution")?.GetSafe("processed-rows")?.GetSafe("total")?.ToDoubleSafe(),
                PreExecProcessedRowsNative = payload.GetSafe("pre-execution")?.GetSafe("processed-rows")?.GetSafe("native")?.ToDoubleSafe(),
                PreExecProcessedRowsBYOL = payload.GetSafe("pre-execution")?.GetSafe("processed-rows-byol")?.ToDoubleSafe(),
                PreExecProcessedRowsFileBYOL = payload.GetSafe("pre-execution")?.GetSafe("processed-rows-file-byol")?.ToDoubleSafe(),
                PreExecResultSpoolingNumBuiltChunks = payload.GetSafe("pre-execution")?.GetSafe("result-spooling-number-built-chunks")?.ToDoubleSafe(),
                PreExecThreadTime = payload.GetSafe("pre-execution")?.GetSafe("threads")?.GetSafe("thread-time")?.ToDoubleSafe(),
                PreExecThreadsCPUTime = payload.GetSafe("pre-execution")?.GetSafe("threads")?.GetSafe("cpu-time")?.ToDoubleSafe(),
                PreExecThreadsWaitTime = payload.GetSafe("pre-execution")?.GetSafe("threads")?.GetSafe("wait-time")?.ToDoubleSafe(),
                PreExecPeakTransactionMemMb = payload.GetSafe("pre-execution")?.GetSafe("peak-transaction-memory-mb")?.ToDoubleSafe(),
                //execution
                ExecElapsed = payload.GetSafe("execution")?.GetSafe("elapsed")?.ToDoubleSafe(),
                ExecWaitTimeObjLock = payload.GetSafe("execution")?.GetSafe("wait-time-objects-lock")?.ToDoubleSafe(),
                ExecyWaitTimeDBLock = payload.GetSafe("execution")?.GetSafe("wait-time-database-lock")?.ToDoubleSafe(),
                ExecProcessedRowsTotal = payload.GetSafe("execution")?.GetSafe("processed-rows")?.GetSafe("total")?.ToDoubleSafe(),
                ExecProcessedRowsNative = payload.GetSafe("execution")?.GetSafe("processed-rows")?.GetSafe("native")?.ToDoubleSafe(),
                ExecProcessedRowsBYOL = payload.GetSafe("execution")?.GetSafe("processed-rows-byol")?.ToDoubleSafe(),
                ExecProcessedRowsFileBYOL = payload.GetSafe("execution")?.GetSafe("processed-rows-file-byol")?.ToDoubleSafe(),
                ExecResultSpoolingNumBuiltChunks = payload.GetSafe("execution")?.GetSafe("result-spooling-number-built-chunks")?.ToDoubleSafe(),
                ExecThreadTime = payload.GetSafe("execution")?.GetSafe("threads")?.GetSafe("thread-time")?.ToDoubleSafe(),
                ExecThreadsCpuTime = payload.GetSafe("execution")?.GetSafe("threads")?.GetSafe("cpu-time")?.ToDoubleSafe(),
                ExecThreadsWaitTime = payload.GetSafe("execution")?.GetSafe("threads")?.GetSafe("wait-time")?.ToDoubleSafe(),
                ExecThreadsWaitTimeBuffBackPressure = payload.GetSafe("execution")?.GetSafe("threads")?.GetSafe("wait-time-write-buffer-backpressure")?.ToDoubleSafe(),
                ExecPeakTransactionMemMb = payload.GetSafe("execution")?.GetSafe("peak-transaction-memory-mb")?.ToDoubleSafe(),
                //adaptive-compilation
                ExecAdaptiveCompilationTime = payload.GetSafe("execution")?.GetSafe("adaptive-compilation")?.GetSafe("compilation-time")?.ToDoubleSafe(),
                ExecAdaptiveCompilationOptExpected = payload.GetSafe("execution")?.GetSafe("adaptive-compilation")?.GetSafe("optimized")?.GetSafe("expected")?.ToDoubleSafe(),
                ExecAdaptiveCompilationOptActual = payload.GetSafe("execution")?.GetSafe("adaptive-compilation")?.GetSafe("optimized")?.GetSafe("actual")?.ToDoubleSafe(),





                QueryExecutionTime =
                    payload.GetSafe("execution-time")?.ToDoubleSafe()
                    ?? payload.GetSafe("query-execution-time")?.ToDoubleSafe()
                    ?? default(double?),

                QueryTrunc = payload.GetSafe("query-trunc")?.ToString(),
                ResultSizeMb = payload.GetSafe("result-size-mb")?.ToDoubleSafe(),
                Rows = payload.GetSafe("rows")?.ToDoubleSafe(),
                Spooling = payload.GetSafe("spooling")?.ToBoolSafe(),

                Statement = payload.GetSafe("statement")?.ToString(),
                TimeToSchedule = payload.GetSafe("time-to-schedule")?.ToDoubleSafe(),
                TransactionId = payload.GetSafe("transaction-id")?.ToString(),
                StatementId = payload.GetSafe("statement-id")?.ToString(),

                StorageAccessTime = payload.GetSafe("execution")?.GetSafe("storage")?.GetSafe("access-time")?.ToDoubleSafe(),
                StorageAccessCount = payload.GetSafe("execution")?.GetSafe("storage")?.GetSafe("access-count")?.ToLongSafe(),
                StorageAccessBytes = payload.GetSafe("execution")?.GetSafe("storage")?.GetSafe("access-bytes")?.ToLongSafe(),
                StorageWorkerBlockedTime = payload.GetSafe("execution")?.GetSafe("storage")?.GetSafe("worker-blocked-time")?.ToDoubleSafe(),
                StorageWorkerBlockedCount = payload.GetSafe("execution")?.GetSafe("storage")?.GetSafe("worker-blocked-count")?.ToLongSafe(),
                StorageCacheHitCount = payload.GetSafe("execution")?.GetSafe("storage")?.GetSafe("cache-hit-count")?.ToLongSafe(),


                StorageCacheHitBytes = payload.GetSafe("execution")?.GetSafe("storage")?.GetSafe("cache-hit-bytes")?.ToLongSafe(),
                StorageCacheBytesSaved = payload.GetSafe("execution")?.GetSafe("storage")?.GetSafe("cache-bytes-saved")?.ToLongSafe(),




                // connection-startup-begin
                DbUser = payload.GetSafe("db-user")?.ToString(),
                Options = payload.GetSafe("options")?.ToString(),

                // connection-startup-end
                ElapsedInterpretOptions = payload.GetSafe("elapsed-interpret-options")?.ToDoubleSafe(),
                ElapsedCheckUser = payload.GetSafe("elapsed-check-user")?.ToDoubleSafe(),
                ElapsedCheckAuthentication = payload.GetSafe("elapsed-check-authentication")?.ToDoubleSafe(),
                HaveCred = payload.GetSafe("have-cred")?.ToBoolSafe(),
                CredName = payload.GetSafe("cred-name")?.ToString(),

                // cancel-request-received
                Id = payload.GetSafe("id")?.ToString(Formatting.None), // some fields have a json object as an id, others have an int, this is a stop gap for our current parsing routine

                Secret = payload.GetSafe("secret")?.ToLongSafe(),

                // connection-close-request
                Reason = payload.GetSafe("reason")?.ToString(),

                // dbregistry-*
                NewRefCount = payload.GetSafe("new-ref-count")?.ToLongSafe(),
                Error = payload.GetSafe("error")?.ToString(),

                // dbregistry-load
                CanonicalPath = payload.GetSafe("canonical-path")?.ToString(),
                PathGiven = payload.GetSafe("path-given")?.ToString(),
                ElapseRegistryInsert = payload.GetSafe("elapsed-registry-insert")?.ToDoubleSafe(),
                AlreadyLoaded = payload.GetSafe("already-loaded")?.ToBoolSafe(),
                Reopen = payload.GetSafe("reopen")?.ToBoolSafe(),
                LoadSuccess = payload.GetSafe("load-success")?.ToBoolSafe(),
                DatabaseUuid = payload.GetSafe("database-uuid")?.ToString(),

                // dbregistry-release
                Saved = payload.GetSafe("saved")?.ToBoolSafe(),
                FailedOnLoad = payload.GetSafe("failed-on-load")?.ToBoolSafe(),
                WasUnloaded = payload.GetSafe("was-unloaded")?.ToBoolSafe(),
                WasDropped = payload.GetSafe("was-dropped")?.ToBoolSafe(),
                ElapsedSave = payload.GetSafe("elapsed-save")?.ToDoubleSafe(),
                Closed = payload.GetSafe("closed")?.ToBoolSafe(),
                ElapsedRegistryClose = payload.GetSafe("elapsed-registry-close")?.ToDoubleSafe(),

                // query-result-sent
                Success = payload.GetSafe("success")?.ToBoolSafe(),
                TimeSinceQueryEnd = payload.GetSafe("time-since-query-end")?.ToDoubleSafe(),
                TransferredVolumeMb = payload.GetSafe("transferred-volume-mb")?.ToDoubleSafe(),

                // tcp-ip-client-allowed, tcp-ip-client-rejected
                RemoteAddress = payload.GetSafe("remote-address")?.ToString(),

                // query-plan-slow, query-plan-spooling, query-plan-cancelled
                Plan = payload.GetSafe("plan")?.ToString(),

                // startup-info
                CommandLine = payload.GetSafe("command-line")?.ToString(),
                ServerVersion = payload.GetSafe("server-version")?.ToString(),
                BuildVersion = payload.GetSafe("build-version")?.ToString(),
                BuildType = payload.GetSafe("build-type")?.ToString(),
                BuildCpuFeatures = payload.GetSafe("build-cpu-features")?.ToString(),
                NetworkThreads = payload.GetSafe("network-threads")?.ToLongSafe(),
                ParentPid = payload.GetSafe("parent-pid")?.ToLongSafe(),
                MinProtocolVersion = payload.GetSafe("min-protocol-version")?.ToLongSafe(),
                MaxProtocolVersion = payload.GetSafe("max-protocol-version")?.ToLongSafe(),

                // resource-stats
                VirtualTotalMb = payload.GetSafe("memory")?.GetSafe("virtual")?.GetSafe("total-mb")?.ToLongSafe(),
                VirtualSystemMb = payload.GetSafe("memory")?.GetSafe("virtual")?.GetSafe("system-mb")?.ToLongSafe(),
                VirtualProcessMb = payload.GetSafe("memory")?.GetSafe("virtual")?.GetSafe("process-mb")?.ToLongSafe(),
                PhysicalTotalMb = payload.GetSafe("memory")?.GetSafe("physical")?.GetSafe("total-mb")?.ToLongSafe(),
                PhysicalSystemMb = payload.GetSafe("memory")?.GetSafe("physical")?.GetSafe("system-mb")?.ToLongSafe(),
                PhysicalProcessMb = payload.GetSafe("memory")?.GetSafe("physical")?.GetSafe("process-mb")?.ToLongSafe(),
                GlobalCurrentMb = payload.GetSafe("mem-tracker")?.GetSafe("global")?.GetSafe("current-mb")?.ToLongSafe(),
                GlobalPeakMb = payload.GetSafe("mem-tracker")?.GetSafe("global")?.GetSafe("peak-mb")?.ToLongSafe(),
                GlobalNetworkReadbufferCurrentMb = payload.GetSafe("mem-tracker")?.GetSafe("global_network_readbuffer")?.GetSafe("current-mb")?.ToLongSafe(),
                GlobalNetworkReadbufferPeakMb = payload.GetSafe("mem-tracker")?.GetSafe("global_network_readbuffer")?.GetSafe("peak-mb")?.ToLongSafe(),
                GlobalNetworkWriteBufferCurrentMb = payload.GetSafe("mem-tracker")?.GetSafe("global_network_writebuffer")?.GetSafe("current-mb")?.ToLongSafe(),
                GlobalNetworkWriteBufferPeakMb = payload.GetSafe("mem-tracker")?.GetSafe("global_network_writebuffer")?.GetSafe("peak-mb")?.ToLongSafe(),
                GlobalStringpoolCurrentMb = payload.GetSafe("mem-tracker")?.GetSafe("global_stringpool")?.GetSafe("current-mb")?.ToLongSafe(),
                GlobalStringpoolPeakMb = payload.GetSafe("mem-tracker")?.GetSafe("global_stringpool")?.GetSafe("peak-mb")?.ToLongSafe(),
                GlobalTransactionsCurrentMb = payload.GetSafe("mem-tracker")?.GetSafe("global_transactions")?.GetSafe("current-mb")?.ToLongSafe(),
                GlobalTransactionsPeakMb = payload.GetSafe("mem-tracker")?.GetSafe("global_transactions")?.GetSafe("peak-mb")?.ToLongSafe(),
                GlobalLockedCurrentMb = payload.GetSafe("mem-tracker")?.GetSafe("global_locked")?.GetSafe("current-mb")?.ToLongSafe(),
                GlobalLockedPeakMb = payload.GetSafe("mem-tracker")?.GetSafe("global_locked")?.GetSafe("peak-mb")?.ToLongSafe(),
                GlobalTupleDataCurrentMb = payload.GetSafe("mem-tracker")?.GetSafe("global_tuple_data")?.GetSafe("current-mb")?.ToLongSafe(),
                GlobalTupleDataPeakMb = payload.GetSafe("mem-tracker")?.GetSafe("global_tuple_data")?.GetSafe("peak-mb")?.ToLongSafe(),
                GlobalPlanCacheCurrentMb = payload.GetSafe("mem-tracker")?.GetSafe("global_plan_cache")?.GetSafe("current-mb")?.ToLongSafe(),
                GlobalPlanCachePeakMb = payload.GetSafe("mem-tracker")?.GetSafe("global_plan_cache")?.GetSafe("peak-mb")?.ToLongSafe(),
                GlobalExternalTableCacheCurrentMb = payload.GetSafe("mem-tracker")?.GetSafe("global_external_table_cache")?.GetSafe("current-mb")?.ToLongSafe(),
                GlobalExternalTableCachePeakMb = payload.GetSafe("mem-tracker")?.GetSafe("global_external_table_cache")?.GetSafe("peak-mb")?.ToLongSafe(),
                GlobalDiskNetworkReadbufferCurrentMb = payload.GetSafe("mem-tracker")?.GetSafe("global_disk_network_readbuffer")?.GetSafe("current-mb")?.ToLongSafe(),
                GlobalDiskNetworkReadbufferPeakMb = payload.GetSafe("mem-tracker")?.GetSafe("global_disk_network_readbuffer")?.GetSafe("peak-mb")?.ToLongSafe(),
                GlobalDiskNetworkWritebufferCurrentMb = payload.GetSafe("mem-tracker")?.GetSafe("global_disk_network_writebuffer")?.GetSafe("current-mb")?.ToLongSafe(),
                GlobalDiskNetworkWritebufferPeakMb = payload.GetSafe("mem-tracker")?.GetSafe("global_disk_network_writebuffer")?.GetSafe("peak-mb")?.ToLongSafe(),
                GlobalDiskStringpoolCurrentMb = payload.GetSafe("mem-tracker")?.GetSafe("global_disk_stringpool")?.GetSafe("current-mb")?.ToLongSafe(),
                GlobalDiskStringpoolPeakMb = payload.GetSafe("mem-tracker")?.GetSafe("global_disk_stringpool")?.GetSafe("peak-mb")?.ToLongSafe(),
                GlobalDiskTransactionCurrentMb = payload.GetSafe("mem-tracker")?.GetSafe("global_disk_transaction")?.GetSafe("current-mb")?.ToLongSafe(),
                GlobalDiskTransactionPeakMb = payload.GetSafe("mem-tracker")?.GetSafe("global_disk_transaction")?.GetSafe("peak-mb")?.ToLongSafe(),
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
    public static class JsonExtensions
    {
        // For doubles
        public static double ToDoubleSafe(this JToken? token) =>
            double.TryParse(token?.ToString(), out var r) ? r : 0;

        // For longs
        public static long ToLongSafe(this JToken? token) =>
            long.TryParse(token?.ToString(), out var r) ? r : 0;
        //For bools
        public static bool ToBoolSafe(this JToken? token) =>
           bool.TryParse(token?.ToString(), out var r) ? r : false;
        public static JToken? GetSafe(this JToken? token, string key)
        {
            // If it's not an object (JObject), it can't have children/keys.
            if (token is JObject obj)
            {
                return obj[key];
            }
            return null; // Return null instead of crashing
        }
    }
}
