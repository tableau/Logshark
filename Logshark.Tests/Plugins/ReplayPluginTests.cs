using FluentAssertions;
using LogShark.Tests.Plugins.Helpers;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using LogShark.Plugins.Replayer;
using LogShark.Shared;
using LogShark.Shared.LogReading.Containers;
using Xunit;

namespace LogShark.Tests.Plugins
{
    public class ReplayerPluginTests : InvariantCultureTestsBase
    {
        private static readonly LogFileInfo TestLogFileInfo = new LogFileInfo("test.log", @"folder1/test.log", "node1", DateTime.MinValue);

        [Fact]
        public void RunTestCases_ReplayPlugin()
        {
            var testWriterFactory = new TestWriterFactory();
            foreach (var testCase in _testCases)
            {
                var jsonFile = $"Playback{testCase.LineNumber}.json";
                var plugin = new ReplayerPlugin();
                var configDict = new Dictionary<string, string>() {
                        { "ReplayerOutputDirectory", Path.Combine(Directory.GetCurrentDirectory(), "ReplayerOutput") },
                        { "ReplayFileName", jsonFile } };
                var config = ConfigGenerator.GetConfigFromDictionary(configDict);
                plugin.Configure(testWriterFactory, config, null, new NullLoggerFactory());

                var testLines = testCase.LogContents.ToString();
                var logLines = testLines.Split("\n");

                // the first line will be the Apache event
                var apacheLine = logLines[0];
                LogLine l1 = new LogLine(new ReadLogLineResult(1, apacheLine), testCase.LogFileInfo);
                plugin.ProcessLogLine(l1, LogType.Apache);

                // the remaining lines will be the VizqlserverCpp events
                for (var i = 1; i < logLines.Length; i++)
                {
                    var l2 = new LogLine(new ReadLogLineResult(i + 1, JsonConvert.DeserializeObject<NativeJsonLogsBaseEvent>(logLines[i])), testCase.LogFileInfo);
                    plugin.ProcessLogLine(l2, LogType.VizqlserverCpp);
                }

                plugin.CompleteProcessing();
                var fullJsonFile = Path.Combine("ReplayerOutput", jsonFile);
                var output = File.ReadAllText(fullJsonFile);

                var actualJson = JToken.Parse(output);
                var expectedJson = JToken.Parse(testCase.ExpectedOutput.ToString());
                actualJson.ToString().Should().Be(expectedJson.ToString());

                if (File.Exists(fullJsonFile))
                {
                    File.Delete(fullJsonFile);
                }
            }
        }

        private readonly IList<PluginTestCase> _testCases = new List<PluginTestCase>
        {
            // test end-bootstrap-session vizqlserver logs
            new PluginTestCase {
                LogContents = "user12 10.30.216.213 - 2019-10-11T17:48:35.268 \"Pacific Daylight Time\" 80 \"GET /views/Regional/Obesity?:embed=y&:showVizHome=n&:toolbar=top&%3Arefresh=false&%3AopenAuthoringInTopWindow=true"
                            + "&%3AbrowserBackButtonUndo=true&%3AcommentingEnabled=true&%3AreloadOnCustomViewSave=true&%3AshowAppBanner=false&%3AisVizPortal=true&:apiID=host0 HTTP/1.1\" \"-\" 200 13013 \"-\" 2686239 "
                            + "XaEi470GP6IieaeE6Qlz7AAAA@M\n"
                            + "{\"ts\":\"2019-10-11T17:49:34.064\",\"pid\":65000,\"tid\":\"37c0\",\"sev\":\"info\",\"req\":\"XaEi470GP6IieaeE6Qlz7AAAA@M\",\"sess\":\"-\",\"site\":\"Default\",\"user\":\"user1\","
                            + "\"k\":\"begin-commands-controller.invoke-command\",\"a\":{},\"v\":{\"args\": \"tabsrv:refresh-server delta-time-ms=\\\"4302820\\\" should-refresh-ds=\\\"true\\\"\",  \"name\": \"tabsrv:refresh-server\"}"
                            + ",\"ctx\":{\"vw\":\"FlightDelays\",\"wb\":\"Regional\"}}\n"
                            + "{\"ts\":\"2019-10-11T17:49:34.064\",\"pid\":65000,\"tid\":\"37c0\",\"sev\":\"info\",\"req\":\"XaEi470GP6IieaeE6Qlz7AAAA@M\",\"sess\":\"+\",\"site\":\"Default\",\"user\":\"user1\","
                            + "\"k\":\"server-telemetry\",\"a\":{},\"v\":{\"request-info\":{\"rid\":\"XaEi470GP6IieaeE6Qlz7AAAA@M\",\"action-type\":\"bootstrap-session\"}, \"sid\":\"+\"},\"ctx\":{\"vw\":\"FlightDelays\",\"wb\":\"Regional\"}}\n"
                            + "{\"ts\":\"2019-10-11T17:49:34.064\",\"pid\":65000,\"tid\":\"37c0\",\"sev\":\"info\",\"req\":\"XaEi470GP6IieaeE6Qlz7AAAA@M\",\"sess\":\"+\",\"site\":\"Default\",\"user\":\"user1\","
                            + "\"k\":\"end-bootstrap-session\",\"a\":{},\"v\":{\"new-session\":\"true\",\"new-session-id\":\"-\"},\"ctx\":{\"vw\":\"FlightDelays\",\"wb\":\"Regional\"}}",
                LogFileInfo = TestLogFileInfo,
                LineNumber = 1,
                ExpectedOutput = new string("[{"
                            +  "\"Url\": \"/views/Regional/Obesity?:embed=y&:showVizHome=n&:toolbar=top&%3Arefresh=false&%3AopenAuthoringInTopWindow=true&%3AbrowserBackButtonUndo=true&%3AcommentingEnabled=true&%3AreloadOnCustomViewSave=true&%3AshowAppBanner=false&%3AisVizPortal=true&:apiID=host0\","
                            +  "\"BrowserStartTime\": \"2019-10-12T00:48:35.268Z\","
                            +  "\"User\": \"user1\","
                            +  "\"HttpStatus\": \"200\","
                            +  "\"AccessRequestID\": \"XaEi470GP6IieaeE6Qlz7AAAA@M\","
                            +  "\"LoadTime\": \"2686239\","
                            +  "\"VizqlSession\": \"+\","
                            +  "\"Commands\": [{"
                            +  "  \"Time\": \"2019-10-12T00:49:34.064Z\","
                            +  "  \"Command\": {"
                            +  "    \"commandNamespace\": \"tabsrv\","
                            +  "    \"commandName\": \"refresh-server\","
                            +  "    \"commandParams\": {"
                            +  "    \"deltaTimeMs\": \"4302820\","
                            +  "    \"shouldRefreshDs\": \"true\""
                            +  "  }"
                            +  "}"
                            +  "}]"
                            +"}]")
            },

            // test server-telemetry vizqlserver logs
            new PluginTestCase {
                LogContents = "user12 10.30.216.213 - 2019-10-11T17:48:35.268 \"Pacific Daylight Time\" 80 \"GET /views/Regional/Obesity?:embed=y&:showVizHome=n&:toolbar=top&%3Arefresh=false&%3AopenAuthoringInTopWindow=true"
                            + "&%3AbrowserBackButtonUndo=true&%3AcommentingEnabled=true&%3AreloadOnCustomViewSave=true&%3AshowAppBanner=false&%3AisVizPortal=true&:apiID=host0 HTTP/1.1\" \"-\" 200 13013 \"-\" 2686239 "
                            + "XaEi470GP6IieaeE6Qlz7AAAA@M\n"
                            + "{\"ts\":\"2019-10-11T17:49:34.064\",\"pid\":65000,\"tid\":\"37c0\",\"sev\":\"info\",\"req\":\"XaEi470GP6IieaeE6Qlz7AAAA@M\",\"sess\":\"-\",\"site\":\"Default\",\"user\":\"user1\","
                            + "\"k\":\"begin-commands-controller.invoke-command\",\"a\":{},\"v\":{\"args\": \"tabsrv:refresh-server delta-time-ms=\\\"4302820\\\" should-refresh-ds=\\\"true\\\"\",  \"name\": \"tabsrv:refresh-server\"}"
                            + ",\"ctx\":{\"vw\":\"FlightDelays\",\"wb\":\"Regional\"}}\n"
                            + "{\"ts\":\"2019-10-11T17:49:34.064\",\"pid\":65000,\"tid\":\"37c0\",\"sev\":\"info\",\"req\":\"XaEi470GP6IieaeE6Qlz7AAAA@M\",\"sess\":\"-\",\"site\":\"Default\",\"user\":\"user1\","
                            + "\"k\":\"server-telemetry\",\"a\":{},\"v\":{\"request-info\":{\"rid\":\"XaEi470GP6IieaeE6Qlz7AAAA@M\",\"action-type\":\"bootstrap-session\"}, \"sid\":\"-\"},\"ctx\":{\"vw\":\"FlightDelays\",\"wb\":\"Regional\"}}",
                LogFileInfo = TestLogFileInfo,
                LineNumber = 2,
                ExpectedOutput = new string("[{"
                            +  "\"Url\": \"/views/Regional/Obesity?:embed=y&:showVizHome=n&:toolbar=top&%3Arefresh=false&%3AopenAuthoringInTopWindow=true&%3AbrowserBackButtonUndo=true&%3AcommentingEnabled=true&%3AreloadOnCustomViewSave=true&%3AshowAppBanner=false&%3AisVizPortal=true&:apiID=host0\","
                            +  "\"BrowserStartTime\": \"2019-10-12T00:48:35.268Z\","
                            +  "\"User\": \"user1\","
                            +  "\"HttpStatus\": \"200\","
                            +  "\"AccessRequestID\": \"XaEi470GP6IieaeE6Qlz7AAAA@M\","
                            +  "\"LoadTime\": \"2686239\","
                            +  "\"VizqlSession\": \"-\","
                            +  "\"Commands\": [{"
                            +  "  \"Time\": \"2019-10-12T00:49:34.064Z\","
                            +  "  \"Command\": {"
                            +  "    \"commandNamespace\": \"tabsrv\","
                            +  "    \"commandName\": \"refresh-server\","
                            +  "    \"commandParams\": {"
                            +  "    \"deltaTimeMs\": \"4302820\","
                            +  "    \"shouldRefreshDs\": \"true\""
                            +  "  }"
                            +  "}"
                            +  "}]"
                            +"}]")
            },

            // test lock-session vizqlserver logs
            new PluginTestCase {
                LogContents = "user12 10.30.216.213 - 2019-10-11T17:48:35.268 \"Pacific Daylight Time\" 80 \"GET /views/Regional/Obesity?:embed=y&:showVizHome=n&:toolbar=top&%3Arefresh=false&%3AopenAuthoringInTopWindow=true"
                            + "&%3AbrowserBackButtonUndo=true&%3AcommentingEnabled=true&%3AreloadOnCustomViewSave=true&%3AshowAppBanner=false&%3AisVizPortal=true&:apiID=host0 HTTP/1.1\" \"-\" 200 13013 \"-\" 2686239 "
                            + "XaEi470GP6IieaeE6Qlz7AAAA@M\n"
                            + "{\"ts\":\"2019-10-11T17:49:34.064\",\"pid\":65000,\"tid\":\"37c0\",\"sev\":\"info\",\"req\":\"XaEi470GP6IieaeE6Qlz7AAAA@M\",\"sess\":\"-\",\"site\":\"Default\",\"user\":\"user1\","
                            + "\"k\":\"begin-commands-controller.invoke-command\",\"a\":{},\"v\":{\"args\": \"tabsrv:refresh-server delta-time-ms=\\\"4302820\\\" should-refresh-ds=\\\"true\\\"\",  \"name\": \"tabsrv:refresh-server\"}"
                            + ",\"ctx\":{\"vw\":\"FlightDelays\",\"wb\":\"Regional\"}}\n"
                            + "{\"ts\":\"2019-10-11T17:49:34.064\",\"pid\":65000,\"tid\":\"37c0\",\"sev\":\"info\",\"req\":\"XaEi470GP6IieaeE6Qlz7AAAA@M\",\"sess\":\"-\",\"site\":\"Default\",\"user\":\"user1\","
                            + "\"k\":\"lock-session\",\"a\":{},\"v\":{},\"ctx\":{\"vw\":\"FlightDelays\",\"wb\":\"Regional\"}}",
                LogFileInfo = TestLogFileInfo,
                LineNumber = 3,
                ExpectedOutput = new string("[{"
                            +  "\"Url\": \"/views/Regional/Obesity?:embed=y&:showVizHome=n&:toolbar=top&%3Arefresh=false&%3AopenAuthoringInTopWindow=true&%3AbrowserBackButtonUndo=true&%3AcommentingEnabled=true&%3AreloadOnCustomViewSave=true&%3AshowAppBanner=false&%3AisVizPortal=true&:apiID=host0\","
                            +  "\"BrowserStartTime\": \"2019-10-12T00:48:35.268Z\","
                            +  "\"User\": \"user1\","
                            +  "\"HttpStatus\": \"200\","
                            +  "\"AccessRequestID\": \"XaEi470GP6IieaeE6Qlz7AAAA@M\","
                            +  "\"LoadTime\": \"2686239\","
                            +  "\"VizqlSession\": \"-\","
                            +  "\"Commands\": [{"
                            +  "  \"Time\": \"2019-10-12T00:49:34.064Z\","
                            +  "  \"Command\": {"
                            +  "    \"commandNamespace\": \"tabsrv\","
                            +  "    \"commandName\": \"refresh-server\","
                            +  "    \"commandParams\": {"
                            +  "    \"deltaTimeMs\": \"4302820\","
                            +  "    \"shouldRefreshDs\": \"true\""
                            +  "  }"
                            +  "}"
                            +  "}]"
                            +"}]")
            },

            // test lock-session vizqlserver logs with site value
            new PluginTestCase {
                LogContents = "user12 10.30.216.213 - 2019-10-11T17:48:35.268 \"Pacific Daylight Time\" 80 \"GET /t/MySite/views/Regional/Obesity?:embed=y&:showVizHome=n&:toolbar=top&%3Arefresh=false&%3AopenAuthoringInTopWindow=true"
                            + "&%3AbrowserBackButtonUndo=true&%3AcommentingEnabled=true&%3AreloadOnCustomViewSave=true&%3AshowAppBanner=false&%3AisVizPortal=true&:apiID=host0 HTTP/1.1\" \"-\" 200 13013 \"-\" 2686239 "
                            + "XaEi470GP6IieaeE6Qlz7AAAA@M\n"
                            + "{\"ts\":\"2019-10-11T17:49:34.064\",\"pid\":65000,\"tid\":\"37c0\",\"sev\":\"info\",\"req\":\"XaEi470GP6IieaeE6Qlz7AAAA@M\",\"sess\":\"-\",\"site\":\"Default\",\"user\":\"user1\","
                            + "\"k\":\"begin-commands-controller.invoke-command\",\"a\":{},\"v\":{\"args\": \"tabsrv:refresh-server delta-time-ms=\\\"4302820\\\" should-refresh-ds=\\\"true\\\"\",  \"name\": \"tabsrv:refresh-server\"}"
                            + ",\"ctx\":{\"vw\":\"FlightDelays\",\"wb\":\"Regional\"}}\n"
                            + "{\"ts\":\"2019-10-11T17:49:34.064\",\"pid\":65000,\"tid\":\"37c0\",\"sev\":\"info\",\"req\":\"XaEi470GP6IieaeE6Qlz7AAAA@M\",\"sess\":\"-\",\"site\":\"Default\",\"user\":\"user1\","
                            + "\"k\":\"lock-session\",\"a\":{},\"v\":{},\"ctx\":{\"vw\":\"FlightDelays\",\"wb\":\"Regional\"}}",
                LogFileInfo = TestLogFileInfo,
                LineNumber = 4,
                ExpectedOutput = new string("[{"
                            +  "\"Url\": \"/t/MySite/views/Regional/Obesity?:embed=y&:showVizHome=n&:toolbar=top&%3Arefresh=false&%3AopenAuthoringInTopWindow=true&%3AbrowserBackButtonUndo=true&%3AcommentingEnabled=true&%3AreloadOnCustomViewSave=true&%3AshowAppBanner=false&%3AisVizPortal=true&:apiID=host0\","
                            +  "\"BrowserStartTime\": \"2019-10-12T00:48:35.268Z\","
                            +  "\"User\": \"user1\","
                            +  "\"HttpStatus\": \"200\","
                            +  "\"AccessRequestID\": \"XaEi470GP6IieaeE6Qlz7AAAA@M\","
                            +  "\"LoadTime\": \"2686239\","
                            +  "\"VizqlSession\": \"-\","
                            +  "\"Commands\": [{"
                            +  "  \"Time\": \"2019-10-12T00:49:34.064Z\","
                            +  "  \"Command\": {"
                            +  "    \"commandNamespace\": \"tabsrv\","
                            +  "    \"commandName\": \"refresh-server\","
                            +  "    \"commandParams\": {"
                            +  "    \"deltaTimeMs\": \"4302820\","
                            +  "    \"shouldRefreshDs\": \"true\""
                            +  "  }"
                            +  "}"
                            +  "}]"
                            +"}]")
            },

            // test request that is neither views nor authoring
            new PluginTestCase {
                LogContents = "user12 10.30.216.213 - 2019-10-11T17:48:35.268 \"Pacific Daylight Time\" 80 \"GET /invalid/Regional/Obesity?:embed=y&:showVizHome=n&:toolbar=top&%3Arefresh=false&%3AopenAuthoringInTopWindow=true"
                            + "&%3AbrowserBackButtonUndo=true&%3AcommentingEnabled=true&%3AreloadOnCustomViewSave=true&%3AshowAppBanner=false&%3AisVizPortal=true&:apiID=host0 HTTP/1.1\" \"-\" 200 13013 \"-\" 2686239 "
                            + "XaEi470GP6IieaeE6Qlz7AAAA@M\n"
                            + "{\"ts\":\"2019-10-11T17:49:34.064\",\"pid\":65000,\"tid\":\"37c0\",\"sev\":\"info\",\"req\":\"XaEi470GP6IieaeE6Qlz7AAAA@M\",\"sess\":\"-\",\"site\":\"Default\",\"user\":\"user1\","
                            + "\"k\":\"begin-request\",\"l\":{},\"a\":{\"depth\":0,\"id\":\"0eglvjSl0hbKDrt2jH2v/y\",\"name\":\"request\",\"req-desc\":\"/show\",\"type\":\"begin\"},\"v\":{},\"ctx\":{\"vw\":\"FlightDelays\",\"wb\":\"Regional\"}}",
                LogFileInfo = TestLogFileInfo,
                LineNumber = 5,
                ExpectedOutput = new string("[]")
            },
            
            // test no commands
            new PluginTestCase {
                LogContents = "user12 10.30.216.213 - 2019-10-11T17:48:35.268 \"Pacific Daylight Time\" 80 \"GET /views/Regional/Obesity?:embed=y&:showVizHome=n&:toolbar=top&%3Arefresh=false&%3AopenAuthoringInTopWindow=true"
                            + "&%3AbrowserBackButtonUndo=true&%3AcommentingEnabled=true&%3AreloadOnCustomViewSave=true&%3AshowAppBanner=false&%3AisVizPortal=true&:apiID=host0 HTTP/1.1\" \"-\" 200 13013 \"-\" 2686239 "
                            + "XaEi470GP6IieaeE6Qlz7AAAA@M\n"
                            + "{\"ts\":\"2019-10-11T17:49:34.064\",\"pid\":65000,\"tid\":\"37c0\",\"sev\":\"info\",\"req\":\"XaEi470GP6IieaeE6Qlz7AAAA@M\",\"sess\":\"-\",\"site\":\"Default\",\"user\":\"user1\","
                            + "\"k\":\"begin-request\",\"l\":{},\"a\":{\"depth\":0,\"id\":\"0eglvjSl0hbKDrt2jH2v/y\",\"name\":\"request\",\"req-desc\":\"/show\",\"type\":\"begin\"},\"v\":{},\"ctx\":{\"vw\":\"FlightDelays\",\"wb\":\"Regional\"}}",
                LogFileInfo = TestLogFileInfo,
                LineNumber = 6,
                ExpectedOutput = new string("[{"
                            +  "\"Url\": \"/views/Regional/Obesity?:embed=y&:showVizHome=n&:toolbar=top&%3Arefresh=false&%3AopenAuthoringInTopWindow=true&%3AbrowserBackButtonUndo=true&%3AcommentingEnabled=true&%3AreloadOnCustomViewSave=true&%3AshowAppBanner=false&%3AisVizPortal=true&:apiID=host0\","
                            +  "\"BrowserStartTime\": \"2019-10-12T00:48:35.268Z\","
                            +  "\"User\": null,"
                            +  "\"HttpStatus\": \"200\","
                            +  "\"AccessRequestID\": \"XaEi470GP6IieaeE6Qlz7AAAA@M\","
                            +  "\"LoadTime\": \"2686239\","
                            +  "\"VizqlSession\": null,"
                            +  "\"Commands\": []"
                            +"}]")
            }
        };

    }
}