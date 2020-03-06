using System;

namespace LogShark.Plugins.SearchServer
{
    public class SearchServerEvent
    {
        public string Class { get; set; }
        public string File { get; set; }
        public string FilePath { get; set; }
        public int LineNumber { get; set; }
        public string Message { get; set; }
        public string Severity { get; set; }
        public DateTime Timestamp { get; set; }
        public string Worker { get; set; }
    }
}