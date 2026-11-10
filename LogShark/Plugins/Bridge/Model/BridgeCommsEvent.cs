using LogShark.Shared.LogReading.Containers;
using Newtonsoft.Json.Linq;
using System;

namespace LogShark.Plugins.Bridge.Model
{
    public class BridgeCommsEvent : BaseEvent
    {
        public string Channel { get; }
        public string Type { get; }
        public string Id { get; }
        public string BodyType { get; }
        public int? TotalCapacity { get; }
        public int? NumActiveJobs { get; }
        public long? AgentMachineDiagnosticsTimestamp { get; }

        public BridgeCommsEvent(LogLine logLine, NativeJsonLogsBaseEvent baseEvent, JObject dataSyncMessage)
            : base(logLine, baseEvent.Timestamp)
        {
            Channel = dataSyncMessage["channel"]?.ToString();
            Type = dataSyncMessage["type"]?.ToString();
            Id = dataSyncMessage["id"]?.ToString();

            var body = dataSyncMessage["body"] as JObject;
            if (body != null)
            {
                BodyType = body["type"]?.ToString();

                var attrs = body["attrs"] as JObject;
                if (attrs != null)
                {
                    if (int.TryParse(attrs["totalCapacity"]?.ToString(), out var totalCap))
                    {
                        TotalCapacity = totalCap;
                    }

                    if (int.TryParse(attrs["numActiveJobs"]?.ToString(), out var numJobs))
                    {
                        NumActiveJobs = numJobs;
                    }

                    // Parse nested JSON string in agentMachineDiagnostics
                    var diagnosticsStr = attrs["agentMachineDiagnostics"]?.ToString();
                    if (!string.IsNullOrEmpty(diagnosticsStr))
                    {
                        try
                        {
                            var diagnostics = JObject.Parse(diagnosticsStr);
                            if (long.TryParse(diagnostics["timestamp"]?.ToString(), out var timestamp))
                            {
                                AgentMachineDiagnosticsTimestamp = timestamp;
                            }
                        }
                        catch (Newtonsoft.Json.JsonException)
                        {
                            // Malformed nested JSON - skip silently
                        }
                    }
                }
            }
        }
    }
}
