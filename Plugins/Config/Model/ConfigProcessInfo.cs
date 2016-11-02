using Logshark.PluginLib.Helpers;
using ServiceStack.DataAnnotations;
using System;

namespace Logshark.Plugins.Config.Model
{
    /// <summary>
    /// Models a piece of information about process topology.
    /// </summary>
    public class ConfigProcessInfo
    {
        [PrimaryKey]
        [AutoIncrement]
        public int Id { get; set; }

        public Guid LogsetHash { get; set; }

        [Index(Unique = true)]
        public Guid ConfigInfoHash { get; set; }

        [Index]
        public DateTime? FileLastModified { get; set; }

        [Index]
        public string Hostname { get; set; }

        [Index]
        public string Process { get; set; }

        public int Worker { get; set; }
        public int Port { get; set; }

        public ConfigProcessInfo()
        {
        }

        public ConfigProcessInfo(string logsetHash, DateTime? fileLastModifiedTimestamp, string hostname, string process, int worker, int port)
        {
            LogsetHash = Guid.Parse(logsetHash);
            FileLastModified = fileLastModifiedTimestamp;
            ConfigInfoHash = HashHelper.GenerateHashGuid(logsetHash, process, worker, port);
            Hostname = hostname;
            Process = process;
            Worker = worker;
            Port = port;
        }

        public override string ToString()
        {
            return String.Format(@"{0}\{1} (worker {2})", Hostname, Process, Worker);
        }
    }
}