using System;
using System.Collections.Generic;
using LogShark.Shared.LogReading.Containers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LogShark.Plugins.Bridge.Model
{
    public class BridgeClientEvent : BaseEvent
    {
        public new DateTime Timestamp { get; }
        public string Severity { get; }
        public string RequestId { get; }
        public string SessionId { get; }
        public string Site { get; }
        public string User { get; }
        public string TraceId { get; }
        public string Key { get; }
        public string Value { get; }
        public string Message { get; }
        public int ProcessId { get; }
        public string ThreadId { get; }

        // Single config key-value pair (one per event/row)
        public string ConfigKey { get; }
        public string ConfigValue { get; }

        public BridgeClientEvent(LogLine logLine, NativeJsonLogsBaseEvent baseEvent, string configKey = null, string configValue = null) 
            : base(logLine, baseEvent.Timestamp)
        {
            Timestamp = baseEvent.Timestamp;
            Severity = baseEvent.Severity;
            RequestId = string.IsNullOrEmpty(baseEvent.RequestId) || baseEvent.RequestId == "-" ? null : baseEvent.RequestId;
            SessionId = string.IsNullOrEmpty(baseEvent.SessionId) || baseEvent.SessionId == "-" ? null : baseEvent.SessionId;
            Site = string.IsNullOrEmpty(baseEvent.Site) || baseEvent.Site == "-" ? null : baseEvent.Site;
            User = string.IsNullOrEmpty(baseEvent.Username) || baseEvent.Username == "-" ? null : baseEvent.Username;
            Key = baseEvent.EventType;
            
            // Get the raw string value from EventPayload
            // If EventPayload is a JValue (string), get the value directly to avoid escaping issues
            if (baseEvent.EventPayload != null && baseEvent.EventPayload.Type == JTokenType.String)
            {
                Value = baseEvent.EventPayload.Value<string>();
            }
            else
            {
                Value = baseEvent.EventPayload?.ToString(Formatting.None);
            }
            Message = $"{Key}: {Value}";
            ProcessId = baseEvent.ProcessId;
            ThreadId = baseEvent.ThreadId;
            
            TraceId = ExtractTraceIdFromValue(Value);
            
            // Set the config key-value pair (passed from plugin)
            ConfigKey = configKey;
            ConfigValue = configValue;
        }
        
        private static string ExtractTraceIdFromValue(string value)
        {
            return null;
        }

        public static Dictionary<string, string> ExtractAndFlattenJson(string valueString)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (string.IsNullOrEmpty(valueString)) return result;

            try
            {
               
                    // Find the first unescaped opening brace
                    var jsonStart = -1;
                    for (int i = 0; i < valueString.Length; i++)
                    {
                        if (valueString[i] == '{' && (i == 0 || valueString[i - 1] != '\\'))
                        {
                            jsonStart = i;
                            break;
                        }
                    }
                    
                    if (jsonStart >= 0)
                    {
                        var jsonEnd = FindMatchingBrace(valueString, jsonStart);
                        if (jsonEnd > jsonStart)
                        {
                            var jsonStr = valueString.Substring(jsonStart, jsonEnd - jsonStart + 1);
                            jsonStr = jsonStr.Trim();
                            
                            // Validate it looks like JSON
                            if (!string.IsNullOrEmpty(jsonStr) && jsonStr.StartsWith("{") && jsonStr.EndsWith("}"))
                            {
                                var configJson = JToken.Parse(jsonStr);
                                
                                // Flatten the JSON
                                FlattenToken(configJson, result, "");
                            }
                        }
                    }
                
            }
            catch
            {
                // Return empty dictionary on error
            }

            return result;
        }

      

        private static int FindMatchingBrace(string json, int startIndex)
        {
            int depth = 0;
            for (int i = startIndex; i < json.Length; i++)
            {
                if (json[i] == '{') depth++;
                else if (json[i] == '}') depth--;
                if (depth == 0) return i;
            }
            return -1;
        }

        private static void FlattenToken(JToken token, Dictionary<string, string> result, string prefix)
        {
            if (token == null) return;

            // Handle JObject explicitly
            if (token is JObject obj)
            {
                foreach (var prop in obj.Properties())
                {
                    var newPrefix = string.IsNullOrEmpty(prefix) ? prop.Name : $"{prefix}.{prop.Name}";
                    FlattenToken(prop.Value, result, newPrefix);
                }
                return;
            }

            // Handle arrays - skip them
            if (token is JArray)
            {
                return;
            }

            // Everything else is a leaf value
            if (!string.IsNullOrEmpty(prefix))
            {
                result[prefix] = token.ToString();
            }
        }

    }
}
