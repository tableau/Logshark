using FluentAssertions;
using Logshark.ArtifactProcessors.TableauServerLogProcessor.Parsers;
using Logshark.Tests.Helpers;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using System.Collections.Generic;

namespace Logshark.Tests.ServerLogProcessorTests
{
    [TestFixture, Description("Unit tests covering buildversion.txt log parsing.")]
    public class BuildVersionParserTests
    {
        [Test, Description("Parses a sample buildversion logfile.")]
        [TestCase("buildversion.txt")]
        public void ParseBuildVersionLogFile(string logFile)
        {
            var logPath = TestDataHelper.GetServerLogProcessorResourcePath(logFile);
            const string sampleLogWorkerName = "worker2";

            IList<JObject> documents = ParserTestHelpers.ParseFile(logPath, new BuildVersionParser(), sampleLogWorkerName);

            documents.Count.Should().Be(1, "Should have one parsed document!");
        }
    }
}