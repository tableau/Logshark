using System;

namespace LogShark.Plugins.Backgrounder.Model
{
    // One row per table-insert performed during a backgrounder extract refresh.
    // Built by merging two nativeapi (BackgrounderCpp) events that share the same req + table-name:
    //   - "inserter-end"           -> ElapsedMsec, ChunkCount, SizeBytes (byte-count), MbPerSec
    //   - "data-inserter-summary"  -> Rows, SizeBytes (size-bytes), Elapsed + breakdown, ProtocolId
    // Linked back to the Java BackgrounderJob via req == request_id == job_id. See BackgrounderEventPersister.
    //
    // Reference shape of the two raw native events (both carry req/sess/site/user/pid/ts at the top level):
    //   data-inserter-summary: {"k":"data-inserter-summary","v":{"elapsed":"0.09","elapsed-finish":"0.038",
    //       "elapsed-get-builders":"0.034","elapsed-read":"0.010","protocol-id":"26","rows":"10,194",
    //       "size-bytes":"2,998,255","table-name":"Orders$"}}
    //   inserter-end:          {"k":"hyper-api","v":{"inserter-end":"{\"table-name\":\"Orders$\",
    //       \"elapsed-msec\":44,\"chunk-count\":1,\"byte-count\":2998255,\"mb-per-sec\":68.14}"}}
    public class BackgrounderExtractDataInserter
    {
        // ---- Linkage / identity ----

        /// <summary>
        /// ID of the Java backgrounder job this insert belongs to. The native events only carry
        /// <see cref="RequestId"/> (req); the persister resolves req -> request_id -> job_id at drain time.
        /// Only populated for refresh_extracts / increment_extracts jobs.
        /// </summary>
        public string BackgrounderJobId { get; set; }

        /// <summary>
        /// Identifier of the worker (node) that ran this insert, taken from the native log file's
        /// LogFileInfo.Worker — the same value populated on <see cref="BackgrounderJob.WorkerId"/> and the
        /// other Backgrounder datasources. Lets inserter rows be joined to the rest of a worker's activity.
        /// </summary>
        public string WorkerId { get; set; }

        /// <summary>
        /// Native request GUID (the "req" field). Doubles as the merge key between the
        /// data-inserter-summary and inserter-end events and as the link back to the Java job.
        /// </summary>
        public string RequestId { get; set; }

        /// <summary>
        /// Event timestamp ("ts"). When the two native events are merged, the earliest of the two is kept.
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// The Hyper table being loaded (e.g. "Orders$"). Second half of the (req + table-name) merge key.
        /// </summary>
        public string TableName { get; set; }

        // ---- Performance ----

        /// <summary>
        /// Rows inserted into the table. From data-inserter-summary "rows". Logged with thousands
        /// separators (e.g. "10,194"); the parser strips commas before parsing.
        /// </summary>
        public long? Rows { get; set; }

        /// <summary>
        /// Bytes written for this table. Reported by BOTH events under different keys and formats:
        /// data-inserter-summary "size-bytes" (comma-formatted string, e.g. "2,998,255") and
        /// inserter-end "byte-count" (plain integer, e.g. 2998255). Same quantity; first value set wins.
        /// </summary>
        public long? SizeBytes { get; set; }

        /// <summary>
        /// Insert duration in MILLISECONDS as measured by the Hyper API layer. From inserter-end "elapsed-msec".
        /// Not a unit-converted copy of <see cref="Elapsed"/> — it is an independent measurement at a different layer.
        /// </summary>
        public long? ElapsedMsec { get; set; }

        /// <summary>
        /// Insert throughput in MB/s reported by the Hyper API. From inserter-end "mb-per-sec".
        /// </summary>
        public double? MbPerSec { get; set; }

        /// <summary>
        /// Total insert duration in SECONDS as measured by the data-inserter layer. From data-inserter-summary
        /// "elapsed". Measures a slightly different span than <see cref="ElapsedMsec"/> (ms, Hyper API layer),
        /// so the two will not match exactly; both are kept intentionally.
        /// </summary>
        public double? Elapsed { get; set; }

        // ---- Timing breakdown (all seconds, from data-inserter-summary; roughly sum toward Elapsed) ----

        /// <summary>Sub-phase (seconds): time spent reading source data. From "elapsed-read".</summary>
        public double? ElapsedRead { get; set; }

        /// <summary>Sub-phase (seconds): time spent acquiring/preparing column builders. From "elapsed-get-builders".</summary>
        public double? ElapsedGetBuilders { get; set; }

        /// <summary>Sub-phase (seconds): time spent finalizing/flushing the insert. From "elapsed-finish".</summary>
        public double? ElapsedFinish { get; set; }

        /// <summary>Number of data chunks the Hyper API wrote for this table. From inserter-end "chunk-count".</summary>
        public long? ChunkCount { get; set; }

        /// <summary>Internal protocol/connection identifier for the data-inserter session. From data-inserter-summary "protocol-id".</summary>
        public string ProtocolId { get; set; }

        // ---- Context ----

        /// <summary>VizQL session ID that ran the extract ("sess").</summary>
        public string VizqlSessionId { get; set; }

        /// <summary>Tableau site name ("site").</summary>
        public string Site { get; set; }

        /// <summary>User the extract job ran as ("user").</summary>
        public string User { get; set; }

        /// <summary>OS process ID of the backgrounder worker that logged the event ("pid").</summary>
        public int ProcessId { get; set; }

        // Copies values from the complementary native event (the two events have disjoint
        // metric fields plus identical identity fields), keeping the earliest timestamp.
        public void FillMissingFrom(BackgrounderExtractDataInserter other)
        {
            if (other == null)
            {
                return;
            }

            Rows ??= other.Rows;
            SizeBytes ??= other.SizeBytes;
            ElapsedMsec ??= other.ElapsedMsec;
            MbPerSec ??= other.MbPerSec;
            Elapsed ??= other.Elapsed;
            ElapsedRead ??= other.ElapsedRead;
            ElapsedGetBuilders ??= other.ElapsedGetBuilders;
            ElapsedFinish ??= other.ElapsedFinish;
            ChunkCount ??= other.ChunkCount;
            ProtocolId ??= other.ProtocolId;
            VizqlSessionId ??= other.VizqlSessionId;
            Site ??= other.Site;
            User ??= other.User;
            TableName ??= other.TableName;
            WorkerId ??= other.WorkerId;

            if (other.Timestamp != default && (Timestamp == default || other.Timestamp < Timestamp))
            {
                Timestamp = other.Timestamp;
            }
        }
    }
}
