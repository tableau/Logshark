using System;

namespace LogShark.Plugins.Config.Models
{
    public class ConfigProcessInfo
    {
        public DateTime FileLastModifiedUtc { get; }
        public string Hostname { get; }
        public int Port { get; }
        public string Process { get; }
        public string Worker { get; }

        public ConfigProcessInfo(DateTime fileLastModifiedUtc, string hostname, int port, string process, string worker)
        {
            FileLastModifiedUtc = fileLastModifiedUtc;
            Hostname = hostname;
            Port = port;
            Process = process;
            Worker = worker;
        }
    }
}