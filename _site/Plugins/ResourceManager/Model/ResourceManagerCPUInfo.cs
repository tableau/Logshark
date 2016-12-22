﻿using Logshark.PluginLib.Helpers;
using MongoDB.Bson;
using System;
using System.Text.RegularExpressions;

namespace Logshark.Plugins.ResourceManager.Model
{
    public class ResourceManagerCpuInfo : ResourceManagerEvent
    {
        private static readonly Regex CurrentAndTotalCpuUtilRegex = new Regex(@".*\s(?<current_process_util>\d+?)%.*\s(?<total_processes_util>\d+?)%.*", RegexOptions.Compiled | RegexOptions.ExplicitCapture);
        private static readonly Regex CurrentCpuUtilRegex = new Regex(@".*\s(?<current_process_util>\d+?)%.*", RegexOptions.Compiled | RegexOptions.ExplicitCapture);

        public int ProcessCpuUtil { get; set; }

        //We only get this in the logs when the process CPU Util value is greater than zero, for some reason.
        public int? TotalCpuUtil { get; set; }

        public ResourceManagerCpuInfo()
        {
        }

        public ResourceManagerCpuInfo(BsonDocument cpuInfoEvent, string processName)
            : base(cpuInfoEvent, processName)
        {
            SetProcessAndTotalCpuUtil(cpuInfoEvent);
        }

        private void SetProcessAndTotalCpuUtil(BsonDocument cpuInfoEvent)
        {
            try
            {
                string utilString = BsonDocumentHelper.GetString("v", cpuInfoEvent);
                var currentAndTotalMatch = CurrentAndTotalCpuUtilRegex.Match(utilString);
                var currentMatch = CurrentCpuUtilRegex.Match(utilString);
                if (currentAndTotalMatch.Success)
                {
                    ProcessCpuUtil = Int32.Parse(currentAndTotalMatch.Groups["current_process_util"].Value);
                    TotalCpuUtil = Int32.Parse(currentAndTotalMatch.Groups["total_processes_util"].Value);
                }
                else if (currentMatch.Success)
                {
                    ProcessCpuUtil = Int32.Parse(currentMatch.Groups["current_process_util"].Value);
                }
                else
                {
                    throw new Exception("Match not found!");
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Could not gather CPU Utilization information from logline " + cpuInfoEvent, ex);
            }
        }
    }
}