using System;
using LogShark.Containers;
using LogShark.Extensions;
using LogShark.Plugins.Shared;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LogShark.Plugins.DataServer.Model
{
    public class DataServerEvent : BaseCppEvent
    {
        public string EventValue { get; }

        // Context fields
        public string ClientProcessId { get; }
        public string ClientRequestId { get; }
        public string ClientSessionId { get; }
        public string ClientThreadId { get; }
        public string ClientType { get; }
        public string ClientUsername { get; }
        
        // Value fields
        public string CacheLoadOutcome { get; set; }
        public string CacheNamespace { get; set; }
        public string CacheStoreOutcome { get; set; }
        public long? ChunkCount { get; set; }
        public long? Cols { get; set; }
        public string DbName { get; set; }
        public double? ElapsedMs { get; set; }
        public long? EqcLoadElapsedMs { get; set; }
        public string EqcLoadOutcome { get; set; }
        public string EqcSource { get; set; }
        public string KeyHash { get; set; }
        public double? MbPerSecond { get; set; }
        public string MemLoadGuid { get; set; }
        public long? MemLoadFetchMs { get; set; }
        public long? MemLoadLockMs { get; set; }
        public long? MemLoadTotalMs { get; set; }
        public string MemStoreGuid { get; set; }
        public long? MemStorePutMs { get; set; }
        public long? MemStoreLockMs { get; set; }
        public long? MemStoreTotalMs { get; set; }
        public string MemLoadOutcome { get; set; }
        public string LogicalQueryHash { get; set; }
        public string Port { get; set; }
        public string ProtocolClass { get; set; }
        public int? ProtocolGroupConnectionLimit { get; set; }
        public int? ProtocolGroupId { get; set; }
        public int? ProtocolGroupProtocolsCount { get; set; }
        public int? ProtocolId { get; set; }
        public string Query { get; set; }
        public string QueryCategory { get; set; }
        public long? QueryHash { get; set; }
        public string QueryKind { get; set; }
        public long? QueryLatencyMs { get; set; }
        public long? Rows { get; set; }
        public string Server { get; set; }
        public string TableName { get; set; }
        public string TempTableAction { get; set; }
        public long? ValueSizeBytes { get; set; }

        public DataServerEvent(
            LogLine logLine,
            DateTime timestamp,
            string eventKey,
            string requestId,
            string sessionId,
            string severity,
            string site,
            string threadId,
            string user,
            string message)
            : base(
                logLine,
                timestamp,
                eventKey,
                null,
                requestId,
                sessionId,
                severity,
                site,
                threadId,
                user)
        {
            EventValue = message;
        }
        
        public DataServerEvent(LogLine logLine, NativeJsonLogsBaseEvent baseEvent) : base(logLine, baseEvent)
        {
            if (baseEvent.EventPayload == null)
            {
                throw new ArgumentNullException(nameof(baseEvent.EventPayload), "Event payload cannot be null");
            }
            
            EventValue = baseEvent.EventPayload.ToString(Formatting.None);

            var contextMetrics = baseEvent.ContextMetrics;
            ClientProcessId = contextMetrics?.ClientProcessId;
            ClientRequestId = contextMetrics?.ClientRequestId;
            ClientSessionId = contextMetrics?.ClientSessionId;
            ClientThreadId = contextMetrics?.ClientThreadId;
            ClientType = contextMetrics?.ClientType;
            ClientUsername = contextMetrics?.ClientUsername;
            
            SetPropertiesForSpecificEvent(baseEvent.EventType, baseEvent.EventPayload);
        }
        
        private void SetPropertiesForSpecificEvent(string eventType, JToken payload)
        {
            if (payload == null)
            {
                return;
            }
            
            switch (eventType)
            {
                case "end-ds.connect-data-connection":
                case "end-ds.load-metadata":
                case "end-ds.connect":
                case "end-ds.validate":
                case "end-ds.validate-extract":
                case "end-ds.parser-connect-extract":
                case "end-ds.parser-connect":
                    ElapsedMs = payload.GetDoubleFromPath("elapsed") * 1000;
                    break;
                case "extract-archive-file":
                    ElapsedMs = payload.GetDoubleFromPath("elapsed-ms");
                    break;
                case "end-protocol.query":
                case "end-query":
                    Cols = payload.GetLongFromPath("cols");
                    ElapsedMs = payload.GetDoubleFromPath("elapsed") * 1000;
                    ProtocolClass = payload.GetStringFromPath("protocol-class");
                    ProtocolId = payload.GetIntFromPath("protocol-id");
                    Query = payload.GetStringFromPath("query-trunc");
                    QueryCategory = payload.GetStringFromPath("query-category");
                    QueryHash = payload.GetLongFromPath("query-hash");
                    Rows = payload.GetLongFromPath("rows");
                    break;
                case "get-cached-query":
                    CacheLoadOutcome = payload.GetStringFromPaths("cache-load-outcome", "cache-outcome");
                    Cols = payload.GetLongFromPath("eqc-column-count");
                    ElapsedMs = payload.GetDoubleFromPath("ms");
                    EqcLoadElapsedMs = payload.GetLongFromPath("eqc-load-elapsed-ms");
                    EqcLoadOutcome = payload.GetStringFromPaths("eqc-outcome", "eqc-load-outcome");
                    EqcSource = payload.GetStringFromPath("eqc-source");
                    KeyHash = payload.GetStringFromPath("eqc-key-hash");
                    LogicalQueryHash = payload.GetStringFromPath("logical-query-hash");
                    MemLoadFetchMs = payload.GetLongFromPath("mem-load-fetch-ms");
                    MemLoadGuid = payload.GetStringFromPath("mem-load-guid");
                    MemLoadLockMs = payload.GetLongFromPath("mem-load-lock-ms");
                    MemLoadOutcome = payload.GetStringFromPaths("mem-load-outcome", "mem-outcome");
                    MemLoadTotalMs = payload.GetLongFromPath("mem-load-total-ms");
                    MemStoreGuid = payload.GetStringFromPath("mem-store-guid");
                    MemStoreLockMs = payload.GetLongFromPath("mem-store-lock-ms");
                    MemStorePutMs = payload.GetLongFromPath("mem-store-put-ms");
                    MemStoreTotalMs = payload.GetLongFromPath("mem-store-total-ms");
                    ProtocolClass = payload.GetStringFromPath("class");
                    QueryKind = payload.GetStringFromPath("kind");
                    Rows = payload.GetLongFromPath("eqc-row-count");
                    ValueSizeBytes = payload.GetLongFromPath("eqc-value-size-b");
                    break;
                case "begin-protocol.query":
                case "begin-query":
                    ProtocolId = payload.GetIntFromPath("protocol-id");
                    Query = payload.GetStringFromPath("query");
                    QueryCategory = payload.GetStringFromPath("query-category");
                    QueryHash = payload.GetLongFromPath("query-hash");
                    break;
                case "ec-load":
                    CacheLoadOutcome = payload.GetStringFromPath("outcome");
                    CacheNamespace = payload.GetStringFromPath("cns");
                    ElapsedMs = payload.GetDoubleFromPath("elapsed-ms");
                    KeyHash = payload.GetStringFromPath("key-hash");
                    break;
                case "ec-store":
                    CacheNamespace = payload.GetStringFromPath("cns");
                    CacheStoreOutcome = payload.GetStringFromPath("outcome");
                    ElapsedMs = payload.GetDoubleFromPath("elapsed-ms");
                    KeyHash = payload.GetStringFromPath("key-hash");
                    ValueSizeBytes = payload.GetLongFromPath("value-size-b");
                    break;
                case "eqc-store":
                    CacheStoreOutcome = payload.GetStringFromPaths("outcome", "eqc-store-outcome", "cache-store-outcome");
                    Cols = payload.GetLongFromPath("column-count");
                    ElapsedMs = payload.GetDoubleFromPaths("elapsed-total-ms", "eqc-total-ms");
                    ProtocolClass = payload.GetStringFromPath("class");
                    QueryKind = payload.GetStringFromPaths("query-kind", "kind");
                    QueryLatencyMs = payload.GetLongFromPath("query-latency-ms");
                    Rows = payload.GetLongFromPath("row-count");
                    ValueSizeBytes = payload.GetLongFromPath("value-size-b");
                    break;
                case "read-metadata":
                    ElapsedMs = payload.GetDoubleFromPath("elapsed") * 1000;
                    ProtocolId = payload.GetIntFromPath("id");
                    TableName = payload.GetStringFromPath("table");

                    var readMetadataAttributes = payload.SelectToken("attributes", false); 
                    DbName = readMetadataAttributes?.GetStringFromPath("dbname");
                    ProtocolClass = readMetadataAttributes?.GetStringFromPath("class");
                    
                    break;
                case "data-server-temp-table":
                    ElapsedMs = payload.GetDoubleFromPath("elapsed-ms");
                    TempTableAction = payload.GetStringFromPath("action");
                    break;
                case "end-sql-regular-table-tuples-insert":
                    Cols = payload.GetLongFromPath("num-columns");
                    ElapsedMs = payload.GetDoubleFromPath("elapsed-insert") * 1000;
                    ProtocolId = payload.GetIntFromPath("protocol-id");
                    Rows = payload.GetLongFromPath("num-tuples");
                    break;
                case "end-sql-regular-table-tuples-create":
                    Cols = payload.GetLongFromPath("num-columns");
                    ElapsedMs = payload.GetDoubleFromPath("elapsed-create") * 1000;
                    ProtocolId = payload.GetIntFromPath("protocol-id");
                    break;
                case "end-sql-temp-table-tuples-create":
                    ElapsedMs = payload.GetDoubleFromPath("elapsed") * 1000;
                    ProtocolId = payload.GetIntFromPath("protocol-id");
                    break;
                case "hyper-libpq-protocol":
                    ElapsedMs = payload.GetDoubleFromPath("elapsed") * 1000;
                    ProtocolId = payload.GetIntFromPath("protocol-id");
                    Rows = payload.GetLongFromPath("rows");
                    break;
                case "hyper-query-summary":
                    ElapsedMs = payload.GetDoubleFromPath("elapsed") * 1000;
                    ProtocolId = payload.GetIntFromPath("protocol-id");
                    QueryHash = payload.GetLongFromPath("query-hash");
                    break;
                case "hyper-read-first-tuples":
                case "end-hyper-read-tuples":
                    Cols = payload.GetLongFromPath("cols");
                    ElapsedMs = payload.GetDoubleFromPath("elapsed") * 1000;
                    Rows = payload.GetLongFromPathAnyNumberStyle("rows");
                    break;
                case "hyper-api":
                    var inserterEnd = payload.GetStringFromPath("inserter-end");
                    var inserterEndToken = string.IsNullOrWhiteSpace(inserterEnd) || !inserterEnd.StartsWith('{')
                        ? JToken.FromObject(new {})
                        : JToken.Parse(inserterEnd);
                    ChunkCount = inserterEndToken?.GetLongFromPath("chunk-count");
                    ElapsedMs = inserterEndToken?.GetDoubleFromPath("elapsed-msec");
                    MbPerSecond = inserterEndToken?.GetDoubleFromPath("mb-per-sec");
                    TableName = inserterEndToken?.GetStringFromPath("table-name");
                    ValueSizeBytes = inserterEndToken?.GetLongFromPath("byte-count");
                    break;
                case "data-inserter-summary":
                case "hyper-send-chunk":
                    ElapsedMs = payload.GetDoubleFromPath("elapsed") * 1000;
                    ProtocolId = payload.GetIntFromPath("protocol-id");
                    Rows = payload.GetLongFromPath("rows");
                    ValueSizeBytes = payload.GetLongFromPathAnyNumberStyle("size-bytes");
                    break;
                case "hyper-process-chunks":
                    ChunkCount = payload.GetLongFromPath("chunks");
                    ElapsedMs = payload.GetDoubleFromPath("elapsed") * 1000;
                    break;
                case "hyper-format-tuples":
                    ChunkCount = payload.GetLongFromPath("chunks-processed");
                    ElapsedMs = payload.GetDoubleFromPath("elapsed") * 1000;
                    break;
                case "construct-protocol-group":
                case "destruct-protocol-group":
                    ProtocolGroupConnectionLimit = payload.GetIntFromPath("connection-limit");
                    ProtocolGroupId = payload.GetIntFromPath("group-id");
                    ProtocolGroupProtocolsCount = payload.GetIntFromPath("protocols-count");

                    var constructProtocolGroupAttributes = payload.SelectToken("attributes", false);
                    DbName = constructProtocolGroupAttributes?.GetStringFromPath("dbname");
                    Port = constructProtocolGroupAttributes?.GetStringFromPath("port");
                    ProtocolClass = constructProtocolGroupAttributes?.GetStringFromPath("class");
                    Server = constructProtocolGroupAttributes?.GetStringFromPath("server");
                    
                    break;
                case "construct-protocol":
                    ElapsedMs = payload.GetDoubleFromPath("created-elapsed") * 1000;
                    ProtocolId = payload.GetIntFromPath("id");
                    
                    var constructProtocolAttributes = payload.SelectToken("attributes", false);
                    DbName = constructProtocolAttributes?.GetStringFromPath("dbname");
                    Port = constructProtocolAttributes?.GetStringFromPath("port");
                    ProtocolClass = constructProtocolAttributes?.GetStringFromPath("class");
                    Server = constructProtocolAttributes?.GetStringFromPath("server");

                    break;
                case "destruct-protocol-elapsed":
                    ElapsedMs = payload.GetDoubleFromPath("elapsed") * 1000;
                    ProtocolGroupId = payload.GetIntFromPath("group-id");
                    ProtocolId = payload.GetIntFromPath("id");
                    
                    var destructProtocolAttributes = payload.SelectToken("attributes", false);
                    DbName = destructProtocolAttributes?.GetStringFromPath("dbname");
                    Port = destructProtocolAttributes?.GetStringFromPath("port");
                    ProtocolClass = destructProtocolAttributes?.GetStringFromPath("class");
                    Server = destructProtocolAttributes?.GetStringFromPath("server");

                    break;
                case "protocol.create-protocol":
                    ProtocolClass = payload.GetStringFromPath("class");
                    break;
                case "protocol-added-to-group":
                case "protocol-removed-from-group":
                    ProtocolId = payload.GetIntFromPath("protocol-id");
                    
                    var groupPayload = payload.SelectToken("group", false);
                    ProtocolGroupConnectionLimit = groupPayload?.GetIntFromPath("connection-limit");
                    ProtocolGroupId = groupPayload?.GetIntFromPath("group-id");
                    ProtocolGroupProtocolsCount = groupPayload?.GetIntFromPath("protocols-count");

                    var groupAttributes = groupPayload?.SelectToken("attributes", false);
                    DbName = groupAttributes?.GetStringFromPath("dbname");
                    Port = groupAttributes?.GetStringFromPath("port");
                    ProtocolClass = groupAttributes?.GetStringFromPath("class");
                    Server = groupAttributes?.GetStringFromPath("server");

                    break;
            }
        }
    }
}