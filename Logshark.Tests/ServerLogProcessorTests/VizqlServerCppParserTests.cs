using FluentAssertions;
using Logshark.ArtifactProcessors.TableauServerLogProcessor.Parsers;
using Logshark.Tests.Helpers;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;

namespace Logshark.Tests.ServerLogProcessorTests
{
    [TestFixture]
    public class VizqlServerCppTests
    {
        [Test, Description("Parses a sample Vizqlserver C++ JSON log line to a JObject.  This parse must recursively strip any properties with value fields which are empty or equal to '-'.  It must also write out line#, filename and node.")]
        public void ParseVizqlServerCppLogLine()
        {
            const string sampleLogLine =
                @"{""ts"":""2015-08-12T18:17:24.211"",""pid"":58008,""tid"":""ccbc"",""sev"":""info"",""req"":""-"",""sess"":""-"",""site"":""-"",""user"":""-"",""k"":""PathAccessChecker"",""v"":{""this"":""0x000000009c97d410"",""matching-rules"":[{""index"":""1"",""path"":""*"",""allowed"":""1"",""type"":""allowed-config""}],""allowall-from-config"":""1""}}";
            const string expectedResult =
                @"{""ts"":""2015-08-12T11:17:24.211-07:00"",""pid"":58008,""tid"":""ccbc"",""sev"":""info"",""k"":""PathAccessChecker"",""v"":{""this"":""0x000000009c97d410"",""matching-rules"":[{""index"":""1"",""path"":""*"",""allowed"":""1"",""type"":""allowed-config""}],""allowall-from-config"":""1""},""line"":1}";

            var actualResult = ParserTestHelpers.ParseSingleLine(sampleLogLine, new VizqlServerCppParser());
            Assert.AreEqual(expectedResult, actualResult);
        }

        [Test, Description("Parses a sample Vizqlserver logfile to a collection of JObjects.")]
        [TestCase("vizqlserver_cpp.txt", Description = "Test VizQLServer log lines")]
        public void ParseVizqlServerLogFile(string logFile)
        {
            var logPath = TestDataHelper.GetServerLogProcessorResourcePath(logFile);
            const string sampleLogWorkerName = "worker2";

            IList<JObject> documents = ParserTestHelpers.ParseFile(logPath, new VizqlServerCppParser(), sampleLogWorkerName);
            var lineCount = File.ReadAllLines(logPath).Length;

            documents.Count.Should().Be(lineCount, "Number of parsed documents should match number of lines in file!");
        }
    }
}