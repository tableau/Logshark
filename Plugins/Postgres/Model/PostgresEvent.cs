using Logshark.PluginLib.Helpers;
using MongoDB.Bson;
using ServiceStack.DataAnnotations;
using System;
using System.Text.RegularExpressions;

namespace Logshark.Plugins.Postgres.Model
{
    internal class PostgresEvent
    {
        private static readonly Regex DurationMessageRegex = new Regex(@"^duration: (?<duration>\d+?)\..*", RegexOptions.Compiled);

        [PrimaryKey]
        [AutoIncrement]
        public int Id { get; set; }

        public Guid LogsetHash { get; set; }

        [Index(Unique = true)]
        public Guid EventHash { get; set; }

        [Index]
        public DateTime Timestamp { get; set; }
        public string ProcessId { get; set; }
        public string Severity { get; set; }
        public string Message { get; set; }
        public int? Duration { get; set; }
        public string Worker { get; set; }

        public PostgresEvent() { }

        public PostgresEvent(BsonDocument logLine, Guid logsetHash)
        {
            LogsetHash = logsetHash;
            Timestamp = BsonDocumentHelper.GetDateTime("ts", logLine);
            ProcessId = BsonDocumentHelper.GetString("pid", logLine);
            Severity = BsonDocumentHelper.GetString("sev", logLine);
            Message = BsonDocumentHelper.GetString("message", logLine);
            if (Message != null)
            {
                // Check if message contains a duration.
                Match match = DurationMessageRegex.Match(Message);
                if (match.Success && match.Groups["duration"] != null && match.Groups["duration"].Success)
                {
                    int duration;
                    if (Int32.TryParse(match.Groups["duration"].Value, out duration))
                    {
                        Duration = duration;
                    }
                }
            }
            Worker = BsonDocumentHelper.GetString("worker", logLine);
            EventHash = GetEventHash(logLine);
        }

        protected Guid GetEventHash(BsonDocument logLine)
        {
            string file = BsonDocumentHelper.GetString("file", logLine);
            int line = BsonDocumentHelper.GetInt("line", logLine);
            return HashHelper.GenerateHashGuid(Timestamp, ProcessId, Message, Worker, file, line);
        }
    }
}
