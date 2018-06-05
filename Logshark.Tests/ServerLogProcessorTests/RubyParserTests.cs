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
    [TestFixture, Description("Unit tests covering ruby log parsing.")]
    public class RubyParserTests
    {
        [Test, Description("Parses a sample tabadmin.log file into a collection of JSON documents.")]
        [TestCase("tabadmin.log")]
        public void ParseTabAdminLog(string logFile)
        {
            var logPath = TestDataHelper.GetServerLogProcessorResourcePath(logFile);

            IList<JObject> documents = ParserTestHelpers.ParseFile(logPath, new TabAdminParser());

            // Count number of actual log events by counting the number of timestamps
            var lineCount = Regex.Matches(File.ReadAllText(logPath), @"\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}.\d{3} [+|-]\d{4}_[A-Z]").Count;

            documents.Count.Should().Be(lineCount, "Number of parsed documents should match number of lines in file!");
        }
    }
}