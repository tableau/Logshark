using LogShark.Shared.LogReading.Containers;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Threading;
using Tableau.HyperAPI;
using YamlDotNet.Core.Tokens;

namespace LogShark.Plugins.Bridge.Model
{
    public class LiveQueryMetricsEvent : BaseEvent
    {

        public new DateTime Timestamp { get; }
        public DateTime Arrived { get; set; }
        public DateTime Completed { get; set; }
        public DateTime Dequeued { get; set; }

        public long DurationMS { get; set; }
        public long ProcessingTimeMS { get; set; }
        public bool TimedOut { get; set; }
        public string Type { get; set; }
        public string DBSrvUri { get; set; }
        public int RequestsPending { get; set; }
        public string RQSSession { get; set; }
        public string RequestID { get; set; }
        public int TotalActive { get; set; }
        public int PID { get; set; }
        public string TID { get; set; }



        public LiveQueryMetricsEvent(LogLine logLine, NativeJsonLogsBaseEvent baseEvent)
            : base(logLine, baseEvent.Timestamp)
        {
            Timestamp = baseEvent.Timestamp;
            RequestID = string.IsNullOrEmpty(baseEvent.RequestId) || baseEvent.RequestId == "-" ? null : baseEvent.RequestId;
            var Key = baseEvent.EventType;
            var Value = baseEvent.EventPayload?.ToString(Formatting.None);
            PID = baseEvent.ProcessId;
            TID = baseEvent.ThreadId;

            //Extract items from Value. Key remains liverquery-metrics for this log
            var payloadJson = Value.ToString();
            var payload = JsonConvert.DeserializeObject<dynamic>(payloadJson);
            Arrived = DateTime.Parse( payload?["Arrived"]?.ToString()) ??  null;
            Completed = DateTime.Parse(payload?["Completed"]?.ToString()) ?? null;
            Dequeued = DateTime.Parse(payload?["Dequeued"]?.ToString()) ?? null;
            DurationMS = long.Parse(payload?["Duration (ms)"]?.ToString()) ?? null;
            ProcessingTimeMS = long.Parse(payload?["ProcessingTime (ms)"]?.ToString()) ?? null;
            TimedOut = bool.Parse( payload?["Timed-out"]?.ToString()) ?? null;
            Type = payload?["Type"]?.ToString() ?? null;
            DBSrvUri = payload?["dbSrvUri"]?.ToString() ?? null;
            RequestsPending = int.Parse(payload?["requestsPending"]?.ToString()) ?? null;
            RQSSession = payload?["rqsSession"]?.ToString() ?? null;
            TotalActive = int.Parse(payload?["total-active"]?.ToString()) ?? null;
        }

    }
}
