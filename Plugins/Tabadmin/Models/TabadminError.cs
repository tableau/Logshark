using Logshark.PluginLib.Extensions;
using MongoDB.Bson;
using System.Collections.Generic;

namespace Logshark.Plugins.Tabadmin.Models
{
    /// <summary>
    /// An error, fatal, or warning message from a Tabadmin log.
    /// </summary>
    internal sealed class TabadminError : TabadminLogEvent
    {
        public string Severity { get; set; }
        public string Message { get; set; }

        public TabadminError()
        {
        }

        public TabadminError(BsonDocument document, IEnumerable<TableauServerVersion> versionTimeline) : base(document, versionTimeline)
        {
            Severity = document.GetString("sev");
            Message = document.GetString("message");
        }
    }
}