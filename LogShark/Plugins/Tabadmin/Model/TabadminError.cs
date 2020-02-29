using System;
using System.Collections.Generic;
using System.Text;

namespace LogShark.Plugins.Tabadmin.Model
{
    public class TabadminError
    {
        public string File { get; set; }
        public string FilePath { get; set; }
        public string Hostname { get; set; }
        public string Id { get; set; }
        public int Line { get; set; }
        public string Message { get; set; }
        public string Severity { get; set; }
        public DateTime Timestamp { get; set; }
        public DateTime TimestampGmt { get; set; }
        public string TimestampOffset { get; set; }
        public string Version { get; set; }
        public string VersionId { get; set; }
        public string VersionLong { get; set; }
        public string Worker { get; set; }
    }
}
