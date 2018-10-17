using System;

namespace Logshark.Plugins.Config.Model
{
    /// <summary>
    /// Models a piece of information about process topology.
    /// </summary>
    public class ConfigProcessInfo
    {
        public string Hostname { get; set; }
        public string Process { get; set; }
        public string Worker { get; set; }
        public int Port { get; set; }
        public DateTime? FileLastModified { get; set; }

        public ConfigProcessInfo()
        {
        }

        public ConfigProcessInfo(string hostname, string process, string worker, int port, DateTime? fileLastModifiedTimestamp)
        {
            Hostname = hostname;
            Process = process;
            Worker = worker;
            Port = port;
            FileLastModified = fileLastModifiedTimestamp;
        }

        public override string ToString()
        {
            return String.Format(@"{0}\{1} (worker {2})", Hostname, Process, Worker);
        }
    }
}