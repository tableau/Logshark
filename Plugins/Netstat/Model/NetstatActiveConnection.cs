using Logshark.PluginLib.Helpers;
using Logshark.Plugins.Netstat.Helpers;
using MongoDB.Bson;
using ServiceStack.DataAnnotations;
using System;

namespace Logshark.Plugins.Netstat.Model
{
    [Alias("netstat_active_connections")]
    internal class NetstatActiveConnection
    {
        [PrimaryKey]
        [AutoIncrement]
        public int Id { get; set; }

        [Index]
        public Guid LogsetHash { get; private set; }

        [Index(Unique = true)]
        public Guid EntryHash { get; private set; }

        public string Worker { get; private set; }

        [Index]
        public DateTime? FileLastModified { get; private set; }

        public int Line { get; private set; }

        [Index]
        public string ProcessName { get; private set; }

        public int? ProcessId { get; private set; }

        public string ComponentName { get; private set; }

        [Index]
        public string Protocol { get; private set; }

        public string LocalAddress { get; private set; }

        [Index]
        public int? LocalPort { get; private set; }

        public string ForeignAddress { get; private set; }

        public int? ForeignPort { get; private set; }

        public string TcpState { get; private set; }

        public int? RecvQ { get; private set; }

        public int? SendQ { get; private set; }

        public bool IsKnownTableauServerProcess { get; private set; }

        public NetstatActiveConnection()
        {
        }

        public NetstatActiveConnection(Guid logsetHash, string worker, BsonDocument activeConnectionsDocument, DateTime? fileLastModified)
        {
            LogsetHash = logsetHash;
            Worker = worker;
            FileLastModified = fileLastModified;
            Line = BsonDocumentHelper.GetInt("line", activeConnectionsDocument);
            ProcessName = BsonDocumentHelper.GetString("process", activeConnectionsDocument);
            ProcessId = BsonDocumentHelper.GetNullableInt("pid", activeConnectionsDocument);
            ComponentName = BsonDocumentHelper.GetString("component", activeConnectionsDocument);
            Protocol = BsonDocumentHelper.GetString("protocol", activeConnectionsDocument);
            LocalAddress = BsonDocumentHelper.GetString("local_address", activeConnectionsDocument);
            LocalPort = BsonDocumentHelper.GetNullableInt("local_port", activeConnectionsDocument);
            ForeignAddress = BsonDocumentHelper.GetString("foreign_address", activeConnectionsDocument);
            ForeignPort = BsonDocumentHelper.GetNullableInt("foreign_port", activeConnectionsDocument);
            TcpState = BsonDocumentHelper.GetString("tcp_state", activeConnectionsDocument);
            RecvQ = BsonDocumentHelper.GetNullableInt("recv_q", activeConnectionsDocument);
            SendQ = BsonDocumentHelper.GetNullableInt("send_q", activeConnectionsDocument);
            IsKnownTableauServerProcess = ProcessNameHelper.IsKnownTableauServerProcess(ProcessName);
            EntryHash = HashHelper.GenerateHashGuid(LogsetHash.ToString(), Worker, Line, ProcessName, ComponentName, Protocol, LocalAddress, LocalPort, ForeignAddress, ForeignPort, TcpState);
        }
    }
}