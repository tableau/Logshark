using System;
using System.Collections.Generic;
using LogShark.Containers;
using LogShark.Extensions;
using LogShark.Plugins.Shared;
using LogShark.Plugins.VizqlDesktop.Model;
using LogShark.Writers;
using LogShark.Writers.Containers;

namespace LogShark.Plugins.VizqlDesktop
{
    public class VizqlDesktopEventProcessor : IDisposable
    {
        private static readonly DataSetInfo SessionsDsi = new DataSetInfo("VizqlDesktop", "VizqlDesktopSessions");
        private static readonly DataSetInfo ErrorsDsi = new DataSetInfo("VizqlDesktop", "VizqlDesktopErrorEvents");
        private static readonly DataSetInfo EndQueryEventsDsi = new DataSetInfo("VizqlDesktop", "VizqlDesktopEndQueryEvents");
        private static readonly DataSetInfo PerformanceEventsDsi = new DataSetInfo("VizqlDesktop", "VizqlDesktopPerformanceEvents");

        private readonly IWriter<VizqlDesktopSession> _sessionsWriter;
        private readonly IWriter<VizqlDesktopErrorEvent> _errorsWriter;
        private readonly IWriter<VizqlEndQueryEvent> _endQueryEventsWriter;
        private readonly IWriter<VizqlPerformanceEvent> _performanceEventsWriter;
        private readonly SessionTracker _sessionTracker;
        private readonly int _maxQueryLength;

        public VizqlDesktopEventProcessor(IWriterFactory writerFactory, int maxQueryLength)
        {
            _sessionsWriter = writerFactory.GetWriter<VizqlDesktopSession>(SessionsDsi);
            _errorsWriter = writerFactory.GetWriter<VizqlDesktopErrorEvent>(ErrorsDsi);
            _endQueryEventsWriter = writerFactory.GetWriter<VizqlEndQueryEvent>(EndQueryEventsDsi);
            _performanceEventsWriter = writerFactory.GetWriter<VizqlPerformanceEvent>(PerformanceEventsDsi);
            _sessionTracker = new SessionTracker();
            _maxQueryLength = maxQueryLength;
        }

        public void ProcessEvent(NativeJsonLogsBaseEvent baseEvent, LogLine logLine)
        {
            if (baseEvent.EventType == "startup-info")
            {
                var @event = new VizqlDesktopSession(baseEvent);
                _sessionsWriter.AddLine(@event);
                _sessionTracker.RegisterSession(logLine.LogFileInfo.FilePath, @event.SessionId);
                return;
            }

            var sessionId = _sessionTracker.GetSessionId(logLine.LogFileInfo.FilePath);
            
            if (baseEvent.Severity == "error" || baseEvent.Severity == "fatal")
            {
                var @event = new VizqlDesktopErrorEvent(baseEvent, logLine, sessionId);
                _errorsWriter.AddLine(@event);
                return;
            }
            
            if (sessionId == null) // Only errors allowed to be captured without session 
            {
                return;
            }

            if (baseEvent.EventType == "end-query")
            {
                var @event = new VizqlEndQueryEvent(baseEvent, logLine, sessionId, _maxQueryLength);
                _endQueryEventsWriter.AddLine(@event);
                // No return so we can output this event as a performance event as well. Workbook currently expects to see end-query in both data sources 
            }

            var performanceEvent = TryParsePerformanceEvent(baseEvent, logLine, sessionId);
            if (performanceEvent != null)
            {
                _performanceEventsWriter.AddLine(performanceEvent);
                return;
            };
        }

        internal IEnumerable<WriterLineCounts> CompleteProcessing()
        {
            return new List<WriterLineCounts>
            {
                _sessionsWriter.Close(),
                _errorsWriter.Close(),
                _endQueryEventsWriter.Close(),
                _performanceEventsWriter?.Close()
            };
        }

        public void Dispose()
        {
            _sessionsWriter?.Dispose();
            _errorsWriter?.Dispose();
            _endQueryEventsWriter?.Dispose();
            _performanceEventsWriter?.Dispose();
        }

        private static VizqlPerformanceEvent TryParsePerformanceEvent(NativeJsonLogsBaseEvent baseEvent, LogLine logLine, string sessionId)
        {
            if (!SupportedPerformanceEvents.ContainsKey(baseEvent.EventType))
            {
                return null;
            }

            var (elapsedPath, divisorToGetSeconds) = SupportedPerformanceEvents[baseEvent.EventType];
            var elapsedSeconds = baseEvent.EventPayload.GetDoubleFromPath(elapsedPath) / divisorToGetSeconds;
            
            return new VizqlPerformanceEvent(logLine, baseEvent, sessionId, elapsedSeconds);
        }

        private static readonly Dictionary<string, (string ElapsedPath, double DivisorToGetSeconds)> SupportedPerformanceEvents = new Dictionary<string, (string, double)>
        {
            { "compute-percentages", ("elapsed", 1) },
            { "compute-x-axis-descriptor", ("elapsed", 1) },
            { "compute-x-set-interp", ("elapsed", 1) },
            { "compute-y-axis-descriptor", ("elapsed", 1) },
            { "compute-y-set-interp", ("elapsed", 1) },
            { "construct-protocol", ("created-elapsed", 1) },
            { "ec-drop", ("elapsed-ms", 1000) },
            { "ec-load", ("elapsed-ms", 1000) },
            { "ec-store", ("elapsed-ms", 1000) },
            { "end-compute-quick-filter-state", ("elapsed", 1) },
            { "end-data-interpreter", ("elapsed", 1) },
            { "end-partition-interpreter", ("elapsed", 1) },
            { "end-prepare-primary-mapping-table", ("elapsed", 1) },
            { "end-prepare-quick-filter-queries", ("elapsed", 1) },
            { "end-query", ("elapsed", 1) },
            { "end-sql-temp-table-tuples-create", ("elapsed", 1) },
            { "end-update-sheet", ("elapsed", 1) },
            { "end-visual-interpreter", ("elapsed", 1) },
            { "end-visual-model-producer", ("elapsed", 1) },
            { "eqc-load", ("elapsed-ms", 1000) },
            { "eqc-store", ("elapsed-ms", 1000) },
            { "generate-axis-encodings", ("elapsed", 1) },
            { "get-cached-query", ("elapsed-ms", 1000) },
            { "process_query", ("elapsed", 1) },
            { "qp-batch-summary", ("elapsed", 1) },
            { "qp-query-end", ("elapsed", 1) },
            { "vmp-generate-axis-encodings", ("elapsed", 1) },
        };

        private class SessionTracker
        {
            private readonly Dictionary<string, string> _fileToSessionIdMap = new Dictionary<string, string>();

            public void RegisterSession(string filePath, string sessionId)
            {
                if (_fileToSessionIdMap.ContainsKey(filePath))
                {
                    _fileToSessionIdMap[filePath] = sessionId;
                }
                else
                {
                    _fileToSessionIdMap.Add(filePath, sessionId);
                }
            }
            
            public string GetSessionId(string filePath)
            {
                return _fileToSessionIdMap.ContainsKey(filePath)
                    ? _fileToSessionIdMap[filePath]
                    : null;
            }
        }
    }
}