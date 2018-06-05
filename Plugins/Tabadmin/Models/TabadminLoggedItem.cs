using Logshark.PluginLib.Helpers;
using Logshark.Plugins.Tabadmin.Helpers;
using MongoDB.Bson;
using ServiceStack.DataAnnotations;
using System;

namespace Logshark.Plugins.Tabadmin.Models
{
    /// <summary>
    /// A generalized object representing a log line. Will usually need to be inherited from to be useful.
    /// </summary>
    internal class TabadminLoggedItem : TabadminModelBase
    {
        public int? VersionId { get; set; }
        public string TimestampOffset { get; set; }

        [Index]
        public DateTime Timestamp { get; set; }

        public DateTime TimestampGmt { get; set; }
        public string Worker { get; set; }
        public string Hostname { get; set; }
        public string File { get; set; }
        public string FilePath { get; set; }
        public int Line { get; set; }

        public TabadminLoggedItem()
        {
        }

        public TabadminLoggedItem(BsonDocument logLine, Guid logsetHash)
        {
            LogsetHash = logsetHash;
            Timestamp = BsonDocumentHelper.GetDateTime("ts", logLine);
            TimestampOffset = BsonDocumentHelper.GetString("ts_offset", logLine);
            TimestampGmt = (DateTime)DateTimeConversionHelper.ConvertDateTime(Timestamp, TimestampOffset);
            Worker = BsonDocumentHelper.GetString("worker", logLine);
            Hostname = BsonDocumentHelper.GetString("hostname", logLine);
            File = BsonDocumentHelper.GetString("file", logLine);
            FilePath = BsonDocumentHelper.GetString("file_path", logLine);
            Line = BsonDocumentHelper.GetInt("line", logLine);
            EventHash = HashHelper.GenerateHashGuid(Timestamp, TimestampOffset, Worker, Hostname, File, FilePath, Line);
        }
    }
}