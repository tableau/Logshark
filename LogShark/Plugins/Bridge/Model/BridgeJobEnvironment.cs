using System;
using Newtonsoft.Json;

namespace LogShark.Plugins.Bridge.Model
{
    public class BridgeJobEnvironment
    {
        // Core job identification
        public string JobId { get; set; }
        
        // Startup info fields
        public string Domain { get; set; }
        public string Hostname { get; set; }
        public string OS { get; set; }
        public string ProcessId { get; set; }
        public string StartTime { get; set; }
        public string TableauVersion { get; set; }
        
        // Environment fields
        public string Username { get; set; }
        
        // CPU info
        public string CpuName { get; set; }
        
        // Proxy settings
        public bool? EnableProxyCookie { get; set; }

        public BridgeJobEnvironment(string jobId)
        {
            JobId = jobId?.Trim('{', '}').ToLowerInvariant() ?? jobId;
        }

        public void UpdateFromStartupInfo(object eventPayload)
        {
            try
            {
                var payloadJson = eventPayload.ToString();
                var payload = JsonConvert.DeserializeObject<dynamic>(payloadJson);
                
                Domain = payload?["domain"]?.ToString() ?? null;
                Hostname = payload?["hostname"]?.ToString() ?? null;
                OS = payload?["os"]?.ToString() ?? null;
                ProcessId = payload?["process-id"]?.ToString() ?? null;
                StartTime = payload?["start-time"]?.ToString() ?? null;
                TableauVersion = payload?["tableau-version"]?.ToString() ?? null;
            }
            catch
            {
                // Ignore parsing errors
            }
        }

        public void UpdateFromEnvironment(object eventPayload)
        {
            try
            {
                var payloadJson = eventPayload.ToString();
                var payload = JsonConvert.DeserializeObject<dynamic>(payloadJson);
                
                Username = payload?["USERNAME"]?.ToString() ?? null;
            }
            catch
            {
                // Ignore parsing errors
            }
        }

        public void UpdateFromCpuMemoryInfo(object eventPayload)
        {
            try
            {
                var payloadJson = eventPayload.ToString();
                var payload = JsonConvert.DeserializeObject<dynamic>(payloadJson);
                
                CpuName = payload?["cpu-name"]?.ToString() ?? CpuName;
            }
            catch
            {
                // Ignore parsing errors
            }
        }

        public void UpdateFromEnableProxyCookie(object eventPayload)
        {
            try
            {
                // msg events contain plain text messages, not JSON
                var message = eventPayload?.ToString();
                if (!string.IsNullOrEmpty(message))
                {
                    var match = System.Text.RegularExpressions.Regex.Match(message, @"EnableProxyCookie:\s*(true|false)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                    if (match.Success)
                    {
                        if (bool.TryParse(match.Groups[1].Value, out bool proxyValue))
                        {
                            EnableProxyCookie = proxyValue;
                        }
                    }
                }
            }
            catch
            {
                // Ignore parsing errors
            }
        }

        public void UpdateFromAnyEvent(string eventType, object eventPayload)
        {
            // Only process environment-related events
            if (!IsEnvironmentEvent(eventType) || eventPayload == null)
            {
                return;
            }

            try
            {
                // Check event type and update accordingly
                switch (eventType?.ToLowerInvariant())
                {
                    case "startup-info":
                        UpdateFromStartupInfo(eventPayload);
                        break;
                    case "environment":
                        UpdateFromEnvironment(eventPayload);
                        break;
                    case "cpu-memory-info":
                        UpdateFromCpuMemoryInfo(eventPayload);
                        break;
                    case "memory-usage":
                        // Memory usage events don't typically contain environment data we need
                        break;
                case "msg":
                    // Check if this is an EnableProxyCookie message
                    var message = eventPayload?.ToString();
                    if (!string.IsNullOrEmpty(message) && message.ToLowerInvariant().Contains("enableproxycookie"))
                    {
                        UpdateFromEnableProxyCookie(eventPayload);
                    }
                    break;
                }
            }
            catch
            {
                // Ignore parsing errors
            }
        }

        private bool IsEnvironmentEvent(string eventType)
        {
            return eventType == "startup-info" || 
                   eventType == "environment" || 
                   eventType == "cpu-memory-info" || 
                   eventType == "memory-usage" ||
                   eventType == "msg";
        }
    }
}
