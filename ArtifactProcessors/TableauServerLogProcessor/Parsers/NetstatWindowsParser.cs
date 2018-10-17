using LogParsers.Base;
using LogParsers.Base.Exceptions;
using LogParsers.Base.Helpers;
using LogParsers.Base.Parsers;
using Logshark.ArtifactProcessors.TableauServerLogProcessor.Parsers.Helpers.Netstat;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Logshark.ArtifactProcessors.TableauServerLogProcessor.Parsers
{
    /// <summary>
    /// Parses Netstat output to JSON.
    /// </summary>
    public sealed class NetstatWindowsParser : BaseParser, IParser
    {
        private readonly DateTime? lastModifiedTimestamp;

        private static readonly string collectionName = ParserConstants.NetstatCollectionName;
        private static readonly IList<string> indexNames = new List<string> { "worker" };
        private static readonly CollectionSchema collectionSchema = ParserUtil.CreateCollectionSchema(collectionName, indexNames);

        public override CollectionSchema CollectionSchema => collectionSchema;
        public override bool IsMultiLineLogType => true;
        protected override bool UseLineNumbers => false;

        public NetstatWindowsParser()
        {
        }

        public NetstatWindowsParser(LogFileContext fileContext)
            : base(fileContext)
        {
            lastModifiedTimestamp = fileContext.LastWriteTime;
        }

        public override JObject ParseLogDocument(TextReader reader)
        {
            // Windows Netstat lists process & component name information *below* one or more associated port reservation lines.
            // Our technique for getting around this is to keep pushing port reservations into a queue until we hit a process name, at which point
            // everything in that queue should be associated with the encountered process name.
            var unassignedPortReservations = new Queue<PortReservation>();
            var processedPortReservations = new List<PortReservation>();

            string line;
            int lineCounter = 0;
            while ((line = ReadLine(reader)) != null)
            {
                lineCounter++;
                line = line.Trim();

                if (IsPortReservationEntry(line))
                {
                    try
                    {
                        var portReservation = CreatePortReservation(line, lineCounter);
                        unassignedPortReservations.Enqueue(portReservation);
                    }
                    catch (Exception ex)
                    {
                        throw new ParsingException(String.Format("Failed to parse line {0} of document: {1}", lineCounter, ex.Message), ex);
                    }
                }
                else if (IsComponentNameEntry(line))
                {
                    foreach (var unassignedPortReservation in unassignedPortReservations)
                    {
                        unassignedPortReservation.Component = line;
                    }
                }
                else if (IsProcessNameEntry(line))
                {
                    string processName = line.Trim('[', ']');

                    while (unassignedPortReservations.Any())
                    {
                        // Found the process name at the bottom of a group of entries!  Flush the queue and add to results.
                        var portReservation = unassignedPortReservations.Dequeue();
                        portReservation.Process = processName;

                        processedPortReservations.Add(portReservation);
                    }
                }
            }

            // Roll up entries into JObject
            return CreateNetstatJson(processedPortReservations);
        }

        private static bool IsPortReservationEntry(string line)
        {
            return line.StartsWith("TCP") || line.StartsWith("UDP");
        }

        private static PortReservation CreatePortReservation(string line, int lineNumber)
        {
            var tokens = Tokenize(line);

            // Sanity check
            if (tokens.Count < 3 || tokens.Count > 4)
            {
                throw new ArgumentException("Netstat entry does not a valid number of tokens!");
            }

            var portReservation = new PortReservation
            {
                Protocol = tokens[0],
                LocalAddress = ExtractHostname(tokens[1]),
                LocalPort = ExtractPort(tokens[1]),
                ForeignAddress = ExtractHostname(tokens[2]),
                ForeignPort = ExtractPort(tokens[2]),
                Line = lineNumber,
            };
            if (tokens.Count == 4)
            {
                portReservation.TcpState = tokens[3];
            }

            return portReservation;
        }

        private static bool IsComponentNameEntry(string line)
        {
            var tokens = Tokenize(line);

            // A component line has exactly one word and is not wrapped in brackets.
            return tokens.Count == 1 &&
                   !tokens.First().StartsWith("[") && !tokens.First().EndsWith("]");
        }

        private static bool IsProcessNameEntry(string line)
        {
            // A process name line is wrapped in square brackets, or else is unknown with a static message.
            return line.StartsWith("[") && line.EndsWith("]") ||
                   line.Equals("Can not obtain ownership information");
        }

        private JObject CreateNetstatJson(ICollection<PortReservation> portReservations)
        {
            var json = new JObject
            {
                { "active_connections", new JArray(portReservations.Select(reservation => reservation.ToJToken())) },
                { "last_modified_at", lastModifiedTimestamp}
            };

            return InsertMetadata(json);
        }

        /// <summary>
        /// Breaks up a string into a sequence of trimmed tokens.
        /// </summary>
        private static IList<string> Tokenize(string line)
        {
            return line.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries)
                       .Select(token => token.Trim())
                       .ToList();
        }

        /// <summary>
        /// Extracts a hostname from a hostname:port string.
        /// </summary>
        private static string ExtractHostname(string str)
        {
            int lastIndexOfColon = str.LastIndexOf(':');
            if (lastIndexOfColon == -1)
            {
                return null;
            }

            return str.Substring(0, lastIndexOfColon).Trim('[', ']');
        }

        /// <summary>
        /// Extracts a port from a hostname:port string.
        /// </summary>
        private static int? ExtractPort(string str)
        {
            int lastIndexOfColon = str.LastIndexOf(':');
            if (lastIndexOfColon == -1)
            {
                return null;
            }

            var portStartIndex = lastIndexOfColon + 1;
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