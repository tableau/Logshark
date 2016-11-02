using Logshark.PluginLib.Helpers;
using Logshark.Plugins.Netstat.Helpers;
using MongoDB.Bson;
using ServiceStack.DataAnnotations;
using System;

namespace Logshark.Plugins.Netstat.Model
{
    internal class NetstatEntry
    {
        [PrimaryKey]
        [AutoIncrement]
        public int Id { get; set; }

        [Index]
        public Guid LogsetHash { get; private set; }

        [Index(Unique = true)]
        public Guid EntryHash { get; private set; }

        public int Worker { get; private set; }

        [Index]
        public DateTime? FileLastModified { get; set; }

        public int Line { get; private set; }

        [Index]
        public string ProcessName { get; private set; }

        public string ComponentName { get; private set; }

        [Index]
        public string Protocol { get; private set; }

        public string LocalAddress { get; private set; }

        [Index]
        public int? LocalPort { get; private set; }

        public string ForeignAddress { get; private set; }

        public int? ForeignPort { get; private set; }

        public string TcpState { get; private set; }

        public bool IsKnownTableauServerProcess { get; private set; }

        public NetstatEntry()
        {
        }

        public NetstatEntry(Guid logsetHash, int worker, BsonDocument netstatEntryDocument, BsonDocument transportReservationDocument, DateTime? fileLastModified)
        {
            LogsetHash = logsetHash;
            Worker = worker;
            FileLastModified = fileLastModified;
            Line = BsonDocumentHelper.GetInt("line", netstatEntryDocument);
            ProcessName = BsonDocumentHelper.GetString("process", netstatEntryDocument);
            ComponentName = BsonDocumentHelper.GetString("component", netstatEntryDocument);
            Protocol = BsonDocumentHelper.GetString("protocol", transportReservationDocument);
            LocalAddress = BsonDocumentHelper.GetString("local_address", transportReservationDocument);
            LocalPort = BsonDocumentHelper.GetNullableInt("local_port", transportReservationDocument);
            ForeignAddress = BsonDocumentHelper.GetString("foreign_address", transportReservationDocument);
            ForeignPort = BsonDocumentHelper.GetNullableInt("foreign_port", transportReservationDocument);
            TcpState = BsonDocumentHelper.GetString("tcp_state", transportReservationDocument);
            IsKnownTableauServerProcess = ProcessNameHelper.IsKnownTableauServerProcess(ProcessName);
            EntryHash = HashHelper.GenerateHashGuid(LogsetHash.ToString(), Worker, Line, ProcessName, ComponentName, Protocol, LocalAddress, LocalPort, ForeignAddress, ForeignPort, TcpState);
        }
    }
}