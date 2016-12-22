using Logshark.PluginLib.Helpers;
using MongoDB.Bson;
using System;
using System.Text.RegularExpressions;

namespace Logshark.Plugins.Tabadmin.Models
{
    /// <summary>
    /// A log entry representing an admin-initiated action (a "tabadmin command").
    /// </summary>
    internal class TabadminAction : TabadminLoggedItem
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

        public TabadminAction(BsonDocument logLine, Guid logsetHash) : base(logLine, logsetHash)
        {
            // Split the message field into command and argument substrings.
            Match match = commandAndArgumentRegex.Match(BsonDocumentHelper.GetString("message", logLine));
            Command = match.Groups["command"].Value;
            Arguments = match.Groups["arguments"].Value;
        }
    }
}