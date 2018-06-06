using Logshark.PluginLib.Helpers;
using MongoDB.Bson;
using System;
using System.Text.RegularExpressions;

namespace Logshark.Plugins.ResourceManager.Model
{
    public class ResourceManagerMemoryInfo : ResourceManagerEvent
    {
        // Gets byte counts for current & total utilization by greedily capturing numeric sequences (composed of digits and comma separators) from between the ":" & "bytes" and ";" & "bytes" token pairs.
        private static readonly Regex CurrentAndTotalMemoryUtilRegex = new Regex(@".*: (?<current_process_util>[\d,]+?) bytes.*; (?<tableau_total_util>[\d,]+?) bytes", RegexOptions.Compiled | RegexOptions.ExplicitCapture);

        public long ProcessMemoryUtil { get; set; }
        public long TotalMemoryUtil { get; set; }

        public ResourceManagerMemoryInfo()
        {
        }

        public ResourceManagerMemoryInfo(BsonDocument memoryInfoEvent, string processName)
            : base(memoryInfoEvent, processName)
        {
            SetProcessAndTotalMemoryUtil(memoryInfoEvent);
        }

        private void SetProcessAndTotalMemoryUtil(BsonDocument cpuInfoEvent)
        {
            try
            {
                string utilString = BsonDocumentHelper.GetString("v", cpuInfoEvent);
                var currentAndTotalMatch = CurrentAndTotalMemoryUtilRegex.Match(utilString);
                if (currentAndTotalMatch.Success)
                {
                    ProcessMemoryUtil = Int64.Parse(currentAndTotalMatch.Groups["current_process_util"].Value.Replace(",", ""));
                    TotalMemoryUtil = Int64.Parse(currentAndTotalMatch.Groups["tableau_total_util"].Value.Replace(",", ""));
                }
                else
                {
                    throw new Exception("Could not gather memory information from logline: " + utilString);
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Could not gather memory information from logline " + cpuInfoEvent, ex);
            }
        }
    }
}