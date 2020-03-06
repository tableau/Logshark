using System;
using System.Collections.Generic;
using System.Text;

namespace LogShark.Plugins.Vizportal
{
    public class VizportalEvent
    {
        public string RequestId { get; set; }

        public DateTime Timestamp { get; set; }

        public string User { get; set; }

        public string SessionId { get; set; }

        public string Site { get; set; }

        public string Severity { get; set; }

        public string Class { get; set; }

        public string Message { get; set; }

        public string Worker { get; set; }

        public string FilePath { get; set; }

        public string File { get; set; }

        public int LineNumber { get; set; }
    }
}
