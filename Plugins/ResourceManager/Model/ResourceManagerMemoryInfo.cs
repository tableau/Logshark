using Logshark.PluginLib.Helpers;
using MongoDB.Bson;
using System;
using System.Text.RegularExpressions;

namespace Logshark.Plugins.ResourceManager.Model
{
    public class ResourceManagerMemoryInfo : ResourceManagerEvent
    {
        private static readonly Regex CurrentAndTotalMemoryUtilRegex = new Regex(@".*\s(?<current_process_util>\d+?)\s.*;\s(?<tableau_total_util>\d+?)\s.*", RegexOptions.Compiled | RegexOptions.ExplicitCapture);

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
                    ProcessMemoryUtil = Int64.Parse(currentAndTotalMatch.Groups["current_process_util"].Value);
                    TotalMemoryUtil = Int64.Parse(currentAndTotalMatch.Groups["tableau_total_util"].Value);
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