using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace LogShark.Plugins.Shared
{
    public static class ProcessInfoParser
    {
        private static readonly IList<Regex> ProcessIdRegexes = new List<Regex>
        {
            // Parse the process index from a filename which contains a timestamp
            // For example, extract "0" from "backgrounder_3-0_2017_12_19_03_24_29.txt"
            new Regex(@"\w+?_(\d+-)?(?<process_id>\d+?)_\d{4}.*",
                RegexOptions.Compiled | RegexOptions.ExplicitCapture),

            // Parse the process index from a filename which does not contain a timestamp
            // For example, extract "0" from "tabprotosrv_dataserver_1-0_1.txt"
            new Regex(@"\w+?-(?<process_id>\d+?).*",
                RegexOptions.Compiled | RegexOptions.ExplicitCapture)
        };
        
        public static string GetProcessName(LogType logType)
        {
            switch (logType)
            {
                case LogType.BackgrounderCpp:
                    return "backgrounder";
                case LogType.DataserverCpp:
                    return "dataserver";
                case LogType.ProtocolServer:
                    return "protocolserver";
                case LogType.VizportalCpp:
                    return "vizportal";
                case LogType.VizqlserverCpp:
                    return "vizqlserver";
                case LogType.Hyper:
                    return "hyper";
                default:
                    throw new ArgumentException($"Somebody forgot to add case for {logType} to {nameof(GetProcessName)}");
            }
        }
        
        /// <summary>
        /// Unfortunately our logs do not log the process index in the actual log events, so we have to rely on
        /// extracting this information from the log filename.
        /// </summary>
        public static int? ParseProcessIndex(string filename)
        {
            foreach (var match in ProcessIdRegexes.Select(regex => regex.Match(filename))
                .Where(match => match.Success))
            {
                // Match found; see if we can parse the matched group as an integer.
                var parsedSuccessfully = int.TryParse(match.Groups["process_id"].Value, out var processId);
                if (parsedSuccessfully)
                {
                    return processId;
                }
            }
            
            return null;
        }
    }
}