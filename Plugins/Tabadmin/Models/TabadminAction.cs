using Logshark.PluginLib.Extensions;
using MongoDB.Bson;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Logshark.Plugins.Tabadmin.Models
{
    /// <summary>
    /// A log entry representing an admin-initiated action (a "tabadmin command").
    /// </summary>
    internal sealed class TabadminAction : TabadminLogEvent
    {
        // Example String: "run as: <script> backup e:\Tableau\Backup\tabserver_20141020 -t e:\Tableau\TempData"
        // Would return: command="backup"; arguments="e:\Tableau\Backup\tabserver_20141020 -t e:\Tableau\TempData";
        private static readonly Regex commandAndArgumentRegex = new Regex(@"run as: <script>[\s](?<command>[\w]+)([\s](?<arguments>.*))?",
                                                                          RegexOptions.Compiled);

        public string Command { get; set; }
        public string Arguments { get; set; }

        public TabadminAction()
        {
        }

        public TabadminAction(BsonDocument document, IEnumerable<TableauServerVersion> versionTimeline) : base(document, versionTimeline)
        {
            // Split the message field into command and argument substrings.
            Match match = commandAndArgumentRegex.Match(document.GetString("message"));
            Command = match.Groups["command"].Value;
            Arguments = match.Groups["arguments"].Value;
        }
    }
}