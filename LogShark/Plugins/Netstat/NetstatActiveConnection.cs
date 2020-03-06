using System;

namespace LogShark.Plugins.Netstat
{
    public class NetstatActiveConnection
    {
        public int Line { get; set; }

        public string ProcessName { get; set; }

        public int? ProcessId { get; set; }

        public string ComponentName { get; set; }

        public string Protocol { get; set; }

        public string LocalAddress { get; set; }

        public string LocalPort { get; set; }

        public string ForeignAddress { get; set; }

        public string ForeignPort { get; set; }

        public string TcpState { get; set; }

        public int? RecvQ { get; set; }

        public int? SendQ { get; set; }

        public bool IsKnownTableauServerProcess { get; set; }

        public string Worker { get; set; }

        public DateTime? FileLastModified { get; set; }
    }
}