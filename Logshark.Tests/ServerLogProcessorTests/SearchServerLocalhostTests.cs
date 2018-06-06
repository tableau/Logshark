using FluentAssertions;
using Logshark.ArtifactProcessors.TableauServerLogProcessor.Parsers;
using Logshark.Tests.Helpers;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;

namespace Logshark.Tests.ServerLogProcessorTests
{
    [TestFixture, Description("Unit tests covering SearchServerLocalhost log parsing.")]
    public class SearchServerLocalhostTests
    {
        [Test, Description("Parses a sample searchserverlocalhost logfile into a collection of JSON documents.")]
        [TestCase("searchserverlocalhost.txt")]
        public void ParseSearchServerLocalhostFullFile(string logFile)
        {
            var logPath = TestDataHelper.GetServerLogProcessorResourcePath(logFile);

            IList<JObject> documents = ParserTestHelpers.ParseFile(logPath, new SearchServerLocalhostParser());

            var lineCount = File.ReadAllLines(logPath).Length;

            documents.Count.Should().Be(lineCount, "Number of parsed documents should match number of lines in file!");
        }
    }
}