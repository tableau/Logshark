using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Text.RegularExpressions;

namespace Logshark.Plugins.Postgres.Model
{
    [BsonIgnoreExtraElements]
    internal class PostgresEvent
    {
        private static readonly Regex DurationMessageRegex = new Regex(@"^duration: (?<duration>\d+?)\..*", RegexOptions.ExplicitCapture | RegexOptions.Compiled);

        [BsonElement("ts")]
        public DateTime Timestamp { get; set; }

        [BsonElement("pid")]
        public int ProcessId { get; set; }

        [BsonElement("sev")]
        public string Severity { get; set; }

        [BsonElement("message")]
        public string Message { get; set; }

        [BsonIgnore]
        public int? Duration
        {
            get
            {
                if (Message != null)
                {
                    // Check if message contains a duration.
                    Match match = DurationMessageRegex.Match(Message);
                    if (match.Success && match.Groups["duration"] != null)
                    {
                        int duration;
                        if (Int32.TryParse(match.Groups["duration"].Value, out duration))
                        {
                            return duration;
                        }
                    }
                }

                return null;
            }
        }

        [BsonElement("worker")]
        public string Worker { get; set; }

        [BsonElement("file_path")]
        public string FilePath { get; set; }

        [BsonElement("file")]
        public string File { get; set; }

        [BsonElement("line")]
        public int LineNumber { get; set; }
    }
}