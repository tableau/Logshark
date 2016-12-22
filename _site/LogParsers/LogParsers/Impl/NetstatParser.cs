using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LogParsers.Helpers;

namespace LogParsers
{
    /// <summary>
    /// Parses Netstat output to JSON.
    /// </summary>
    public sealed class NetstatParser : BaseParser, IParser
    {
        private readonly DateTime? lastModifiedTimestamp;

        private class PortReservation
        {
            public string Protocol { private get; set; }
            public string LocalAddress { private get; set; }
            public int? LocalPort { private get; set; }
            public string ForeignAddress { private get; set; }
            public int? ForeignPort { private get; set; }
            public string TcpState { private get; set; }

            public JToken ToJToken()
            {
                return new JObject
                {
                    { "protocol", Protocol},
                    { "local_address", LocalAddress },
                    { "local_port", LocalPort },
                    { "foreign_address", ForeignAddress },
                    { "foreign_port", ForeignPort },
                    { "tcp_state", TcpState}
                };
            }
        }

        private class NetstatEntry
        {
            public string Process { private get; set; }
            public string Component { private get; set; }
            public ICollection<PortReservation> TransportReservations { get; private set; }
            public int Line { private get; set; }

            public NetstatEntry()
            {
                TransportReservations = new List<PortReservation>();
            }

            public JToken ToJToken()
            {
                JToken token = new JObject
                {
                    { "process", Process },
                    { "component", Component },
                    { "transport_reservations", new JArray(TransportReservations.Select(res => res.ToJToken()))},
                    { "line", Line }
                };

                return token;
            }
        }

        private static readonly string collectionName = ParserConstants.NetstatCollectionName;
        private static readonly IList<string> indexNames = new List<string> { "local_port", "worker" };
        private static readonly CollectionSchema collectionSchema = ParserUtil.CreateCollectionSchema(collectionName, indexNames);

        public override CollectionSchema CollectionSchema
        {
            get { return collectionSchema; }
        }

        public override bool IsMultiLineLogType
        {
            get { return true; }
        }

        protected override bool UseLineNumbers
        {
            get { return false; }
        }

        public NetstatParser()
        {
        }

        public NetstatParser(LogFileContext fileContext)
            : base(fileContext)
        {
            lastModifiedTimestamp = fileContext.LastWriteTime;
        }

        public override JObject ParseLogDocument(TextReader reader)
        {
            IList<NetstatEntry> entries = new List<NetstatEntry>();

            NetstatEntry entry = new NetstatEntry();

            string line;
            int lineCounter = 0;
            while ((line = ReadLine(reader)) != null)
            {
                lineCounter++;

                // Ignore headers & empty lines.
                if (!IsNetstatContent(line))
                {
                    continue;
                }

                IList<string> tokens = line.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToList();
                if (tokens.Count == 1)
                {
                    if (tokens[0].StartsWith("[") && tokens[0].EndsWith("]"))
                    {
                        entry.Process = tokens[0].Trim('[', ']');
                        entry.Line = lineCounter;
                        entries.Add(entry);
                        entry = new NetstatEntry();
                    }
                    else
                    {
                        entry.Component = tokens[0];
                    }
                }
                else if (tokens.Count == 3 || tokens.Count == 4)
                {
                    var portReservation = new PortReservation
                    {
                        Protocol = tokens[0],
                        LocalAddress = ExtractHostname(tokens[1]),
                        LocalPort = ExtractPort(tokens[1]),
                        ForeignAddress = ExtractHostname(tokens[2]),
                        ForeignPort = ExtractPort(tokens[2])
                    };
                    if (tokens.Count == 4)
                    {
                        portReservation.TcpState = tokens[3];
                    }
                    entry.TransportReservations.Add(portReservation);
                }
            }

            // Roll up entries into JObject
            JObject netstatJson = CreateJObject(entries);

            return InsertMetadata(netstatJson);
        }

        private JObject CreateJObject(ICollection<NetstatEntry> netstatEntries)
        {
            JObject jObject = new JObject();

            JArray netstatEntryArray = new JArray();
            foreach (var netstatEntry in netstatEntries)
            {
                netstatEntryArray.Add(netstatEntry.ToJToken());
            }
            jObject["entries"] = netstatEntryArray;
            jObject["last_modified_at"] = lastModifiedTimestamp;

            return jObject;
        }

        private static bool IsNetstatContent(string line)
        {
            return !String.IsNullOrWhiteSpace(line) && !line.StartsWith("Active Connections") && !line.StartsWith("  Proto");
        }

        private static string ExtractHostname(string str)
        {
            if (!str.Contains(":"))
            {
                return null;
            }

            var lastColon = str.LastIndexOf(':');
            return str.Substring(0, lastColon).Trim('[', ']');
        }

        private static int? ExtractPort(string str)
        {
            if (!str.Contains(":"))
            {
                return null;
            }

            var portStartIndex = str.LastIndexOf(':') + 1;
            if (portStartIndex >= str.Length)
            {
                return null;
            }

            int port;
            bool parsedPort = Int32.TryParse(str.Substring(portStartIndex), out port);
            if (!parsedPort)
            {
                return null;
            }

            return port;
        }
    }
}