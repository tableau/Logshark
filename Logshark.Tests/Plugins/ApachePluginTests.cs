using FluentAssertions;
using LogShark.Plugins.Apache;
using LogShark.Shared;
using LogShark.Shared.LogReading.Containers;
using LogShark.Tests.Plugins.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace LogShark.Tests.Plugins
{
    public class ApachePluginTests : InvariantCultureTestsBase
    {
        private static readonly LogFileInfo TestLogFileInfo = new LogFileInfo("test.log", @"folder1/test.log", "node1", DateTime.MinValue);

        private readonly IConfiguration _configKeepingHealthChecks;
        private readonly IConfiguration _configSkippingHealthChecks;

        public ApachePluginTests()
        {
            _configKeepingHealthChecks = CreateConfig(true);
            _configSkippingHealthChecks = CreateConfig(false);
        }

        [Fact]
        public void RunTestCases_KeepHealthChecks()
        {
            var testWriterFactory = new TestWriterFactory();
            using (var plugin = new ApachePlugin())
            {
                plugin.Configure(testWriterFactory, _configKeepingHealthChecks.GetSection("PluginsConfiguration:ApachePlugin"), null, new NullLoggerFactory());

                foreach (var testCase in _testCases)
                {
                    var logLine = testCase.GetLogLine();
                    plugin.ProcessLogLine(logLine, LogType.Apache);
                }
            }

            var expectedOutput = _testCases.Select(testCase => testCase.ExpectedOutput).ToList();
            var testWriter = testWriterFactory.Writers.Values.First() as TestWriter<ApacheEvent>;

            testWriterFactory.Writers.Count.Should().Be(1);
            testWriter.WasDisposed.Should().Be(true);
            testWriter.ReceivedObjects.Should().BeEquivalentTo(expectedOutput);
        }

        [Fact]
        public void RunTestCases_SkipHealthChecks()
        {
            var testWriterFactory = new TestWriterFactory();
            using (var plugin = new ApachePlugin())
            {
                plugin.Configure(testWriterFactory, _configSkippingHealthChecks.GetSection("PluginsConfiguration:ApachePlugin"), null, new NullLoggerFactory());

                foreach (var testCase in _testCases)
                {
                    var logLine = testCase.GetLogLine();
                    plugin.ProcessLogLine(logLine, LogType.Apache);
                }
            }

            var expectedOutput = _testCases
                .Select(testCase => testCase.ExpectedOutput)
                .Where(@event => ((dynamic)@event).RequestBody != "/favicon.ico")
                .ToList();
            testWriterFactory.Writers.Count.Should().Be(1);
            var testWriter = testWriterFactory.Writers.Values.First() as TestWriter<ApacheEvent>;
            testWriter.WasDisposed.Should().Be(true);
            testWriter.ReceivedObjects.Should().BeEquivalentTo(expectedOutput);
        }

        [Fact]
        public void BadInput()
        {
            var processingNotificationsCollector = new ProcessingNotificationsCollector(10);
            var testWriterFactory = new TestWriterFactory();
            using (var plugin = new ApachePlugin())
            {
                plugin.Configure(testWriterFactory, null, processingNotificationsCollector, new NullLoggerFactory());

                var wrongContentFormat = new LogLine(new ReadLogLineResult(123, 1234), TestLogFileInfo);
                var nullContent = new LogLine(new ReadLogLineResult(123, null), TestLogFileInfo);

                plugin.ProcessLogLine(wrongContentFormat, LogType.Apache);
                plugin.ProcessLogLine(nullContent, LogType.Apache);
            }

            testWriterFactory.AssertAllWritersAreDisposedAndEmpty(1);
            processingNotificationsCollector.TotalErrorsReported.Should().Be(2);
        }

        private readonly IList<PluginTestCase> _testCases = new List<PluginTestCase>
        {
            new PluginTestCase { // 2021.3+ w/refer stub
                LogContents = "localhost 10.55.555.555 - 2021-07-07T00:00:00.246 \"-0700\" 80 \"POST /vizportal/api/web/v1/getSessionInfo HTTP/1.1\" \"-\" 200 5502 \"39\" 36348 YNul@aDcsiH97so8z7I@CAAAAMU - - - - \"-\" \"http://referer/\" 10.44.44.444 - 200",
                LogFileInfo = TestLogFileInfo,
                LineNumber = 124,
                ExpectedOutput = new
                {
                    ContentLength = 39L,
                    FileName = TestLogFileInfo.FileName,
                    FilePath = TestLogFileInfo.FilePath,
                    LineNumber = 124,
                    Port = 80,
                    RequestBody = "/vizportal/api/web/v1/getSessionInfo",
                    Requester = "10.55.555.555",
                    RequestId = "YNul@aDcsiH97so8z7I@CAAAAMU",
                    RequestIp = "localhost",
                    RequestMethod = "POST",
                    RequestTimeMS = 36348L,
                    StatusCode = 200,
                    Timestamp = new DateTime(2021, 7, 7, 0, 0, 0, 246),
                    TimestampOffset = "-0700",
                    Worker = TestLogFileInfo.Worker,
                    XForwardedFor = "-",
                    TableauErrorSource = "-",
                    TableauErrorCode = "-",
                    TableauServiceName = "-",
                    TableauStatusCode = (int?)null,
                    TableauTrace = "-",
                    RefererStub = "http://referer/",
                    RemoteLogName = "-",
                    LocalIp = "10.44.44.444",
                    OriginalRequestStatus = 200
                }
            },

            new PluginTestCase { // 2021.3+ w/error code
                LogContents = "localhost 10.55.555.555 - 2021-07-07T00:00:00.245 \"Pacific Daylight Time\" 80 \"POST /dataserver/create.xml HTTP/1.1\" \"-\" 500 924 \"848\" 1704011 YOXsVVEy5E7ZXYVMqlbvawAAA5U Configuration 3 0xA73B8869 dataserver \"-\" \"\" 10.44.44.444 - 500",
                LogFileInfo = TestLogFileInfo,
                LineNumber = 124,
                ExpectedOutput = new
                {
                    ContentLength = 848L,
                    FileName = TestLogFileInfo.FileName,
                    FilePath = TestLogFileInfo.FilePath,
                    LineNumber = 124,
                    Port = 80,
                    RequestBody = "/dataserver/create.xml",
                    Requester = "10.55.555.555",
                    RequestId = "YOXsVVEy5E7ZXYVMqlbvawAAA5U",
                    RequestIp = "localhost",
                    RequestMethod = "POST",
                    RequestTimeMS = 1704011L,
                    StatusCode = 500,
                    Timestamp = new DateTime(2021, 7, 7, 0, 0, 0, 245),
                    TimestampOffset = "Pacific Daylight Time",
                    Worker = TestLogFileInfo.Worker,
                    XForwardedFor = "-",
                    TableauErrorSource = "Configuration",
                    TableauErrorCode = "0xA73B8869",
                    TableauServiceName = "dataserver",
                    TableauStatusCode = 3,
                    TableauTrace = "-",
                    RefererStub = "",
                    RemoteLogName = "-",
                    LocalIp = "10.44.44.444",
                    OriginalRequestStatus = 500
                }
            },
            new PluginTestCase { // 2021.3+ w/no error code
                LogContents = "localhost 10.55.555.555 - 2021-07-07T00:00:00.244 \"Pacific Daylight Time\" 80 \"POST /server/d/Queue/EEEFFFAAABBBCCCDDDEEEFFF11122233-7:1/test.xml HTTP/1.1\" \"-\" 202 4708 \"16541\" 669974 YOVQ8FEy5E7ZXYVMqlbvdgAAArg - - - - \"-\" \"\" 10.44.44.444 - 202",
                LogFileInfo = TestLogFileInfo,
                LineNumber = 124,
                ExpectedOutput = new
                {
                    ContentLength = 16541L,
                    FileName = TestLogFileInfo.FileName,
                    FilePath = TestLogFileInfo.FilePath,
                    LineNumber = 124,
                    Port = 80,
                    RequestBody = "/server/d/Queue/EEEFFFAAABBBCCCDDDEEEFFF11122233-7:1/test.xml",
                    Requester = "10.55.555.555",
                    RequestId = "YOVQ8FEy5E7ZXYVMqlbvdgAAArg",
                    RequestIp = "localhost",
                    RequestMethod = "POST",
                    RequestTimeMS = 669974L,
                    StatusCode = 202,
                    Timestamp = new DateTime(2021, 7, 7, 0, 0, 0, 244),
                    TimestampOffset = "Pacific Daylight Time",
                    Worker = TestLogFileInfo.Worker,
                    XForwardedFor = "-",
                    TableauErrorSource = "-",
                    TableauErrorCode = "-",
                    TableauServiceName = "-",
                    TableauStatusCode = (int?)null,
                    TableauTrace = "-",
                    RefererStub = "",
                    RemoteLogName = "-",
                    LocalIp = "10.44.44.444",
                    OriginalRequestStatus = 202
                }
            },

            new PluginTestCase { // Format between the format above and below that included tableau trace but nothing else from above
                LogContents = "localhost ::1 - 2019-10-03T22:25:32.975 \"Pacific Daylight Time\" 80 \"POST /dataserver/create.xml HTTP/1.1\" \"-\" 500 395 \"803\" 215505 XZbXzBPVoXiKAmfcaR4JPAAAAUM System 4 0x640D9F85 dataserver \"-\"",
                LogFileInfo = TestLogFileInfo,
                LineNumber = 124,
                ExpectedOutput = new {
                    ContentLength = 803L,
                    FileName = TestLogFileInfo.FileName,
                    FilePath = TestLogFileInfo.FilePath,
                    LineNumber = 124,
                    Port = 80,
                    RequestBody = "/dataserver/create.xml",
                    Requester = "::1",
                    RequestId = "XZbXzBPVoXiKAmfcaR4JPAAAAUM",
                    RequestIp = "localhost",
                    RequestMethod = "POST",
                    RequestTimeMS = 215505L,
                    StatusCode = 500,
                    Timestamp = new DateTime(2019, 10, 3, 22, 25, 32, 975),
                    TimestampOffset = "Pacific Daylight Time",
                    Worker = TestLogFileInfo.Worker,
                    XForwardedFor = "-",
                    TableauErrorSource = "System",
                    TableauErrorCode = "0x640D9F85",
                    TableauServiceName = "dataserver",
                    TableauStatusCode = 4,
                }
            },

            new PluginTestCase { // 2020.1+ with tableau error code line
                LogContents = "localhost ::1 - 2019-10-03T22:25:32.975 \"Pacific Daylight Time\" 80 \"POST /dataserver/create.xml HTTP/1.1\" \"-\" 500 395 \"803\" 215505 XZbXzBPVoXiKAmfcaR4JPAAAAUM System 4 0x640D9F85 dataserver",
                LogFileInfo = TestLogFileInfo,
                LineNumber = 124,
                ExpectedOutput = new {
                    ContentLength = 803L,
                    FileName = TestLogFileInfo.FileName,
                    FilePath = TestLogFileInfo.FilePath,
                    LineNumber = 124,
                    Port = 80,
                    RequestBody = "/dataserver/create.xml",
                    Requester = "::1",
                    RequestId = "XZbXzBPVoXiKAmfcaR4JPAAAAUM",
                    RequestIp = "localhost",
                    RequestMethod = "POST",
                    RequestTimeMS = 215505L,
                    StatusCode = 500,
                    Timestamp = new DateTime(2019, 10, 3, 22, 25, 32, 975),
                    TimestampOffset = "Pacific Daylight Time",
                    Worker = TestLogFileInfo.Worker,
                    XForwardedFor = "-",
                    TableauErrorSource = "System",
                    TableauErrorCode = "0x640D9F85",
                    TableauServiceName = "dataserver",
                    TableauStatusCode = 4,
                }
            },

            new PluginTestCase { // 2020.1+ w/no error code line
                LogContents = "localhost 127.0.0.1 - 2019-10-03T22:25:33.303 \"Pacific Daylight Time\" 80 \"HEAD /favicon.ico HTTP/1.1\" \"-\" 200 - \"-\" 6002 XZbXzRPVoXiKAmfcaR4JPQAAAUI - - - -",
                LogFileInfo = TestLogFileInfo,
                LineNumber = 125,
                ExpectedOutput = new {
                    ContentLength = (long?) null,
                    FileName = TestLogFileInfo.FileName,
                    FilePath = TestLogFileInfo.FilePath,
                    LineNumber = 125,
                    Port = 80,
                    RequestBody = "/favicon.ico",
                    Requester = "127.0.0.1",
                    RequestId = "XZbXzRPVoXiKAmfcaR4JPQAAAUI",
                    RequestIp = "localhost",
                    RequestMethod = "HEAD",
                    RequestTimeMS = 6002L,
                    StatusCode = 200,
                    Timestamp = new DateTime(2019, 10, 3, 22, 25, 33, 303),
                    TimestampOffset = "Pacific Daylight Time",
                    Worker = TestLogFileInfo.Worker,
                    XForwardedFor = "-",
                    TableauErrorSource = "-",
                    TableauErrorCode = "-",
                    TableauServiceName = "-",
                    TableauStatusCode = (int?)null,
                }},

            new PluginTestCase { // Random line from test logs
                LogContents = "10.177.50.147 10.177.50.147 - 2018-10-02T22:18:41.686 \"Coordinated Universal Time\" 80 \"GET /auth?language=en&api=0.31&format=xml&client_type=tabcmd HTTP/1.1\" \"-\" 200 6330 \"-\" 703082 W7Puwe2JQy69mALs70FQ4wAAAiA",
                LogFileInfo = TestLogFileInfo,
                LineNumber = 123,
                ExpectedOutput = new {
                    ContentLength = (long?) null,
                    FileName = TestLogFileInfo.FileName,
                    FilePath = TestLogFileInfo.FilePath,
                    LineNumber = 123,
                    Port = 80,
                    RequestBody = "/auth?language=en&api=0.31&format=xml&client_type=tabcmd",
                    Requester = "10.177.50.147",
                    RequestId = "W7Puwe2JQy69mALs70FQ4wAAAiA",
                    RequestIp = "10.177.50.147",
                    RequestMethod = "GET",
                    RequestTimeMS = 703082,
                    StatusCode = 200,
                    Timestamp = new DateTime(2018, 10, 2, 22, 18, 41, 686),
                    TimestampOffset = "Coordinated Universal Time",
                    Worker = TestLogFileInfo.Worker,
                    XForwardedFor = "-",
                    TableauErrorSource = "",
                    TableauErrorCode = "",
                    TableauServiceName = "",
                    TableauStatusCode = (int?)null,
                }},

            new PluginTestCase { // 2018.2 line from original LogShark tests
                LogContents = "10.210.24.3 127.0.0.1 - 2018-05-09T16:07:58.120 \"GMT Daylight Time\" 80 \"POST /vizql/w/Superstore/v/Overview/bootstrapSession/sessions/185CCDC854A44765BB0298E93B403879-0:3 HTTP/1.1\" \"-\" 200 136026 \"784\" 2370951 WvMOzgKIhfzh9kFWO@ow2gAAA1Y",
                LogFileInfo = TestLogFileInfo,
                LineNumber = 124,
                ExpectedOutput = new {
                    ContentLength = 784L,
                    FileName = TestLogFileInfo.FileName,
                    FilePath = TestLogFileInfo.FilePath,
                    LineNumber = 124,
                    Port = 80,
                    RequestBody = "/vizql/w/Superstore/v/Overview/bootstrapSession/sessions/185CCDC854A44765BB0298E93B403879-0:3",
                    Requester = "127.0.0.1",
                    RequestId = "WvMOzgKIhfzh9kFWO@ow2gAAA1Y",
                    RequestIp = "10.210.24.3",
                    RequestMethod = "POST",
                    RequestTimeMS = 2370951,
                    StatusCode = 200,
                    Timestamp = new DateTime(2018, 5, 9, 16, 7, 58, 120),
                    TimestampOffset = "GMT Daylight Time",
                    Worker = TestLogFileInfo.Worker,
                    XForwardedFor = "-",
                    TableauErrorSource = "",
                    TableauErrorCode = "",
                    TableauServiceName = "",
                    TableauStatusCode = (int?)null,
                }},

            new PluginTestCase { // 2018.2 line with missing request and bad content length
                LogContents = "10.210.24.3 127.0.0.1 - 2018-05-09T16:07:58.120 \"GMT Daylight Time\" 80 \"/vizql/w/Superstore/v/Overview/bootstrapSession/sessions/185CCDC854A44765BB0298E93B403879-0:3 HTTP/1.1\" \"-\" 200 136026 \"<script>alert(Content-Length)</script>\" 2370951 WvMOzgKIhfzh9kFWO@ow2gAAA1Y",
                LogFileInfo = TestLogFileInfo,
                LineNumber = 125,
                ExpectedOutput = new {
                    ContentLength = (long?) null,
                    FileName = TestLogFileInfo.FileName,
                    FilePath = TestLogFileInfo.FilePath,
                    LineNumber = 125,
                    Port = 80,
                    RequestBody = "/vizql/w/Superstore/v/Overview/bootstrapSession/sessions/185CCDC854A44765BB0298E93B403879-0:3",
                    Requester = "127.0.0.1",
                    RequestId = "WvMOzgKIhfzh9kFWO@ow2gAAA1Y",
                    RequestIp = "10.210.24.3",
                    RequestMethod = "",
                    RequestTimeMS = 2370951,
                    StatusCode = 200,
                    Timestamp = new DateTime(2018, 5, 9, 16, 7, 58, 120),
                    TimestampOffset = "GMT Daylight Time",
                    Worker = TestLogFileInfo.Worker,
                    XForwardedFor = "-"
                }},

            new PluginTestCase { // 2018.2 gateway health check line
                LogContents = "localhost 127.0.0.1 - 2018-10-03T00:01:14.335 \"Coordinated Universal Time\" 80 \"HEAD /favicon.ico HTTP/1.1\" \"-\" 200 - \"-\" 0 W7QGyu2JQy69mALs70FVSgAAAhA",
                LogFileInfo = TestLogFileInfo,
                LineNumber = 126,
                ExpectedOutput = new {
                    ContentLength = (long?) null,
                    FileName = TestLogFileInfo.FileName,
                    FilePath = TestLogFileInfo.FilePath,
                    LineNumber = 126,
                    Port = 80,
                    RequestBody = "/favicon.ico",
                    Requester = "127.0.0.1",
                    RequestId = "W7QGyu2JQy69mALs70FVSgAAAhA",
                    RequestIp = "localhost",
                    RequestMethod = "HEAD",
                    RequestTimeMS = 0,
                    StatusCode = 200,
                    Timestamp = new DateTime(2018, 10, 3, 0, 1, 14, 335),
                    TimestampOffset = "Coordinated Universal Time",
                    Worker = TestLogFileInfo.Worker,
                    XForwardedFor = "-"
                }},

            new PluginTestCase { // 9.x line from original LogShark tests
                LogContents = "3.209.152.107 - - 2015-07-17 00:01:04.222 Eastern Daylight Time 80 \"GET /t/Test/views/SuperstoreReport/SuperstoreReport?:iid=1&:embed=y HTTP/1.1\" \"-\" 200 18388 \"-\" 11341724 Vah@AAMoOaUAADKU7-cAAAMO",
                LogFileInfo = TestLogFileInfo,
                LineNumber = 127,
                ExpectedOutput = new {
                    ContentLength = (long?) null,
                    FileName = TestLogFileInfo.FileName,
                    FilePath = TestLogFileInfo.FilePath,
                    LineNumber = 127,
                    Port = 80,
                    RequestBody = "/t/Test/views/SuperstoreReport/SuperstoreReport?:iid=1&:embed=y",
                    Requester = "-",
                    RequestId = "Vah@AAMoOaUAADKU7-cAAAMO",
                    RequestIp = "3.209.152.107",
                    RequestMethod = "GET",
                    RequestTimeMS = 11341724,
                    StatusCode = 200,
                    Timestamp = new DateTime(2015, 7, 17, 0, 1, 4, 222),
                    TimestampOffset = "Eastern Daylight Time",
                    Worker = TestLogFileInfo.Worker,
                    XForwardedFor = "-"
                }},

            new PluginTestCase { // 9.x line with missing request and bad content length
                LogContents = "172.16.115.248 - - 2019-01-10 20:07:14.256 Eastern Standard Time 443 \"BASELINE-CONTROL /jqZPZpAJ.htm HTTP/1.1\" \"45.33.86.32\" 405 246 \"<script>alert(Content-Length)</script>\" 15632 XDfsQrLI3QXJ7VR7OZto1QAAA@M",
                LogFileInfo = TestLogFileInfo,
                LineNumber = 128,
                ExpectedOutput = new {
                    ContentLength = (long?) null,
                    FileName = TestLogFileInfo.FileName,
                    FilePath = TestLogFileInfo.FilePath,
                    LineNumber = 128,
                    Port = 443,
                    RequestBody = "BASELINE-CONTROL /jqZPZpAJ.htm",
                    Requester = "-",
                    RequestId = "XDfsQrLI3QXJ7VR7OZto1QAAA@M",
                    RequestIp = "172.16.115.248",
                    RequestMethod = "",
                    RequestTimeMS = 15632,
                    StatusCode = 405,
                    Timestamp = new DateTime(2019, 1, 10, 20, 7, 14, 256),
                    TimestampOffset = "Eastern Standard Time",
                    Worker = TestLogFileInfo.Worker,
                    XForwardedFor = "45.33.86.32"
                }},

            new PluginTestCase { // 9.x gateway health check line
                LogContents = "127.0.0.1 - - 2015-05-18 10:12:40.573 E. Australia Standard Time 80 \"HEAD /jqZPZpAJ.htm HTTP/1.1\" \"-\" 200 - \"-\" 0 VVkueAr1Ay4AAEQENzQAAAJO",
                LogFileInfo = TestLogFileInfo,
                LineNumber = 129,
                ExpectedOutput = new {
                    ContentLength = (long?) null,
                    FileName = TestLogFileInfo.FileName,
                    FilePath = TestLogFileInfo.FilePath,
                    LineNumber = 129,
                    Port = 80,
                    RequestBody = "/jqZPZpAJ.htm",
                    Requester = "-",
                    RequestId = "VVkueAr1Ay4AAEQENzQAAAJO",
                    RequestIp = "127.0.0.1",
                    RequestMethod = "HEAD",
                    RequestTimeMS = 0,
                    StatusCode = 200,
                    Timestamp = new DateTime(2015, 5, 18, 10, 12, 40, 573),
                    TimestampOffset = "E. Australia Standard Time",
                    Worker = TestLogFileInfo.Worker,
                    XForwardedFor = "-"
                }},

            new PluginTestCase { // 2018.2 line with no body
                LogContents = "localhost 45.56.79.13 - 2019-07-17T07:33:30.408 \"+0000\" 8000 \"\" \"-\" 400 226 \"-\" 15 -",
                LogFileInfo = TestLogFileInfo,
                LineNumber = 130,
                ExpectedOutput = new {
                    FileName = TestLogFileInfo.FileName,
                    FilePath = TestLogFileInfo.FilePath,
                    LineNumber = 130,
                    Port = 8000,
                    RequestBody = "",
                    Requester = "45.56.79.13",
                    RequestId = "-",
                    RequestIp = "localhost",
                    RequestMethod = "",
                    RequestTimeMS = 15L,
                    StatusCode = 400,
                    Timestamp = new DateTime(2019, 7, 17, 07, 33, 30, 408),
                    TimestampOffset = "+0000",
                    Worker = TestLogFileInfo.Worker,
                    XForwardedFor = "-"
                }},

            new PluginTestCase { // 9.x line from original LogShark tests modifed with no body
                LogContents = "3.209.152.107 - - 2015-07-17 00:01:04.222 Eastern Daylight Time 80 \"\" \"-\" 200 18388 \"-\" 11341724 Vah@AAMoOaUAADKU7-cAAAMO",
                LogFileInfo = TestLogFileInfo,
                LineNumber = 131,
                ExpectedOutput = new {
                    ContentLength = (long?) null,
                    FileName = TestLogFileInfo.FileName,
                    FilePath = TestLogFileInfo.FilePath,
                    LineNumber = 131,
                    Port = 80,
                    RequestBody = "",
                    Requester = "-",
                    RequestId = "Vah@AAMoOaUAADKU7-cAAAMO",
                    RequestIp = "3.209.152.107",
                    RequestMethod = "",
                    RequestTimeMS = 11341724,
                    StatusCode = 200,
                    Timestamp = new DateTime(2015, 7, 17, 0, 1, 4, 222),
                    TimestampOffset = "Eastern Daylight Time",
                    Worker = TestLogFileInfo.Worker,
                    XForwardedFor = "-"
                }},
        };

        private static IConfiguration CreateConfig(bool keepHealthChecksValue)
        {
            return ConfigGenerator.GetConfigWithASingleValue(
                "PluginsConfiguration:ApachePlugin:IncludeGatewayChecks",
                keepHealthChecksValue.ToString());
        }
    }
}