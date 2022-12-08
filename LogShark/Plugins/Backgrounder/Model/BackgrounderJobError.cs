using System;

namespace LogShark.Plugins.Backgrounder.Model
{
    public class BackgrounderJobError
    {
        public string BackgrounderJobId { get; set; }
        public string Class { get; set; }
        public string File { get; set; }
        public int Line { get; set; }
        public string Message { get; set; }
        public string Severity { get; set; }
        public string Site { get; set; }
        public string Thread { get; set; }
        public DateTime Timestamp { get; set; }
    }
}