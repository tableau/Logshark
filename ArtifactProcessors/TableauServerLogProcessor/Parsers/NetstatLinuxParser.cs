using LogParsers.Base;
using LogParsers.Base.Exceptions;
using LogParsers.Base.Extensions;
using LogParsers.Base.Helpers;
using LogParsers.Base.Parsers;
using Logshark.ArtifactProcessors.TableauServerLogProcessor.Parsers.Helpers.Netstat;
using Logshark.Common.Extensions;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Logshark.ArtifactProcessors.TableauServerLogProcessor.Parsers
{
    public sealed class NetstatLinuxParser : BaseParser, IParser
    {
        private readonly DateTime? lastModifiedTimestamp;

        private static readonly string collectionName = ParserConstants.NetstatCollectionName;
        private static readonly IList<string> indexNames = new List<string> { "worker" };
        private static readonly CollectionSchema collectionSchema = ParserUtil.CreateCollectionSchema(collectionName, indexNames);

        private readonly IList<NetstatRegex> netstatRegexes = new List<NetstatRegex>
        {
            new NetstatRegex(NetstatRegex.EntryType.ActiveInternetConnection, new Regex(
                        @"^
                          (?<protocol>\w+?)\s+
                          (?<recv_q>\d+?)\s+
                          (?<send_q>\d+?)\s+
                          (?<local_address>.+?):(?<local_port>[\d\*]+?)\s+
                          (?<foreign_address>.+?):(?<foreign_port>[\d\*]+?)\s+
                          ((?<state>\w+?)\s+)?
                          ((?<pid>\d+?)?/)?
                          (?<program_name>.+?)\s*$",
                    RegexOptions.ExplicitCapture | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled)
                ),
            new NetstatRegex(NetstatRegex.EntryType.UnixDomainSocket, new Regex(
                        @"^
                          (?<protocol>\w+?)\s+
                          (?<reference_count>\d+?)\s+
                          \[\s((?<flags>[A-Z]*?)\s)?\]\s+
                          (?<type>[A-Z]+?)\s+
                          ((?<state>[A-Z]+?)\s+)?
                          (?<inode>\d+?)\s+
                          ((?<pid>\d+?)/)?
                          (?<program_name>.+?)\s+
                          (?<path>.+?)?
                          $",
                    RegexOptions.ExplicitCapture | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled)
                )
        };

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

        public NetstatLinuxParser()
        {
        }

        public NetstatLinuxParser(LogFileContext fileContext)
            : base(fileContext)
        {
            lastModifiedTimestamp = fileContext.LastWriteTime;
        }

        public override JObject ParseLogDocument(TextReader reader)
        {
            var portReservations = new List<PortReservation>();
            var unixDomainSockets = new List<UnixDomainSocket>();

            string line;
            int lineCounter = 0;
            while ((line = ReadLine(reader)) != null)
            {
                lineCounter++;

                try
                {
                    for (int i = 0; i < netstatRegexes.Count; i++)
                    {
                        var netstatRegexToTry = netstatRegexes[i];

                        var fields = netstatRegexToTry.Regex.MatchNamedCaptures(line);
                        if (fields.Any())
                        {
                            switch (netstatRegexToTry.Type)
                            {
                                case NetstatRegex.EntryType.ActiveInternetConnection:
                                    PortReservation portReservation = BuildPortReservationEntry(fields, lineCounter);
                                    portReservations.Add(portReservation);
                                    break;

                                case NetstatRegex.EntryType.UnixDomainSocket:
                                    UnixDomainSocket unixDomainSocket = BuildUnixDomainSocketEntry(fields, lineCounter);
                                    unixDomainSockets.Add(unixDomainSocket);
                                    break;
                            }

                            // Promote matching regex to front of list to optimize matches of future entries.
                            netstatRegexes.RemoveAt(i);
                            netstatRegexes.Insert(0, netstatRegexToTry);
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw new ParsingException(String.Format("Failed to parse line {0} of document: {1}", lineCounter, ex.Message), ex);
                }
            }

            // Roll up entries into JObject
            return CreateNetstatJson(portReservations, unixDomainSockets);
        }

        private static PortReservation BuildPortReservationEntry(IDictionary<string, object> fields, int lineNumber)
        {
            return new PortReservation
            {
                Protocol = fields.TryGetString("protocol"),
                RecvQ = fields.TryGetInt("recv_q"),
                SendQ = fields.TryGetInt("send_q"),
                LocalAddress = fields.TryGetString("local_address"),
                LocalPort = fields.TryGetInt("local_port"),
                ForeignAddress = fields.TryGetString("foreign_address"),
                ForeignPort = fields.TryGetInt("foreign_port"),
                TcpState = fields.TryGetString("state"),
                ProcessId = fields.TryGetInt("pid"),
                Process = fields.TryGetString("program_name"),
                Line = lineNumber,
            };
        }

        private static UnixDomainSocket BuildUnixDomainSocketEntry(IDictionary<string, object> fields, int lineNumber)
        {
            var unixDomainSocketEntry = new UnixDomainSocket
            {
                Protocol = fields.TryGetString("protocol"),
                ReferenceCount = fields.TryGetInt("reference_count"),
                Type = fields.TryGetString("type"),
                State = fields.TryGetString("state"),
                INode = fields.TryGetInt("inode"),
                ProcessId = fields.TryGetInt("pid"),
                ProgramName = fields.TryGetString("program_name"),
                Path = fields.TryGetString("path"),
                Line = lineNumber
            };

            var rawFlagsString = fields.TryGetString("flags");
            if (!String.IsNullOrWhiteSpace(rawFlagsString))
            {
                unixDomainSocketEntry.Flags.AddRange(rawFlagsString.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
            }

            return unixDomainSocketEntry;
        }

        private JObject CreateNetstatJson(ICollection<PortReservation> portReservations, ICollection<UnixDomainSocket> unixDomainSockets)
        {
            var json = new JObject
            {
                { "active_connections", new JArray(portReservations.Select(reservation => reservation.ToJToken())) },
                { "unix_domain_sockets", new JArray(unixDomainSockets.Select(socket => socket.ToJToken())) },
                { "last_modified_at", lastModifiedTimestamp}
            };

            return InsertMetadata(json);
        }
    }
}