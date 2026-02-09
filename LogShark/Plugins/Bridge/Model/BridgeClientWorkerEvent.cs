using System;
using System.Text.Json.Serialization;
using LogShark.Shared.LogReading.Containers;
using Newtonsoft.Json;

namespace LogShark.Plugins.Bridge.Model
{
    public class BridgeClientWorkerEvent : BaseEvent
    {
        public new DateTime StartTime { get; }
        public string Severity { get; }
        public string RequestId { get; }
        public string Command { get; }
       public string DBServerUri {  get; }
        public string RequestID { get; }
        public string RQSSessionId {  get; }
        public string ServerSessionId { get; }

        public DateTime EndTime { get; set; }

        public string Type { get; set; }

        public double TimeTakenSeconds { get; set; }

        public bool EventEnded { get; set; }

        public BridgeClientWorkerEvent(LogLine logLine, NativeJsonLogsBaseEvent baseEvent) 
            : base(logLine, baseEvent.Timestamp)
        {
           
            Severity = baseEvent.Severity;
            RequestId = string.IsNullOrEmpty(baseEvent.RequestId)? null : baseEvent.RequestId;

                var Value = baseEvent.EventPayload?.ToString(Formatting.None);
                var payloadJson = Value.ToString();
                var payload = JsonConvert.DeserializeObject<dynamic>(payloadJson);

                     Command = payload?["command"]?.ToString() ?? null;
                     DBServerUri = payload?["dbServerUri"]?.ToString() ?? null;
                     RequestID = payload?["requestID"]?.ToString() ?? null;
                     RQSSessionId = payload?["rqsSessionId"]?.ToString() ?? null;
                     ServerSessionId = payload?["serverSessionId"]?.ToString() ?? null;
                     Type = payload?["type"]?.ToString() ?? null;

            if (Type == "begin-live-query-dispatch")
            {
                StartTime = baseEvent.Timestamp;
            }
            else if (Type == "finish-live-query-dispatch")
            {
                EndTime = baseEvent.Timestamp;
            }
            
        }

        public string getType(string payloadJson)
        {
            var payload = JsonConvert.DeserializeObject<dynamic>(payloadJson);
            string type = payload?["type"]?.ToString() ?? null;
            return type;
        }
       
       

    }
} 