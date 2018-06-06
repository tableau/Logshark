using FluentAssertions;
using Logshark.ArtifactProcessors.TableauServerLogProcessor.Parsers;
using Logshark.Tests.Helpers;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace Logshark.Tests.ServerLogProcessorTests
{
    [TestFixture, Description("Unit tests covering pgsql log parsing.")]
    public class PostgresParserTests
    {
        [Test, Description("Parses a sample postgresql log file into a collection of Json documents.  This format is the default in Tableau < 9.3")]
        [TestCase("postgresql_92.log", Description = "Pre-9.3 Format")]
        public void ParseLegacyPostgresLog(string logFile)
        {
            var logPath = TestDataHelper.GetServerLogProcessorResourcePath(logFile);

            IList<JObject> documents = ParserTestHelpers.ParseFile(logPath, new PostgresLegacyParser());

            // Count number of actual log events by counting the number of timestamps
            var lineCount = Regex.Matches(File.ReadAllText(logPath), @"^\d{4}-\d{2}-\d{2}\s\d{2}:\d{2}:\d{2}.\d{3}\s", RegexOptions.Multiline).Count;

            documents.Count.Should().Be(lineCount, "Number of parsed documents should match number of timestamps in file!");
        }

        [Test, Description("Parses a sample postgresql csv file into a collection of Json documents.  This format is the default in Tableau >= 9.3")]
        [TestCase("postgresql_93.csv", Description = "9.3+ Format")]
        public void ParsePostgresLog(string logFile)
        {
            var logPath = TestDataHelper.GetServerLogProcessorResourcePath(logFile);

            IList<JObject> documents = ParserTestHelpers.ParseFile(logPath, new PostgresParser());

            // Count number of actual log events by counting the number of timestamps
            var lineCount = Regex.Matches(File.ReadAllText(logPath), @"^\d{4}-\d{2}-\d{2}\s\d{2}:\d{2}:\d{2}.\d{3}\s", RegexOptions.Multiline).Count;

            documents.Count.Should().Be(lineCount, "Number of parsed documents should match number of timestamps in file!");
        }
    }
}