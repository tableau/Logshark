using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using LogShark.Plugins.ServerTelemetry;
using LogShark.Shared;
using LogShark.Shared.LogReading.Containers;
using LogShark.Tests.Plugins.Helpers;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using Xunit;

namespace LogShark.Tests.Plugins
{
    public class ServerTelemetryPluginTests : InvariantCultureTestsBase
    {
        private static readonly LogFileInfo TestLogFileInfo = new LogFileInfo("nativeapi_vizqlserver_2-0_2019_04_29_00_00_00.txt", @"folder1/nativeapi_vizqlserver_2-0_2019_04_29_00_00_00.txt", "worker2", new DateTime(2019, 05, 13, 13, 33, 31));
        
        [Fact]
        public void BadAndNoOpInput()
        {
            var processingNotificationsCollector = new ProcessingNotificationsCollector(10);
            var testWriterFactory = new TestWriterFactory();
            using (var plugin = new ServerTelemetryPlugin())
            {
                plugin.Configure(testWriterFactory, null, processingNotificationsCollector, new NullLoggerFactory());
                
                var wrongContentFormat = new LogLine(new ReadLogLineResult(123, "ServerTelemetry doesn't expect string"), TestLogFileInfo);
                var nullContent = new LogLine(new ReadLogLineResult(123, null), TestLogFileInfo);
                var differentEventType = new LogLine(new ReadLogLineResult(123, new NativeJsonLogsBaseEvent { EventType = "msg"}), TestLogFileInfo); // This doesn't generate error, as it is a normal condition
                var payloadIsNull = new LogLine(new ReadLogLineResult(123, new NativeJsonLogsBaseEvent { EventType = "server-telemetry"}), TestLogFileInfo);
                
                plugin.ProcessLogLine(wrongContentFormat, LogType.VizqlserverCpp);
                plugin.ProcessLogLine(nullContent, LogType.VizqlserverCpp);
                plugin.ProcessLogLine(differentEventType, LogType.VizqlserverCpp);
                plugin.ProcessLogLine(payloadIsNull, LogType.VizqlserverCpp);
            }

            testWriterFactory.AssertAllWritersAreDisposedAndEmpty(2);
            processingNotificationsCollector.TotalErrorsReported.Should().Be(3);
        }
        
        [Fact]
        public void ServerTelemetryPluginTest()
        {
            var testWriterFactory = new TestWriterFactory();
            using (var plugin = new ServerTelemetryPlugin())
            {
                plugin.Configure(testWriterFactory, null, null, new NullLoggerFactory());

                foreach (var testCase in _testCases)
                {
                    var logLine = testCase.GetLogLine();
                    plugin.ProcessLogLine(logLine, testCase.LogType);
                }
            }

            var testEventWriter = testWriterFactory.Writers.Values.First() as TestWriter<ServerTelemetryEvent>;
            var testMetricWriter = testWriterFactory.Writers.Values.Last() as TestWriter<ServerTelemetryMetric>;

            testWriterFactory.Writers.Count.Should().Be(2);
            testEventWriter.WasDisposed.Should().Be(true);
            testMetricWriter.WasDisposed.Should().Be(true);

            var expectedOutput = _testCases.Select(testCase => (Tuple<object, object[]>)testCase.ExpectedOutput).ToList();
            var expectedEvents = expectedOutput.Select(i => i.Item1);
            var expectedMetrics = expectedOutput.SelectMany(i => i.Item2);

            testEventWriter.ReceivedObjects.Should().BeEquivalentTo(expectedEvents);
            testMetricWriter.ReceivedObjects.Should().BeEquivalentTo(expectedMetrics);
        }

        private readonly List<PluginTestCase> _testCases = new List<PluginTestCase>
        {
            new PluginTestCase
            {
                LogType = LogType.VizqlserverCpp,
                LogContents = JsonConvert.DeserializeObject<NativeJsonLogsBaseEvent>(@"{""ts"":""2019-04-29T05:43:12.214"",""pid"":14932,""tid"":""44a8"",""sev"":""info"",""req"":""-"",""sess"":""-"",""site"":""-"",""user"":""-"",""k"":""server-telemetry"",""v"":{""device-pixel-ratio"":1,""dsd-device-type"":""desktop"",""request-info"":{""action-name"":""ensure-layout-for-sheet"",""action-result-size-bytes"":853240,""action-timestamp"":""2019-04-29T05:43:12.162"",""action-type"":""command"",""annotation-count"":0,""client-render-mode"":true,""customshape-count"":1,""customshape-pixel-count"":1600,""encoding-count"":394,""filterfield-count"":718,""height"":1500,""is-dashboard"":true,""mark-count"":391,""marklabel-count"":236,""metrics"":{""DOMParser_ParseXmlInputSource"":{""count"":339,""max"":1,""min"":0,""total-time-ms"":3},""DOMParser_ParseXmlString"":{""count"":92,""max"":1,""min"":0,""total-time-ms"":3},""DataConnection_connect"":{""count"":6,""max"":103,""min"":0,""total-time-ms"":167},""ExecutingCommand_PerformingIt"":{""count"":1,""max"":381,""min"":381,""total-time-ms"":381},""ExecutingCommand_SerializingResult"":{""count"":1,""max"":3,""min"":3,""total-time-ms"":3},""FederationEngine_AcquireLocks"":{""count"":17,""max"":4343,""min"":0,""total-time-ms"":12671}},""node-count"":122,""num-views"":9,""num-zones"":15,""pane-count"":15,""refline-count"":0,""repository-url"":""DailyDashboard/Transactions"",""rid"":""XMbHKYhYMB5xZxuoriES9QAAA@I"",""session-state"":""private"",""sheetname"":""Transactions"",""textmark-count"":242,""tooltip-count"":1341,""transparent-linemark-count"":0,""vertex-count"":0,""width"":1366},""sid"":""6D4D7873390542ACA4A0D6B1A7B12C83-2:0"",""sitename"":""Enterprise Business Intelligence"",""user-agent"":""Mozilla/5.0 (Windows NT 6.1; WOW64; Trident/7.0; rv:11.0) like Gecko"",""username"":""LASHS"",""workbook-name"":""Daily Dashboard""}}"),
                LogFileInfo = TestLogFileInfo,
                LineNumber = 2501,
                ExpectedOutput = new Tuple<object, object[]>(
                    new {
                        ActionName = "ensure-layout-for-sheet",
                        ActionSizeBytes = 853240,
                        ActionType = "command",
                        AnnotationCount = 0,
                        ClientRenderMode = "True",
                        CustomShapeCount = 1,
                        CustomShapePixelCount = 1600,
                        DevicePixelRatio = 1,
                        DsdDeviceType = "desktop",
                        EncodingCount = 394,
                        FilterFieldCount = 718,
                        FileName = "nativeapi_vizqlserver_2-0_2019_04_29_00_00_00.txt",
                        FilePath = @"folder1/nativeapi_vizqlserver_2-0_2019_04_29_00_00_00.txt",
                        Height = 1500,
                        IsDashboard = "True",
                        LineNumber = 2501,
                        MarkCount = 391,
                        MarkLabelCount = 236,
                        NodeCount = 122,
                        NumViews = 9,
                        NumZones = 15,
                        PaneCount = 15,
                        Process = 0,
                        ProcessId = 14932,
                        ReflineCount = 0,
                        RepositoryURL = "DailyDashboard/Transactions",
                        RequestId = "XMbHKYhYMB5xZxuoriES9QAAA@I",
                        SessionId = "-",
                        SessionIdInMessage = "6D4D7873390542ACA4A0D6B1A7B12C83-2:0",
                        SessionState = "private",
                        SheetName =  "Transactions",
                        SiteName = "Enterprise Business Intelligence",
                        TextMarkCount = 242,
                        ThreadId = "44a8",
                        Timestamp = new DateTime(2019, 04, 29, 05, 43, 12, 214),
                        TooltipCount = 1341,
                        TransparentLinemarkCount = 0,
                        UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64; Trident/7.0; rv:11.0) like Gecko",
                        UserName = "LASHS",
                        VertexCount = 0,
                        Width = 1366,
                        WorkbookName = "Daily Dashboard",
                        Worker = "worker2",
                    },
                    new [] {
                        new {
                            MetricName = "DOMParser_ParseXmlInputSource",
                            RequestId ="XMbHKYhYMB5xZxuoriES9QAAA@I",
                            SessionId = "6D4D7873390542ACA4A0D6B1A7B12C83-2:0",
                            Count = 339,
                            MaxSeconds = 0.001,
                            MinSeconds = 0.0,
                            TotalTimeSeconds = 0.003,
                        },
                        new {
                            MetricName = "DOMParser_ParseXmlString",
                            RequestId ="XMbHKYhYMB5xZxuoriES9QAAA@I",
                            SessionId = "6D4D7873390542ACA4A0D6B1A7B12C83-2:0",
                            Count = 92,
                            MaxSeconds = 0.001,
                            MinSeconds = 0.0,
                            TotalTimeSeconds = 0.003,
                        },
                        new {
                            MetricName = "DataConnection_connect",
                            RequestId ="XMbHKYhYMB5xZxuoriES9QAAA@I",
                            SessionId = "6D4D7873390542ACA4A0D6B1A7B12C83-2:0",
                            Count = 6,
                            MaxSeconds = 0.103,
                            MinSeconds = 0.0,
                            TotalTimeSeconds = 0.167,
                        },
                        new {
                            MetricName = "ExecutingCommand_PerformingIt",
                            RequestId ="XMbHKYhYMB5xZxuoriES9QAAA@I",
                            SessionId = "6D4D7873390542ACA4A0D6B1A7B12C83-2:0",
                            Count = 1,
                            MaxSeconds = 0.381,
                            MinSeconds = 0.381,
                            TotalTimeSeconds = 0.381,
                        },
                        new {
                            MetricName = "ExecutingCommand_SerializingResult",
                            RequestId ="XMbHKYhYMB5xZxuoriES9QAAA@I",
                            SessionId = "6D4D7873390542ACA4A0D6B1A7B12C83-2:0",
                            Count = 1,
                            MaxSeconds = 0.003,
                            MinSeconds = 0.003,
                            TotalTimeSeconds = 0.003,
                        },
                        new {
                            MetricName = "FederationEngine_AcquireLocks",
                            RequestId ="XMbHKYhYMB5xZxuoriES9QAAA@I",
                            SessionId = "6D4D7873390542ACA4A0D6B1A7B12C83-2:0",
                            Count = 17,
                            // This is slightly different from the source data due to floating point precision issues
                            MaxSeconds = 4.343,
                            MinSeconds = 0.0,
                            TotalTimeSeconds = 12.671,
                        },
                    }
                ), 
            },
        };
    }
}
