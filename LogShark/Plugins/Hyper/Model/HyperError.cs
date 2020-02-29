using System;
using System.Collections.Generic;
using System.Text;

namespace LogShark.Plugins.Hyper.Model
{
    public class HyperError
    {
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public string Key { get; set; }
        public int Line { get; set; }
        public int ProcessId { get; set; }
        public string RequestId { get; set; }
        public string SessionId { get; set; }
        public string Severity { get; set; }
        public string Site { get; set; }
        public string ThreadId { get; set; }
        public DateTime Timestamp { get; set; }
        public string User { get; set; }
        public string Value { get; set; }
        public string Worker { get; set; }
    }
}
