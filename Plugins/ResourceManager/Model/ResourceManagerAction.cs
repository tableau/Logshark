using Logshark.PluginLib.Helpers;
using MongoDB.Bson;
using System;
using System.Text.RegularExpressions;

namespace Logshark.Plugins.ResourceManager.Model
{
    public class ResourceManagerAction : ResourceManagerEvent
    {
        // Extract an optionally comma-separated numeric total memory byte count value (and optional process memory byte count) from the end of a static Resource Manager string
        public static Regex TotalMemoryUsageExceededRegex = new Regex(@"Resource Manager: Exceeded allowed memory usage across all processes\D+\s(?<total_usage>[\d,]+?)\s(\D+\s(?<process_usage>[\d,]+?)\s)?", RegexOptions.Compiled | RegexOptions.ExplicitCapture);

        // Extract an optionally comma-separated numeric process memory byte count value from the end of a static Resource Manager string
        public static Regex ProcessMemoryUsageExceededRegex = new Regex(@"^Resource Manager: Exceeded allowed memory usage per process.\s(?<process_usage>[\d,]+?)\s.*", RegexOptions.Compiled | RegexOptions.ExplicitCapture);

        public static Regex CpuUsageExceededRegex = new Regex(@"^Resource Manager: Exceeded sustained high CPU threshold above\s(?<cpu_threshold>\d+?)%.*\s(?<duration>\d+?)\s.*\s(?<process_cpu_util>\d+?)%", RegexOptions.Compiled | RegexOptions.ExplicitCapture);

        public bool CpuUtilTermination { get; set; }
        public int? CpuUtil { get; set; }
        public bool ProcessMemoryUtilTermination { get; set; }
        public long? ProcessMemoryUtil { get; set; }
        public bool TotalMemoryUtilTermination { get; set; }
        public long? TotalMemoryUtil { get; set; }

        public ResourceManagerAction()
        {
        }

        public ResourceManagerAction(BsonDocument actionEvent, string processName)
            : base(actionEvent, processName)
        {
            ParseActionEvent(actionEvent);
        }

        private void ParseActionEvent(BsonDocument actionEvent)
        {
            try
            {
                string utilString = BsonDocumentHelper.GetString("v", actionEvent);
                var cpuUsageMatch = CpuUsageExceededRegex.Match(utilString);
                var processMemoryUsageMatch = ProcessMemoryUsageExceededRegex.Match(utilString);
                var totalMemoryUsageMatch = TotalMemoryUsageExceededRegex.Match(utilString);
                if (cpuUsageMatch.Success)
                {
                    CpuUtil = Int32.Parse(cpuUsageMatch.Groups["process_cpu_util"].Value);
                    CpuUtilTermination = true;
                }
                else if (processMemoryUsageMatch.Success)
                {
                    ProcessMemoryUtil = Int64.Parse(processMemoryUsageMatch.Groups["process_usage"].Value.Replace(",", ""));
                    ProcessMemoryUtilTermination = true;
                }
                else if (totalMemoryUsageMatch.Success)
                {
                    TotalMemoryUtil = Int64.Parse(totalMemoryUsageMatch.Groups["total_usage"].Value.Replace(",", ""));
                    TotalMemoryUtilTermination = true;
                }
                else
                {
                    throw new Exception("Match not found!");
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Could not gather Resource Manager action information from logline " + actionEvent, ex);
            }
        }
    }
}