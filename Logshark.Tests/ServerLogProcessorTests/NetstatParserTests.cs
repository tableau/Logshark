using FluentAssertions;
using LogParsers.Base.Parsers;
using Logshark.ArtifactProcessors.TableauServerLogProcessor.Parsers;
using Logshark.Tests.Helpers;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

namespace Logshark.Tests.ServerLogProcessorTests
{
    [TestFixture, Description("Unit tests covering Netstat log parsing.")]
    public class NetstatParserTests
    {
        [Test, Description("Parses a sample netstat logfile and ensures that the correct structure exists and that the correct number of documents were parsed correctly.")]
        [TestCase("netstat_linux.txt", 49, 14, Description = "Test Linux netstat file")]
        public void ParseLinuxNetstatFile(string logFile, int expectedActiveConnectionEntries, int expectedUnixDomainSocketEntries)
        {
            var parsedNetstatDocument = ParseAndValidateNetstatRootDocument(new NetstatLinuxParser(), logFile);

            var activeConnections = GetActiveConnections(parsedNetstatDocument);
            activeConnections.Should().HaveCount(expectedActiveConnectionEntries);

            var unixDomainSockets = GetUnixDomainSockets(parsedNetstatDocument);
            unixDomainSockets.Should().HaveCount(expectedUnixDomainSocketEntries);
        }

        [Test, Description("Parses a sample netstat logfile and ensures that the correct structure exists and that the correct number of documents were parsed correctly.")]
        [TestCase("netstat_windows.txt", 33, Description = "Test Windows netstat file")]
        public void ParseWindowsNetstatFile(string logFile, int expectedConnectionEntries)
        {
            var parsedNetstatDocument = ParseAndValidateNetstatRootDocument(new NetstatWindowsParser(), logFile);

            var activeConnections = GetActiveConnections(parsedNetstatDocument);
            activeConnections.Should().HaveCount(expectedConnectionEntries);
        }

        private static JObject ParseAndValidateNetstatRootDocument(IParser parser, string logFile)
        {
            var logPath = TestDataHelper.GetServerLogProcessorResourcePath(logFile);

            ICollection<JObject> documents = ParserTestHelpers.ParseFile(logPath, parser);

            documents.Should().HaveCount(1, "Parsing a netstat file should return exactly one root document");

            return documents.First();
        }

        private static JToken GetActiveConnections(JObject netstatRootDocument)
        {
            netstatRootDocument.Should().ContainKey("active_connections", "Parsing a netstat file should yield an active connection collection");

            return netstatRootDocument.GetValue("active_connections");
        }

        private static JToken GetUnixDomainSockets(JObject netstatRootDocument)
        {
            netstatRootDocument.Should().ContainKey("unix_domain_sockets", "Parsing a Linux netstat file should yield an unix domain sockets collection");

            return netstatRootDocument.GetValue("unix_domain_sockets");
        }
    }
}