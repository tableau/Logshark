using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;

namespace Logshark.Plugins.Netstat.Model
{
    [BsonIgnoreExtraElements]
    internal class NetstatDocument
    {
        [BsonElement("active_connections")]
        public IList<NetstatDocumentEntry> ActiveConnections { get; set; }

        [BsonElement("worker")]
        public string Worker { get; set; }

        [BsonElement("last_modified_at")]
        public DateTime? FileLastModified { get; set; }
    }
}