using FluentAssertions;
using Logshark.ArtifactProcessors.TableauServerLogProcessor.Parsers;
using Logshark.Tests.Helpers;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;

namespace Logshark.Tests.ServerLogProcessorTests
{
    [TestFixture, Description("Unit tests covering Hyper log parsing.")]
    public class HyperParserTests
    {
        [Test, Description("Parses a sample Hyper log line to a JObject.  This parse must not yield any properties which are empty or equal to '-', and also standardize the timestamp.")]
        public void ParseHyperLogLine()
        {
            const string sampleLogLine = @"{""ts"": ""2017-04-12T07:49:24.416"", ""pid"": 8988, ""tid"": ""3350"", ""sev"": ""info"", ""req"": ""-"", ""sess"": ""39"", ""site"": ""-"", ""user"": ""devauto"", ""k"": ""query-begin"", ""v"": {""query"": ""SET SESSIONID=\""22C0A203852A481EB196F1BB05278CD0-0:0\"""", ""transaction-visible-id"": 7, ""client-session-id"": """"}}";
            const string expectedResult = @"{""ts"":""2017-04-12T00:49:24.416-07:00"",""pid"":8988,""tid"":""3350"",""sev"":""info"",""sess"":""39"",""user"":""devauto"",""k"":""query-begin"",""v"":{""query"":""SET SESSIONID=\""22C0A203852A481EB196F1BB05278CD0-0:0\"""",""transaction-visible-id"":""7"",""client-session-id"":""""},""line"":1}";

            var actualResult = ParserTestHelpers.ParseSingleLine(sampleLogLine, new HyperParser());
            Assert.AreEqual(expectedResult, actualResult);
        }

        [Test, Description("Parses a sample hyper log file and ensures that all documents were parsed correctly.")]
        [TestCase("hyper.log", Description = "Sample full log file from Perf Test Lab Hyper logs")]
        public void ParseHyperFullFile(string logFile)
        {
            var logPath = TestDataHelper.GetServerLogProcessorResourcePath(logFile);

            IList<JObject> documents = ParserTestHelpers.ParseFile(logPath, new HyperParser());

            var lineCount = File.ReadAllLines(logPath).Length;
            documents.Count.Should().Be(lineCount, "Number of parsed documents should match number of lines in file!");
        }
    }
}