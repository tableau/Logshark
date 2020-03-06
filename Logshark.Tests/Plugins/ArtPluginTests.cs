using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using LogShark.Containers;
using LogShark.LogParser.Containers;
using LogShark.Plugins.Art;
using LogShark.Plugins.Art.Model;
using LogShark.Plugins.Shared;
using LogShark.Tests.Plugins.Helpers;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json.Linq;
using Xunit;

namespace LogShark.Tests.Plugins
{
    public class ArtPluginTests : InvariantCultureTestsBase
    {
        private  static readonly LogFileInfo TestLogFileInfo = new LogFileInfo("test.log", @"folder1/test.log", "node1", DateTime.MinValue);
        
        [Fact]
        public void BadOrNoOpInput()
        {
            var processingNotificationsCollector = new ProcessingNotificationsCollector(10);
            var testWriterFactory = new TestWriterFactory();
            using (var plugin = new ArtPlugin())
            {
                plugin.Configure(testWriterFactory, null, processingNotificationsCollector, new NullLoggerFactory());
                
                var wrongContentFormat = new LogLine(new ReadLogLineResult(123, "Art doesn't expect string"), TestLogFileInfo);
                var nullContent = new LogLine(new ReadLogLineResult(123, null), TestLogFileInfo);
                var payloadIsNull = new LogLine(new ReadLogLineResult(123, new NativeJsonLogsBaseEvent { EventType = "msg", ArtData = null}), TestLogFileInfo);
                var incorrectArtPayload = new LogLine(new ReadLogLineResult(123, new NativeJsonLogsBaseEvent { EventType = "msg", ArtData = JToken.FromObject("JSON was expected here")}), TestLogFileInfo);
                
                plugin.ProcessLogLine(wrongContentFormat, LogType.VizqlserverCpp);
                plugin.ProcessLogLine(nullContent, LogType.VizqlserverCpp);
                plugin.ProcessLogLine(payloadIsNull, LogType.VizqlserverCpp);
                plugin.ProcessLogLine(incorrectArtPayload, LogType.VizqlserverCpp);
            }

            testWriterFactory.AssertAllWritersAreDisposedAndEmpty(1);
            processingNotificationsCollector.TotalErrorsReported.Should().Be(3);
        }

        [Fact]
        public void SpecialCase_CompletelyIncompatibleJsonInArtData()
        {
            var testWriterFactory = new TestWriterFactory();
            using (var plugin = new ArtPlugin())
            {
                plugin.Configure(testWriterFactory, null, null, new NullLoggerFactory());
                
                var incorrectJsonInArtPayload = new LogLine(new ReadLogLineResult(123, new NativeJsonLogsBaseEvent { EventType = "msg", ArtData = JToken.FromObject(new {SomeOtherKey = "test"})}), TestLogFileInfo);
                plugin.ProcessLogLine(incorrectJsonInArtPayload, LogType.VizqlserverCpp);
            }

            var writer = (TestWriter<FlattenedArtEvent>) testWriterFactory.Writers.First().Value;
            writer.ReceivedObjects.Count.Should().Be(1);
            writer.ReceivedObjects.First().Should().NotBeNull(); // All props are null, but object still exists
        }
        
        [Fact]
        public void RunTestCases()
        {
            var testWriterFactory = new TestWriterFactory();
            using (var plugin = new ArtPlugin())
            {
                plugin.Configure(testWriterFactory, null, null, new NullLoggerFactory());

                foreach (var testCase in _testCases)
                {
                    var logLine = testCase.GetLogLine();
                    plugin.ProcessLogLine(logLine, LogType.Filestore);
                }
            }

            var expectedOutput = _testCases.Select(testCase => testCase.ExpectedOutput).ToList();
            testWriterFactory.Writers.Count.Should().Be(1);
            
            var testWriter = testWriterFactory.Writers.Values.First() as TestWriter<FlattenedArtEvent>;
            testWriter.WasDisposed.Should().Be(true);
            testWriter.ReceivedObjects.Should().BeEquivalentTo(expectedOutput);
        }

        private readonly IList<PluginTestCase> _testCases = new List<PluginTestCase>
        {
            // 2018.1 begin event
            new PluginTestCase
            {
                LogContents = new NativeJsonLogsBaseEvent
                {
                    ArtData = JToken.Parse("{\"depth\":1,\"id\":\"P////+nY07NJZiWc1iU55O\",\"name\":\"create-session\",\"root\":\"P/////BcE9zP/////L8mgU\",\"sponsor\":\"P/////BcE9zP/////L8mgU\",\"type\":\"begin\",\"vw\":\"\",\"wb\":\"\"}"),
                    EventType = "begin-create-session",
                    EventPayload = null,
                    ProcessId = 111,
                    Timestamp = new DateTime(2019, 4, 10, 10, 31, 00, 321),
                },
                LogFileInfo = TestLogFileInfo,
                LineNumber = 123,
                ExpectedOutput = new
                {
                    // Base Event
                    FileName = TestLogFileInfo.FileName,
                    FilePath = TestLogFileInfo.FilePath,
                    LineNumber = 123,
                    Timestamp = new DateTime(2019, 4, 10, 10, 31, 00, 321),
                    Worker = TestLogFileInfo.Worker,
                    // Art
                    ArtDepth = 1,
                    ArtId = "P////+nY07NJZiWc1iU55O",
                    ArtName = "create-session",
                    ArtRootId = "P/////BcE9zP/////L8mgU",
                    ArtSponsorId = "P/////BcE9zP/////L8mgU",
                    ArtType = "begin",
                    ArtView = "",
                    ArtWorkbook = ""
                }},
            
            // 2018.1 end event
            new PluginTestCase
            {
                LogContents = new NativeJsonLogsBaseEvent
                {
                    ArtData = JToken.Parse("{\"depth\":1,\"elapsed\":0.002,\"id\":\"P////+nY07NJZiWc1iU55O\",\"name\":\"create-session\",\"res\":{\"alloc\":{\"e\":1.30e+05,\"i\":1.30e+05,\"ne\":1406,\"ni\":1406},\"free\":{\"e\":3.70e+04,\"i\":3.70e+04,\"ne\":450,\"ni\":450},\"kcpu\":{\"e\":0,\"i\":0},\"ntid\":1,\"ucpu\":{\"e\":3,\"i\":3}},\"rk\":\"ok\",\"rv\":{},\"sponsor\":\"P/////BcE9zP/////L8mgU\",\"type\":\"end\",\"vw\":\"\",\"wb\":\"\"}"),
                    EventType = "create-session",
                    EventPayload = null,
                    ProcessId = 111,
                    Timestamp = new DateTime(2019, 4, 10, 10, 31, 00, 321),
                },
                LogFileInfo = TestLogFileInfo,
                LineNumber = 124,
                ExpectedOutput = new
                {
                    // Base Event
                    FileName = TestLogFileInfo.FileName,
                    FilePath = TestLogFileInfo.FilePath,
                    LineNumber = 124,
                    Timestamp = new DateTime(2019, 4, 10, 10, 31, 00, 321),
                    Worker = TestLogFileInfo.Worker,
                    // Art
                    ArtDepth = 1,
                    ArtElapsedSeconds = 0.002,
                    ArtId = "P////+nY07NJZiWc1iU55O",
                    ArtName = "create-session",
                    ArtResultKey = "ok",
                    ArtResultValue = "{}",
                    ArtSponsorId = "P/////BcE9zP/////L8mgU",
                    ArtType = "end",
                    // Art Resource Consumption Metrics
                    ArtAllocatedBytesThisActivity = 1.30e+05,
                    ArtAllocatedBytesThisActivityPlusSponsored = 1.30e+05,
                    ArtNumberOfAllocationsThisActivity = 1406,
                    ArtNumberOfAllocationsThisActivityPlusSponsored = 1406,
                    ArtReleasedBytesThisActivity = 3.70e+04,
                    ArtReleasedBytesThisActivityPlusSponsored = 3.70e+04,
                    ArtNumberOfReleasesThisActivity = 450,
                    ArtNumberOfReleasesThisActivityPlusSponsored = 450,
                    ArtKernelCpuTimeThisActivityMilliseconds = 0,
                    ArtKernelCpuTimeThisActivityPlusSponsoredMilliseconds = 0,
                    ArtNumberOfThreadsActivityRanOn = 1,
                    ArtUserCpuTimeThisActivityMilliseconds = 3,
                    ArtUserCpuTimeThisActivityPlusSponsoredMilliseconds = 3
                }},
            
            // Artificial test - Old format for View, Workbook, Result Key, Result Value. Also Details field should be taken from payload.
            new PluginTestCase
            {
                LogContents = new NativeJsonLogsBaseEvent
                {
                    ArtData = JToken.Parse("{\"view\":\"testView\",\"workbook\":\"testWorkbook\",\"result-c\":\"testResultKey\",\"result-i\":\"testResultValue\"}"),
                    EventType = "create-session",
                    EventPayload = JToken.Parse("{\"testKey\":\"testValue\"}"),
                    ProcessId = 111,
                    Timestamp = new DateTime(2019, 4, 10, 10, 31, 00, 321),
                },
                LogFileInfo = TestLogFileInfo,
                LineNumber = 124,
                ExpectedOutput = new
                {
                    // Base Event
                    FileName = TestLogFileInfo.FileName,
                    FilePath = TestLogFileInfo.FilePath,
                    LineNumber = 124,
                    Timestamp = new DateTime(2019, 4, 10, 10, 31, 00, 321),
                    Worker = TestLogFileInfo.Worker,
                    // Art
                    ArtResultKey = "testResultKey",
                    ArtResultValue = "testResultValue",
                    ArtView = "testView",
                    ArtWorkbook = "testWorkbook",
                    Details = "{\"testKey\":\"testValue\"}"
                }},
            
            // Artificial test - View and Workbook in Context. String Details field
            new PluginTestCase
            {
                LogContents = new NativeJsonLogsBaseEvent
                {
                    ArtData = JToken.Parse("{\"id\":\"testId\",\"vw\":\"ignored\",\"wb\":\"ignored\"}"),
                    ContextMetrics = new ContextMetrics
                    {
                        View = "testView",
                        Workbook = "testWorkbook"
                    },
                    EventType = "create-session",
                    EventPayload = JToken.FromObject("testDetails"),
                    ProcessId = 111,
                    Timestamp = new DateTime(2019, 4, 10, 10, 31, 00, 321),
                },
                LogFileInfo = TestLogFileInfo,
                LineNumber = 124,
                ExpectedOutput = new
                {
                    // Base Event
                    FileName = TestLogFileInfo.FileName,
                    FilePath = TestLogFileInfo.FilePath,
                    LineNumber = 124,
                    Timestamp = new DateTime(2019, 4, 10, 10, 31, 00, 321),
                    Worker = TestLogFileInfo.Worker,
                    // Art
                    ArtId = "testId",
                    Details = "\"testDetails\"",
                    ArtView = "testView",
                    ArtWorkbook = "testWorkbook",
                }},
            
            // Long values in counts
            new PluginTestCase
            {
                LogContents = new NativeJsonLogsBaseEvent
                {
                    ArtData = JToken.Parse("{\"depth\":1,\"elapsed\":0.002,\"id\":\"P////+nY07NJZiWc1iU55O\",\"name\":\"create-session\",\"res\":{\"alloc\":{\"e\":1.30e+05,\"i\":1.30e+05,\"ne\":3147483647,\"ni\":4147483647},\"free\":{\"e\":3.70e+04,\"i\":3.70e+04,\"ne\":5147483647,\"ni\":6147483647},\"kcpu\":{\"e\":7147483647,\"i\":8147483647},\"ntid\":1,\"ucpu\":{\"e\":9147483647,\"i\":10147483647}},\"rk\":\"ok\",\"rv\":{},\"sponsor\":\"P/////BcE9zP/////L8mgU\",\"type\":\"end\",\"vw\":\"\",\"wb\":\"\"}"),
                    EventType = "create-session",
                    EventPayload = null,
                    ProcessId = 111,
                    Timestamp = new DateTime(2019, 4, 10, 10, 31, 00, 321),
                },
                LogFileInfo = TestLogFileInfo,
                LineNumber = 124,
                ExpectedOutput = new
                {
                    // Base Event
                    FileName = TestLogFileInfo.FileName,
                    FilePath = TestLogFileInfo.FilePath,
                    LineNumber = 124,
                    Timestamp = new DateTime(2019, 4, 10, 10, 31, 00, 321),
                    Worker = TestLogFileInfo.Worker,
                    // Art
                    ArtDepth = 1,
                    ArtElapsedSeconds = 0.002,
                    ArtId = "P////+nY07NJZiWc1iU55O",
                    ArtName = "create-session",
                    ArtResultKey = "ok",
                    ArtResultValue = "{}",
                    ArtSponsorId = "P/////BcE9zP/////L8mgU",
                    ArtType = "end",
                    // Art Resource Consumption Metrics
                    ArtAllocatedBytesThisActivity = 1.30e+05,
                    ArtAllocatedBytesThisActivityPlusSponsored = 1.30e+05,
                    ArtNumberOfAllocationsThisActivity = 3147483647,
                    ArtNumberOfAllocationsThisActivityPlusSponsored = 4147483647,
                    ArtReleasedBytesThisActivity = 3.70e+04,
                    ArtReleasedBytesThisActivityPlusSponsored = 3.70e+04,
                    ArtNumberOfReleasesThisActivity = 5147483647,
                    ArtNumberOfReleasesThisActivityPlusSponsored = 6147483647,
                    ArtKernelCpuTimeThisActivityMilliseconds = 7147483647,
                    ArtKernelCpuTimeThisActivityPlusSponsoredMilliseconds = 8147483647,
                    ArtNumberOfThreadsActivityRanOn = 1,
                    ArtUserCpuTimeThisActivityMilliseconds = 9147483647,
                    ArtUserCpuTimeThisActivityPlusSponsoredMilliseconds = 10147483647
                }},
        };

    }
}