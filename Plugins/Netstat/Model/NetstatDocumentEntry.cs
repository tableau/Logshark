using MongoDB.Bson.Serialization.Attributes;

namespace Logshark.Plugins.Netstat.Model
{
    [BsonIgnoreExtraElements]
    internal class NetstatDocumentEntry
    {
        [BsonElement("line")]
        public int Line { get; set; }

        [BsonElement("process")]
        public string ProcessName { get; set; }

        [BsonElement("pid")]
        public int? ProcessId { get; set; }

        [BsonElement("component")]
        public string ComponentName { get; set; }

        [BsonElement("protocol")]
        public string Protocol { get; set; }

        [BsonElement("local_address")]
        public string LocalAddress { get; set; }

        [BsonElement("local_port")]
        public int? LocalPort { get; set; }

        [BsonElement("foreign_address")]
        public string ForeignAddress { get; set; }

        [BsonElement("foreign_port")]
        public int? ForeignPort { get; set; }

        [BsonElement("tcp_state")]
        public string TcpState { get; set; }

        [BsonElement("recv_q")]
        public int? RecvQ { get; set; }

        [BsonElement("send_q")]
        public int? SendQ { get; set; }
    }
}