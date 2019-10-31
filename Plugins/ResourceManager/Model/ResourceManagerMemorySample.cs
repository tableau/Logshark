using Logshark.PluginLib.Helpers;
using MongoDB.Bson;
using System;
using System.Text.RegularExpressions;

namespace Logshark.Plugins.ResourceManager.Model
{
    public class ResourceManagerMemorySample : ResourceManagerEvent
    {
        // Gets byte counts for current & total utilization by greedily capturing numeric sequences (composed of digits and comma separators) from between the ":" & "bytes" and ";" & "bytes" token pairs.
        private static readonly Regex CurrentAndTotalMemoryUtilRegex = new Regex(@".*: (?<current_process_util>[\d,]+?) bytes.*; (?<tableau_total_util>[\d,]+?) bytes", RegexOptions.Compiled | RegexOptions.ExplicitCapture);

        public long ProcessMemoryUtil { get; set; }
        public long TotalMemoryUtil { get; set; }

        public ResourceManagerMemorySample()
        {
        }

        public ResourceManagerMemorySample(BsonDocument memorySample, string processName)
            : base(memorySample, processName)
        {
            SetProcessAndTotalMemoryUtil(memorySample);
        }

        private void SetProcessAndTotalMemoryUtil(BsonDocument memorySample)
        {
            try
            {
                string utilString = BsonDocumentHelper.GetString("v", memorySample);

                var currentAndTotalMatch = CurrentAndTotalMemoryUtilRegex.Match(utilString);
                if (!currentAndTotalMatch.Success)
                {
                    throw new Exception("Could not gather memory information from logline: " + utilString);
                }

                ProcessMemoryUtil = Int64.Parse(currentAndTotalMatch.Groups["current_process_util"].Value.Replace(",", ""));
                TotalMemoryUtil = Int64.Parse(currentAndTotalMatch.Groups["tableau_total_util"].Value.Replace(",", ""));
            }
            catch (Exception ex)
            {
                throw new Exception("Could not gather memory information from logline " + memorySample, ex);
            }
        }
    }
}