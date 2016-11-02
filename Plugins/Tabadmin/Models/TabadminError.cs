using Logshark.PluginLib.Helpers;
using MongoDB.Bson;
using System;

namespace Logshark.Plugins.Tabadmin.Models
{
    /// <summary>
    /// An error, fatal, or warning message from a Tabadmin log.
    /// </summary>
    internal class TabadminError : TabadminLoggedItem
    {
        public string Severity { get; set; }
        public string Message { get; set; }

        public TabadminError()
        {
        }

        public TabadminError(BsonDocument logLine, Guid logsetHash) : base(logLine, logsetHash)
        {
            Severity = BsonDocumentHelper.GetString("sev", logLine);
            Message = BsonDocumentHelper.GetString("message", logLine);
        }
    }
}