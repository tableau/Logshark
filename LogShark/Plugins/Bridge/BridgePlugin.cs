using Flurl.Util;
using Logshark.Plugins.Replayer.Models;
using LogShark.Containers;
using LogShark.Plugins.Bridge.Model;
using LogShark.Shared;
using LogShark.Shared.LogReading.Containers;
using LogShark.Writers;
using LogShark.Writers.Containers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Tableau.HyperAPI;
using YamlDotNet.Core.Tokens;

namespace LogShark.Plugins.Bridge
{
    public class BridgePlugin : IPlugin
    {
        private static readonly List<LogType> ConsumedLogTypesInternal = new List<LogType> { LogType.Bridge };
        public IList<LogType> ConsumedLogTypes => ConsumedLogTypesInternal;

        public string Name => "Bridge";

        private static readonly DataSetInfo ClientWorkerOutputInfo = new DataSetInfo("Bridge", "BridgeClientWorkerEvents");
        private static readonly DataSetInfo ClientOutputInfo = new DataSetInfo("Bridge", "BridgeClientEvents");
        private static readonly DataSetInfo JobDetailsOutputInfo = new DataSetInfo("Bridge", "BridgeJobDetails");
        private static readonly DataSetInfo JobEnvironmentOutputInfo = new DataSetInfo("Bridge", "BridgeJobEnvironment");
        private static readonly DataSetInfo LiveQueryMetricsInfo = new DataSetInfo("Bridge", "LiveQueryMetrics");
        private static readonly DataSetInfo ProtocolQueryInfo = new DataSetInfo("Bridge", "ProtocolQueryInfo");
        private IWriter<BridgeClientWorkerEvent> _clientWorkerWriter;
        private IWriter<BridgeClientEvent> _clientWriter;
        private IWriter<BridgeJobDetails> _jobDetailsWriter;
        private IWriter<BridgeJobEnvironment> _jobEnvironmentWriter;
        private IWriter<LiveQueryMetricsEvent> _liveQueryMetricsWriter;
        private IWriter<ProtocolQueryEvent> _protocolQueryWriter;
        private IProcessingNotificationsCollector _processingNotificationsCollector;

        // Track job details and environments
        private readonly ConcurrentDictionary<string, BridgeJobDetails> _jobDetails = new ConcurrentDictionary<string, BridgeJobDetails>();
        private readonly ConcurrentDictionary<string, BridgeJobDetails> _jobDetailsUpdate = new ConcurrentDictionary<string, BridgeJobDetails>();
        private readonly ConcurrentDictionary<string, BridgeClientWorkerEvent> _clientWorkerEvents = new ConcurrentDictionary<string, BridgeClientWorkerEvent>();
        private readonly ConcurrentDictionary<string, DateTime> _clientWorkerEndEvents = new ConcurrentDictionary<string, DateTime>();
        private readonly ConcurrentDictionary<long, ProtocolQueryEvent> _protocolQueryDetails = new ConcurrentDictionary<long, ProtocolQueryEvent>();
        private readonly ConcurrentDictionary<long, NativeJsonLogsBaseEvent> _protocolQueryEndDetails = new ConcurrentDictionary<long, NativeJsonLogsBaseEvent>();
        private readonly ConcurrentDictionary<string, BridgeJobEnvironment> _jobEnvironments = new ConcurrentDictionary<string, BridgeJobEnvironment>();

        private readonly object _jobDetailsLock = new object();


        public void Configure(IWriterFactory writerFactory, IConfiguration pluginConfig, IProcessingNotificationsCollector processingNotificationsCollector, ILoggerFactory loggerFactory)
        {
            _clientWorkerWriter = writerFactory.GetWriter<BridgeClientWorkerEvent>(ClientWorkerOutputInfo);
            _clientWriter = writerFactory.GetWriter<BridgeClientEvent>(ClientOutputInfo);
            _jobDetailsWriter = writerFactory.GetWriter<BridgeJobDetails>(JobDetailsOutputInfo);
            _jobEnvironmentWriter = writerFactory.GetWriter<BridgeJobEnvironment>(JobEnvironmentOutputInfo);
            _liveQueryMetricsWriter = writerFactory.GetWriter<LiveQueryMetricsEvent>(LiveQueryMetricsInfo);
            _protocolQueryWriter = writerFactory.GetWriter<ProtocolQueryEvent>(ProtocolQueryInfo);
            _processingNotificationsCollector = processingNotificationsCollector;
        }

        public void ProcessLogLine(LogLine logLine, LogType logType)
        {

            if (!(logLine.LineContents is NativeJsonLogsBaseEvent baseEvent))
            {
                var errorMessage = $"Was not able to cast line contents as {nameof(NativeJsonLogsBaseEvent)}";
                _processingNotificationsCollector.ReportError(errorMessage, logLine, nameof(BridgePlugin));
                return;
            }

            try
            {
                var fileName = logLine.LogFileInfo.FileName;
                if (!logLine.LogFileInfo.Worker.Contains(".crash"))
                {
                    if (fileName.StartsWith("TabBridgeClientWorker"))
                    {
                        ProcessClientWorkerEvent(logLine, baseEvent);
                    }
                    else if (fileName.StartsWith("TabBridgeClient"))
                    {
                        ProcessClientEvent(logLine, baseEvent);
                    }
                    else if (fileName.StartsWith("TabBridgeCliJob"))
                    {
                        ProcessCliJobEvent(logLine, baseEvent);
                    }
                    else if (fileName.StartsWith("LiveQueryMetrics"))
                    {
                        ProcessLiveQueryMetrics(logLine, baseEvent);
                    }
                    else
                    {
                        // Skip files that don't match our expected patterns
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                _processingNotificationsCollector.ReportError($"Failed to parse Bridge log line: {ex.Message}", logLine, nameof(BridgePlugin));
            }
        }

        private void ProcessLiveQueryMetrics(LogLine logLine, NativeJsonLogsBaseEvent baseEvent)
        {

            if (baseEvent.EventType == "livequery-metrics")
            {
                var liveQueryMetricsEvent = new LiveQueryMetricsEvent(logLine, baseEvent);
                _liveQueryMetricsWriter.AddLine(liveQueryMetricsEvent);
            }

        }

        private void ProcessClientWorkerEvent(LogLine logLine, NativeJsonLogsBaseEvent baseEvent)
        {

            if (baseEvent.EventType == "live-query") //only process lines with live-query key
            {
                var payloadJsonStr = baseEvent.EventPayload?.ToString(Formatting.None);



                if (payloadJsonStr.Contains("command"))
                {

                    if (payloadJsonStr.Contains("begin-live-query-dispatch"))
                    {
                        var clientWorkerEvent = new BridgeClientWorkerEvent(logLine, baseEvent);
                        _clientWorkerEvents[clientWorkerEvent.RequestID + clientWorkerEvent.Command] = clientWorkerEvent;
                    }
                    else if (payloadJsonStr.Contains("finish-live-query-dispatch"))
                    {
                        var payload = JObject.Parse(payloadJsonStr);
                        string requestID = payload?["requestID"]?.ToString() ?? null;
                        string command = payload?["command"]?.ToString() ?? null;
                        _clientWorkerEndEvents[requestID + command] = baseEvent.Timestamp;
                        foreach (var clientWorkerEvent in _clientWorkerEvents)
                        {
                            if (clientWorkerEvent.Key == requestID + command)
                            {
                                clientWorkerEvent.Value.EndTime = baseEvent.Timestamp;
                                clientWorkerEvent.Value.TimeTakenSeconds = (clientWorkerEvent.Value.EndTime - clientWorkerEvent.Value.StartTime).TotalSeconds;
                                clientWorkerEvent.Value.EventEnded = true;

                                
                                bool removed = _clientWorkerEvents.TryRemove(clientWorkerEvent.Key, out _);
                                if (removed)
                                {
                                    _clientWorkerWriter.AddLine(clientWorkerEvent.Value);
                                }

                                }
                        }




                    }


                }
            }



            if (baseEvent.EventType == "begin-protocol.query") //only process lines with live-query key
            {
                var Value = baseEvent.EventPayload?.ToString(Formatting.None);
                var payloadJson = Value.ToString();
                var payload = JsonConvert.DeserializeObject<dynamic>(payloadJson);
                long QueryHash = long.Parse(payload?["query-hash"]?.ToString()) ?? null;

                if (!_protocolQueryDetails.ContainsKey(QueryHash))
                {
                    var protocolQueryEventDetails = new ProtocolQueryEvent(logLine, baseEvent);
                    if (protocolQueryEventDetails != null)
                    {
                        _protocolQueryDetails[QueryHash] = protocolQueryEventDetails;
                    }
                }

            }
            if (baseEvent.EventType == "end-protocol.query")
            {
                var Value = baseEvent.EventPayload?.ToString(Formatting.None);
                var payloadJson = Value.ToString();
                var payload = JsonConvert.DeserializeObject<dynamic>(payloadJson);
                long queryHash = long.Parse(payload?["query-hash"]?.ToString()) ?? null;
                _protocolQueryEndDetails[queryHash] = baseEvent;
                foreach (var protocolQueryDetail in _protocolQueryDetails)
                {
                    if (protocolQueryDetail.Key == queryHash)
                    {
                        protocolQueryDetail.Value.EventEnded = true;
                        //match the end event here
                        protocolQueryDetail.Value.Cols = long.Parse(payload?["cols"]?.ToString()) ?? null;
                        protocolQueryDetail.Value.Elapsed = double.Parse(payload?["elapsed"]?.ToString()) ?? null;
                        protocolQueryDetail.Value.ProtocolClass = payload?["protocol-class"]?.ToString() ?? null;
                        protocolQueryDetail.Value.ProtocolID = int.Parse(payload?["protocol-id"]?.ToString()) ?? null;

                        protocolQueryDetail.Value.QueryTrunc = payload?["query-trunc"]?.ToString() ?? null;
                        protocolQueryDetail.Value.Rows = long.Parse(payload?["rows"]?.ToString()) ?? 0;
                        //if below fails to remove, we will still check is again at the end
                      
                        bool removed = _protocolQueryDetails.TryRemove(protocolQueryDetail.Key, out _);
                        if (removed)
                        {
                            _protocolQueryWriter.AddLine(protocolQueryDetail.Value);
                        }


                    }
                }
            }


            // Check if this line contains a jobID by looking at the EventPayload directly

            var folderName = logLine.LogFileInfo.Worker; // We extract worker as the folder when processing bridge folders.
            var jobId = ExtractJobIdFromMessage(baseEvent);
            if (!string.IsNullOrEmpty(jobId))
            {
                // Create or get existing job details for this jobID
                if (!_jobDetails.ContainsKey(jobId) )
                {
                    _jobDetails[jobId] = new BridgeJobDetails(jobId, baseEvent.Timestamp, baseEvent.ProcessId, baseEvent.ThreadId ?? "0");
                }
                foreach (var jobDetail in _jobDetails)
                {
                    bool jobFinished = jobDetail.Value.UpdateFromAnyEvent(baseEvent.EventPayload, baseEvent.Timestamp, folderName, baseEvent.EventType);


                    if (jobFinished && jobDetail.Value.HostName != null)
                    {
                        
                        bool removed = _jobDetails.TryRemove(jobDetail.Key, out _); //Remove the jobID from list when job is detected as finished
                        if (removed)
                        {
                            _jobDetailsWriter.AddLine(jobDetail.Value);
                        }
                    }

                }
            }


        }




        private void ProcessClientEvent(LogLine logLine, NativeJsonLogsBaseEvent baseEvent)
        {
            var Value = baseEvent.EventPayload?.ToString(Formatting.None);
            var payloadJson = Value.ToString();
            var payload = JsonConvert.DeserializeObject<dynamic>(payloadJson);
            if (payloadJson.Contains("Current client config file content"))
            {
                // Get the raw string value from EventPayload for extraction
                string valueString;
                if (baseEvent.EventPayload != null && baseEvent.EventPayload.Type == JTokenType.String)
                {
                    valueString = baseEvent.EventPayload.Value<string>();
                }
                else
                {
                    valueString = baseEvent.EventPayload?.ToString(Formatting.None);
                }

                // Extract and flatten all config fields
                var flattenedFields = BridgeClientEvent.ExtractAndFlattenJson(valueString);

                // Create one event per config key-value pair
                if (flattenedFields.Count > 0)
                {
                    foreach (var kvp in flattenedFields)
                    {
                        var clientEvent = new BridgeClientEvent(logLine, baseEvent, kvp.Key, kvp.Value);
                        _clientWriter.AddLine(clientEvent);
                    }
                }
                else
                {
                    // If no fields extracted, still create an event with null config fields
                    var clientEvent = new BridgeClientEvent(logLine, baseEvent);
                    _clientWriter.AddLine(clientEvent);
                }
            }

        }

        private void ProcessCliJobEvent(LogLine logLine, NativeJsonLogsBaseEvent baseEvent)
        {
            try
            {
                // Get the job ID for this file (cached per file)
                var fileName = logLine?.LogFileInfo?.FileName;

                if (string.IsNullOrEmpty(fileName) || baseEvent == null)
                {
                    return;
                }


                var jobId = GetOrExtractJobIdForFile(fileName, baseEvent);

                // If we just found the jobID and it's different from filename, migrate the data FIRST
                if (!string.IsNullOrEmpty(jobId) && jobId != fileName && _jobEnvironments.ContainsKey(fileName))
                {
                    // Migrate data from filename key to jobID key
                    if (_jobEnvironments.ContainsKey(fileName) && !_jobEnvironments.ContainsKey(jobId))
                    {
                        _jobEnvironments[jobId] = _jobEnvironments[fileName];
                        _jobEnvironments[jobId].JobId = jobId;
                        _jobEnvironments.TryRemove(fileName, out _);
                    }
                }

                // Use filename as temporary key if we don't have jobID yet
                var key = string.IsNullOrEmpty(jobId) ? fileName : jobId;

                // Get or create job environment
                if (!_jobEnvironments.ContainsKey(key))
                {
                    try
                    {
                        _jobEnvironments[key] = new BridgeJobEnvironment(key);
                    }
                    catch (Exception ex)
                    {
                        throw;
                    }
                }

                // Only update environment for environment-related events
                if (IsEnvironmentEvent(baseEvent.EventType))
                {
                    try
                    {
                        _jobEnvironments[key].UpdateFromAnyEvent(baseEvent.EventType, baseEvent.EventPayload);
                    }
                    catch (Exception ex)
                    {
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                _processingNotificationsCollector?.ReportError($"Failed to process CLI job event: {ex.Message}", logLine, nameof(BridgePlugin));
            }
        }

        private readonly Dictionary<string, string> _fileJobIdCache = new Dictionary<string, string>();

        private string GetOrExtractJobIdForFile(string fileName, NativeJsonLogsBaseEvent baseEvent)
        {

            var jobId = ExtractJobIdFromCliJobEvent(baseEvent);
            if (!string.IsNullOrEmpty(jobId))
            {
                _fileJobIdCache[fileName] = jobId;
            }

            return jobId;
        }

        private bool IsEnvironmentEvent(string eventType)
        {
            return eventType == "startup-info" ||
                   eventType == "environment" ||
                   eventType == "cpu-memory-info" ||
                   eventType == "memory-usage" ||
                   eventType == "msg";
        }

        private string ExtractJobIdFromCliJobEvent(NativeJsonLogsBaseEvent baseEvent)
        {
            try
            {
                if (baseEvent == null)
                {
                    return null;
                }

                var eventType = baseEvent.EventType;
                var payload = baseEvent.EventPayload?.ToString();


                if (payload == null) return null;

                // Look for job ID in various places
                string jobId = null;

                if (eventType == "msg")
                {
                    var message = payload?.ToString();
                    if (!string.IsNullOrEmpty(message))
                    {
                        // Pattern: "CLI jobID is 8b976dfe-3bd4-422e-b2fc-e3e38befb49a"
                        var jobIdMatch = System.Text.RegularExpressions.Regex.Match(message, @"jobID is\s+([a-f0-9-]+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                        if (jobIdMatch.Success)
                        {
                            jobId = jobIdMatch.Groups[1].Value;
                            return CleanJobId(jobId);
                        }
                    }
                }
                else if (eventType?.Contains("jobId:") == true)
                {
                    // Pattern: "[jobId: {8B976DFE-3BD4-422E-B2FC-E3E38BEFB49A}] - begin-extract-refresh"
                    var jobIdMatch = System.Text.RegularExpressions.Regex.Match(eventType, @"jobId:\s*\{?([a-f0-9-]+)\}?", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                    if (jobIdMatch.Success)
                    {
                        jobId = jobIdMatch.Groups[1].Value;
                        return CleanJobId(jobId);
                    }
                }


                return null;
            }
            catch
            {
                return null;
            }
        }

        private string CleanJobId(string jobId)
        {
            if (string.IsNullOrEmpty(jobId))
                return jobId;

            return jobId.Trim('{', '}', '"', '\'').ToLowerInvariant();
        }

        private string ExtractJobIdFromMessage(NativeJsonLogsBaseEvent baseEvent)
        {
            try
            {
                if (baseEvent?.EventPayload == null)
                {
                    return null;
                }

                // First, check if EventType (k field) contains a jobID pattern (e.g., "job-send-request-{jobID}")
                if (!string.IsNullOrEmpty(baseEvent.EventType))
                {
                    if (baseEvent.EventType.Contains("job-send-request"))
                    {
                        var eventTypeJobIdMatch = System.Text.RegularExpressions.Regex.Match(baseEvent.EventType, @"([a-f0-9]{8}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{12})", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                        if (eventTypeJobIdMatch.Success)
                        {
                            return CleanJobId(eventTypeJobIdMatch.Groups[1].Value);
                        }
                    }
                    else if (baseEvent.EventType.Contains("remote-job"))
                    {
                        // EventPayload is a JToken, try to cast to JObject
                        if (baseEvent.EventPayload.ToString().Contains("jobID"))
                        {
                            var payload = baseEvent.EventPayload as Newtonsoft.Json.Linq.JObject;
                            if (payload != null)
                            {
                                // Check if payload has jobID field (case sensitive first, then case insensitive)
                                //works for "jobID": "xxx-xx-xxx-xx-xxx", "msg":"Adding xx xx"
                                var jobIdToken = payload["jobID"];
                                if (jobIdToken != null)
                                {
                                    var jobId = jobIdToken.ToString();
                                    if (!string.IsNullOrEmpty(jobId))
                                    {
                                        return CleanJobId(jobId);
                                    }
                                }

                            }
                        }
                    }
                    else
                    {
                        //unknown event
                        return null;
                    }
                }

                return null;


            }
            catch (Exception ex)
            {
                _processingNotificationsCollector?.ReportError($"Error extracting jobID: {ex.Message}", nameof(BridgePlugin));
                return null;
            }
        }
        public Newtonsoft.Json.Linq.JObject baseEventToPayload(NativeJsonLogsBaseEvent baseEvent)
        {
            var Value = baseEvent.EventPayload?.ToString(Formatting.None);
            var payloadJson = Value.ToString();
            var payload = JsonConvert.DeserializeObject<dynamic>(payloadJson);
            return payload;
        }

        public SinglePluginExecutionResults CompleteProcessing()
        {

            // Write all accumulated job details
            foreach (var jobDetail in _jobDetails)
            {

                _jobDetailsWriter.AddLine(jobDetail.Value);
            }

            // Write all accumulated job environments
            foreach (var jobEnvironment in _jobEnvironments.Values)
            {
                _jobEnvironmentWriter.AddLine(jobEnvironment);
            }
            foreach (var protocolQueryDetail in _protocolQueryDetails)
            {


                foreach (var protocolQueryEndDetail in _protocolQueryEndDetails)
                {
                    if (protocolQueryDetail.Key == protocolQueryEndDetail.Key)
                    {
                        protocolQueryDetail.Value.EventEnded = true;
                        //match the end event here
                        var Value = protocolQueryEndDetail.Value.EventPayload?.ToString(Formatting.None);
                        var payloadJson = Value.ToString();
                        var payload = JsonConvert.DeserializeObject<dynamic>(payloadJson);

                        protocolQueryDetail.Value.Cols = long.Parse(payload?["cols"]?.ToString()) ?? null;
                        protocolQueryDetail.Value.Elapsed = double.Parse(payload?["elapsed"]?.ToString()) ?? null;
                        protocolQueryDetail.Value.ProtocolClass = payload?["protocol-class"]?.ToString() ?? null;
                        protocolQueryDetail.Value.ProtocolID = int.Parse(payload?["protocol-id"]?.ToString()) ?? null;

                        protocolQueryDetail.Value.QueryTrunc = payload?["query-trunc"]?.ToString() ?? null;
                        protocolQueryDetail.Value.Rows = long.Parse(payload?["rows"]?.ToString()) ?? 0;
                    }

                }
                _protocolQueryWriter.AddLine(protocolQueryDetail.Value);
            }
            foreach (var clientWorkerEvent in _clientWorkerEvents)
            {


                foreach (var clientWorkerEndEvent in _clientWorkerEndEvents)
                {
                    if (clientWorkerEvent.Key == clientWorkerEndEvent.Key)
                    {
                        clientWorkerEvent.Value.EndTime = clientWorkerEndEvent.Value;
                        clientWorkerEvent.Value.TimeTakenSeconds = (clientWorkerEvent.Value.EndTime - clientWorkerEvent.Value.StartTime).TotalSeconds;
                        clientWorkerEvent.Value.EventEnded = true;
                    }


                }
                _clientWorkerWriter.AddLine(clientWorkerEvent.Value);

            }


            var clientWorkerStatistics = _clientWorkerWriter.Close();
            var clientStatistics = _clientWriter.Close();
            var jobDetailsStatistics = _jobDetailsWriter.Close();
            var jobEnvironmentStatistics = _jobEnvironmentWriter.Close();
            var liveQueryMetricsStatistics = _liveQueryMetricsWriter.Close();
            var protocolQueryStatistics = _protocolQueryWriter.Close();

            return new SinglePluginExecutionResults(new List<WriterLineCounts>
            {
                clientWorkerStatistics,
                clientStatistics,
                jobDetailsStatistics,
                jobEnvironmentStatistics,
                liveQueryMetricsStatistics,
                protocolQueryStatistics
            });
        }

        public void Dispose()
        {
            _clientWorkerWriter?.Dispose();
            _clientWriter?.Dispose();
            _jobDetailsWriter?.Dispose();
            _jobEnvironmentWriter?.Dispose();
            _liveQueryMetricsWriter?.Dispose();
            _protocolQueryWriter?.Dispose();
        }
    }
}