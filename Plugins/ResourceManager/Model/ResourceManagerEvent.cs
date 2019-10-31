using log4net;
using Logshark.PluginLib.Extensions;
using Logshark.PluginLib.Logging;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Logshark.Plugins.ResourceManager.Model
{
    public class ResourceManagerEvent
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

        private static readonly ILog Log = PluginLogFactory.GetLogger(Assembly.GetExecutingAssembly(), MethodBase.GetCurrentMethod());

        public string ProcessName { get; set; }
        public int? ProcessId { get; set; }
        public DateTime Timestamp { get; set; }
        public int Pid { get; set; }

        public string Worker { get; set; }
        public string FilePath { get; set; }
        public string FileName { get; set; }
        public int Line { get; set; }

        public ResourceManagerEvent()
        {
        }

        protected ResourceManagerEvent(BsonDocument document, string processName)
        {
            ProcessName = processName;
            ProcessId = ParseProcessId(document.GetString("file"));
            Timestamp = document.GetDateTime("ts");
            Pid = document.GetInt("pid");

            Worker = document.GetString("worker");
            FilePath = document.GetString("file_path");
            FileName = document.GetString("file");
            Line = document.GetInt("line");
        }

        /// <summary>
        /// Unfortunately our logs do not log the process index in the actual log events, so we have to rely on
        /// extracting this information from the log filename.
        /// </summary>
        protected int? ParseProcessId(string filename)
        {
            foreach (var match in ProcessIdRegexes.Select(regex => regex.Match(filename))
                                                  .Where(match => match.Success))
            {
                // Match found; see if we can parse the matched group as an integer.
                int processId;
                bool parsedSuccessfully = Int32.TryParse(match.Groups["process_id"].Value, out processId);
                if (parsedSuccessfully)
                {
                    return processId;
                }
            }

            // Legacy tabprotosrv logs did not encode the process id in the filename, so we should only whine if
            // if we're not working with tabprotosrv.
            if (!filename.StartsWith("tabprotosrv"))
            {
                Log.ErrorFormat("Failed to parse process id from filename '{0}'!", filename);
            }
            
            return null;
        }
    }
}