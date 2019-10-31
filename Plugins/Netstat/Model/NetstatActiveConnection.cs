using Logshark.Plugins.Netstat.Helpers;
using System;

namespace Logshark.Plugins.Netstat.Model
{
    internal class NetstatActiveConnection : NetstatDocumentEntry
    {
        public bool IsKnownTableauServerProcess { get; set; }

        public string Worker { get; set; }

        public DateTime? FileLastModified { get; set; }

        public NetstatActiveConnection()
        {
        }

        public NetstatActiveConnection(NetstatDocumentEntry entry, string worker, DateTime? fileLastModified)
        {
            Line = entry.Line;
            ProcessName = entry.ProcessName;
            ProcessId = entry.ProcessId;
            ComponentName = entry.ComponentName;
            Protocol = entry.Protocol;
            LocalAddress = entry.LocalAddress;
            LocalPort = entry.LocalPort;
            ForeignAddress = entry.ForeignAddress;
            ForeignPort = entry.ForeignPort;
            TcpState = entry.TcpState;
            RecvQ = entry.RecvQ;
            SendQ = entry.SendQ;

            IsKnownTableauServerProcess = ProcessNameHelper.IsKnownTableauServerProcess(ProcessName);
            Worker = worker;
            FileLastModified = fileLastModified;
        }
    }
}