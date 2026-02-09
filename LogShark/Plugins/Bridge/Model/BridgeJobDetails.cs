using System;
using LogShark.Shared.LogReading.Containers;
using Newtonsoft.Json;

namespace LogShark.Plugins.Bridge.Model
{
    public class BridgeJobDetails
    {
        public string JobId { get; set; }
        public DateTime? FirstSeen { get; set; }
        public DateTime? LastSeen { get; set; }
        public DateTime? JobStartTime { get; set; }
        public DateTime? JobEndTime { get; set; }
        public DateTime? AddingJobToQueueTime { get; set; }
        public DateTime? AddedJobToQueueTime { get; set; }
        public DateTime? DispatchingJobTime { get; set; }
        public DateTime? CLIProcessLaunchTime { get; set; }
        public string DataSourceUri { get; set; }
        public string ErrorType { get; set; }
        public string State { get; set; }
        public string RefreshType { get; set; }
        public string TokenID { get; set; }
        public string RetryCount { get; set; }
        public string ProcessId { get; set; }
        public string ThreadId { get; set; }
        public string HostName { get; set; }
        public DateTime Timestamp { get; set; }

        public BridgeJobDetails(string jobId, DateTime timestamp, int processId, string threadId)
        {
            JobId = jobId;
            ProcessId = processId.ToString();
            ThreadId = threadId;
            Timestamp = timestamp;
        }
        public bool getHostDetails(object eventPayload, DateTime timestamp, string eventTypeParam = null)
        {
            return true;
        }
        public bool UpdateFromAnyEvent(object eventPayload, DateTime timestamp, string folder,  string eventTypeParam = null )
        {
            try
            {
               
                if (HostName == null) { HostName = folder; }
                if (eventPayload == null) return false;
                
                string messageText = "";
                string eventJobId = "";
                
                // EventPayload is a JToken, try to cast to JObject
                var payload = eventPayload as Newtonsoft.Json.Linq.JObject;
                if (payload != null)
                {
                    
                    // Get message text from payload["msg"]
                    var msgToken = payload["msg"];
                    messageText = msgToken?.ToString() ?? "";
                    
                // Check if this event contains our jobID (either in the jobID field or in the message text)
                var jobIdToken = payload["jobID"];
                eventJobId = jobIdToken?.ToString() ?? "";
                
                // Also check if the event type contains our jobID (for events like "job-send-request-{jobID}")
                if (string.IsNullOrEmpty(eventJobId) && !string.IsNullOrEmpty(eventTypeParam))
                {
                    var eventTypeJobIdMatch = System.Text.RegularExpressions.Regex.Match(eventTypeParam, @"([a-f0-9]{8}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{12})", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                    if (eventTypeJobIdMatch.Success)
                    {
                        eventJobId = eventTypeJobIdMatch.Groups[1].Value;
                    }
                }
                }
                else
                {
                    // If EventPayload is not a JObject, try to parse it as JSON string
                    var payloadString = eventPayload.ToString();
                    
                    
                    if (!string.IsNullOrEmpty(payloadString))
                    {
                        try
                        {
                            var parsedPayload = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(payloadString);
                            if (parsedPayload != null)
                            {
                                messageText = parsedPayload.msg?.ToString() ?? "";
                                eventJobId = parsedPayload.jobID?.ToString() ?? "";
                                
                                // Also check if the event type contains our jobID
                                if (string.IsNullOrEmpty(eventJobId) && !string.IsNullOrEmpty(eventTypeParam))
                                {
                                    var eventTypeJobIdMatch = System.Text.RegularExpressions.Regex.Match(eventTypeParam, @"([a-f0-9]{8}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{12})", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                                    if (eventTypeJobIdMatch.Success)
                                    {
                                        eventJobId = eventTypeJobIdMatch.Groups[1].Value;
                                    }
                                }
                            }
                            return false;
                        }
                        catch (JsonException)
                        {
                            // If JSON parsing fails, try regex on the raw string
                            var jobIdMatch = System.Text.RegularExpressions.Regex.Match(payloadString, @"""jobID""\s*:\s*""([a-f0-9-]+)""", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                            if (jobIdMatch.Success)
                            {
                                eventJobId = jobIdMatch.Groups[1].Value;
                            }
                            
                            var msgMatch = System.Text.RegularExpressions.Regex.Match(payloadString, @"""msg""\s*:\s*""([^""]*)""", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                            if (msgMatch.Success)
                            {
                                messageText = msgMatch.Groups[1].Value;
                            }
                            return false;
                        }
                    }
                }
                
                bool containsJobId = false;
                if (!string.IsNullOrEmpty(eventJobId))
                {
                    // Clean the event jobID to remove braces and normalize case
                    var cleanEventJobId = eventJobId.Trim('{', '}', '"', '\'').ToLowerInvariant();
                    var cleanJobId = JobId.ToLowerInvariant();
                    containsJobId = string.Equals(cleanEventJobId, cleanJobId, StringComparison.OrdinalIgnoreCase);
                }
                else
                {
                    containsJobId = messageText.Contains(JobId, StringComparison.OrdinalIgnoreCase);
                }
                
                // If this event doesn't contain our jobID, skip it
                if (!containsJobId)
                {
                    return false;
                }
                
                
                // Update FirstSeen and LastSeen timestamps
                if (FirstSeen == null)
                {
                    FirstSeen = timestamp;
                }
                LastSeen = timestamp;
                
                // Extract data from message text patterns
                
                
                // JobStartTime and JobEndTime - check the payload for nested JSON structure
                if (payload != null)
                {
                    var messageType = payload["message-type"]?.ToString();
                    if (messageType == "response-status")
                    {
                        var statuses = payload["statuses"];
                        if (statuses != null)
                        {
                            foreach (var status in statuses)
                            {
                                var statusObj = status as Newtonsoft.Json.Linq.JProperty;
                                if (statusObj?.Value is Newtonsoft.Json.Linq.JObject statusValue)
                                {
                                    var state = statusValue["state"]?.ToString();
                                    State = state; //stores the value
                                    if (state == "RefreshStarted")
                                    {
                                        JobStartTime = timestamp;
                                        
                                    }
                                    else if (state == "RefreshCompleted")
                                    {
                                        JobEndTime = timestamp;
                                        
                                    }
                                    else if (state == "RefreshFailed")
                                    {
                                        JobEndTime = timestamp;
                                        
                                    }
                                    else //We do not know when this job is ended, so we assume the same time as started time.
                                    {
                                        JobEndTime = JobStartTime;
                                    }
                                }
                            }
                        }
                    }
                    else if (messageType == "request-legacy")
                    {
                        //currently this is where the jobid stops appearing. This needs to be improved.
                        return true;
                    }
                }
               
                // Fallback: also check message text for these patterns
                if (messageText.Contains("state=RefreshStarted"))
                {
                    JobStartTime = timestamp;
                    return false;
                }
                if (messageText.Contains("state=RefreshCompleted"))
                {
                    JobEndTime = timestamp;
                    return false;
                }
                
                // AddingJobToQueueTime - look for "Adding remote job requst to queue" (note the typo in logs)
                if (messageText.Contains("Adding remote job requst to queue"))
                {
                    AddingJobToQueueTime = timestamp;
                    return false;
                }
                
                // AddedJobToQueueTime - look for "Added remote job requst to queue" (note the typo in logs)
                if (messageText.Contains("Added remote job requst to queue"))
                {
                    AddedJobToQueueTime = timestamp;
                    return false;
                }
                
                // DispatchingJobTime - look for "Dispatching remote job request"
                // Also extract DataSourceUri from this event
                if (messageText.Contains("Dispatching remote job request"))
                {
                    DispatchingJobTime = timestamp;
                    
                    // Extract DataSourceUri from the payload if present
                    if (payload != null && string.IsNullOrEmpty(DataSourceUri))
                    {
                        var dataSourceUriToken = payload["dataSourceUri"];
                        if (dataSourceUriToken != null)
                        {
                            DataSourceUri = dataSourceUriToken.ToString();
                        }
                    }
                    return false;
                }
                
                // CLIProcessLaunchTime - look for "Launching the CLI process"
                if (messageText.Contains("Launching the CLI process"))
                {
                    CLIProcessLaunchTime = timestamp;
                    return false;
                }

                
                // RefreshType - look for "refresh-type" in payload or messageText
                if (payload != null && string.IsNullOrEmpty(RefreshType))
                {
                    var refreshTypeToken = payload["refresh-type"];
                    if (refreshTypeToken != null)
                    {
                        RefreshType = refreshTypeToken.ToString();
                    }
                    return false;
                }

                
                // TokenID - look for "Assigning token"
                if (string.IsNullOrEmpty(TokenID) && messageText.Contains("Assigning token"))
                {
                    var tokenMatch = System.Text.RegularExpressions.Regex.Match(messageText, @"Assigning token\s*'\{([^}]+)\}'", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                    if (tokenMatch.Success)
                    {
                        TokenID = tokenMatch.Groups[1].Value;
                    }
                    return false;
                }
                
                // RetryCount - look for "retryCount"
                if (string.IsNullOrEmpty(RetryCount) && messageText.Contains("retryCount"))
                {
                    var retryMatch = System.Text.RegularExpressions.Regex.Match(messageText, @"retryCount[:\s]+(\d+)");
                    if (retryMatch.Success)
                    {
                        RetryCount = retryMatch.Groups[1].Value;
                    }
                    return false;
                }
                
                // ProcessId - look for "PID="
                if (messageText.Contains("PID="))
                {
                    var pidMatch = System.Text.RegularExpressions.Regex.Match(messageText, @"PID[=:\s]+(\d+)");
                    if (pidMatch.Success)
                    {
                        ProcessId = pidMatch.Groups[1].Value;
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                // Log the exception to help with debugging
                System.Diagnostics.Debug.WriteLine($"Error in UpdateFromAnyEvent: {ex.Message}");
                return false;
            }
        }
    }
}
