using LogShark.Shared.LogReading.Containers;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Threading;
using Tableau.HyperAPI;
using YamlDotNet.Core.Tokens;

namespace LogShark.Plugins.Bridge.Model
{
    public class ProtocolQueryEvent : BaseEvent
    {

        public new DateTime Timestamp { get; set; }


        public string RequestID { get; set; }
        public string ISCommand { get; set; }
        public string Query {  get; set; }
        public string QueryCategory {  get; set; }
        public long QueryHash {  get; set; }
        public string QueryRootTid {  get; set; }
        public string QueryTags {  get; set; }
        public long Cols { get; set; }
        public double Elapsed {  get; set; }
        public string ProtocolClass {  get; set; }
        public int ProtocolID { get; set; }
        public string QueryTrunc { get; set; }

        public long Rows { get; set; }

        public bool EventEnded { get; set; }

   
  




    public ProtocolQueryEvent(LogLine logLine, NativeJsonLogsBaseEvent baseEvent)
            : base(logLine, baseEvent.Timestamp)
        {
            try
            {
                Timestamp = baseEvent.Timestamp;
                RequestID = string.IsNullOrEmpty(baseEvent.RequestId) || baseEvent.RequestId == "-" ? null : baseEvent.RequestId;
                var Value = baseEvent.EventPayload?.ToString(Formatting.None);
                //Extract items from Value. Key remains liverquery-metrics for this log
                var payloadJson = Value.ToString();
                var payload = JsonConvert.DeserializeObject<dynamic>(payloadJson);
                ISCommand = payload?["is-command"]?.ToString() ?? null;
                Query = payload?["query"]?.ToString() ?? null;
                QueryCategory = payload?["query-category"]?.ToString() ?? null;
                QueryHash = long.Parse(payload?["query-hash"]?.ToString()) ?? null;
                QueryRootTid = payload?["query-root-tid"]?.ToString() ?? null;
                QueryTags = payload?["query-tags"]?.ToString() ?? null;
            }
            catch (Exception e)
            {
                //Why is this null?
            }

        }
       
        public bool endProtocolEvent(NativeJsonLogsBaseEvent baseEvent)
        {
    
            var Value = baseEvent.EventPayload?.ToString(Formatting.None);
            var payloadJson = Value.ToString();
            var payload = JsonConvert.DeserializeObject<dynamic>(payloadJson);

            long queryHash = long.Parse(payload?["query-hash"]?.ToString()) ?? null;
            if (queryHash == QueryHash)
            {
                Cols = long.Parse(payload?["cols"]?.ToString()) ?? null;
                Elapsed = double.Parse(payload?["elapsed"]?.ToString()) ?? null;
                bool isCommand = bool.Parse(payload?["is-command"]?.ToString()) ?? null;
                ProtocolClass = payload?["protocol-class"]?.ToString() ?? null;
                ProtocolID = int.Parse(payload?["protocol-id"]?.ToString()) ?? null;
                string queryCategory = payload?["query-category"]?.ToString() ?? null;

                string queryRootTid = payload?["query-root-tid"]?.ToString() ?? null;
                string queryTags = payload?["query-tags"]?.ToString() ?? null;
                QueryTrunc = payload?["query-trunc"]?.ToString() ?? null;
                Rows = long.Parse(payload?["rows"]?.ToString()) ?? 0;
               return  true;
            }
            return false;
          
        }

    }
}
