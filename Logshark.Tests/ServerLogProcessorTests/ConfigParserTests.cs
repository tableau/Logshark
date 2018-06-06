using FluentAssertions;
using Logshark.ArtifactProcessors.TableauServerLogProcessor.Parsers;
using Logshark.Tests.Helpers;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using System.Collections.Generic;

namespace Logshark.Tests.ServerLogProcessorTests
{
    [TestFixture, Description("Unit tests covering config log parsing.")]
    public class ConfigParserTests
    {
        private const string SampleLogWorkerName = "worker2";

        [Test, Description("Parses a sample TabSvc.yml logfile to a Json document.")]
        [TestCase("tabsvc.yml")]
        public void ParseTabSvcYamlLogFile(string logFile)
        {
            var logPath = TestDataHelper.GetServerLogProcessorResourcePath(logFile);

            IList<JObject> documents = ParserTestHelpers.ParseFile(logPath, new ConfigYamlParser(), SampleLogWorkerName);

            documents.Count.Should().Be(1, "Should have parsed exactly one document from one config file!");
        }

        [Test, Description("Parses a sample Workgroup.yml logfile to a Json document.")]
        [TestCase("workgroup.yml")]
        public void ParseWorkgroupYamlLogFile(string logFile)
        {
            var logPath = TestDataHelper.GetServerLogProcessorResourcePath(logFile);

            IList<JObject> documents = ParserTestHelpers.ParseFile(logPath, new ConfigYamlParser(), SampleLogWorkerName);

            documents.Count.Should().Be(1, "Should have parsed exactly one document from one config file!");
        }

        [Test, Description("Parses a sample pg_hba.conf logfile to a Json document.")]
        [TestCase("pg_hba.conf")]
        public void ParsePostgresHbaConfFile(string logFile)
        {
            var logPath = TestDataHelper.GetServerLogProcessorResourcePath(logFile);

            IList<JObject> documents = ParserTestHelpers.ParseFile(logPath, new PostgresHostConfigParser(), SampleLogWorkerName);

            documents.Count.Should().Be(1, "Should have parsed exactly one document from one config file!");
        }

        [Test, Description("Parses a sample connections.properties logfile to a Json document.")]
        [TestCase("connections.properties")]
        public void ParseConnectionsPropertiesFile(string logFile)
        {
            var logPath = TestDataHelper.GetServerLogProcessorResourcePath(logFile);
            const string sampleLogWorerName = "worker2";

            IList<JObject> documents = ParserTestHelpers.ParseFile(logPath, new ConnectionsConfigParser(), SampleLogWorkerName);

            documents.Count.Should().Be(1, "Should have parsed exactly one document from one config file!");
        }
    }
}