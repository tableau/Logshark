using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using LogShark.LogParser;
using LogShark.LogParser.Containers;
using LogShark.LogParser.LogReaders;
using LogShark.Plugins.Shared;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json.Linq;
using Xunit;

namespace LogShark.Tests.LogParser
{
    [Collection("ILogReader File-based Tests")]
    public class NativeJsonLogsReaderTests : InvariantCultureTestsBase
    {
        [Fact]
        public void ReadEmptyTestFile()
        {
            using (var stream = TestLogFiles.OpenEmptyTestFile())
            {
                var reader = new NativeJsonLogsReader(stream, null, null);
                var results = reader.ReadLines().ToList();
                results.Should().Equal(new List<ReadLogLineResult>());
            }
        }
        
        [Fact]
        public void ReadTestFileWithPlainLines()
        {
            var expectedResults = new List<ReadLogLineResult>();
            for (var i = 1; i <= 6; ++i)
            {
                expectedResults.Add(new ReadLogLineResult(i, null));
            }
            
            var processingNotificationsCollector = new ProcessingNotificationsCollector(10);
            using (var stream = TestLogFiles.OpenTestFileWithPlainLines())
            {
                var reader = new NativeJsonLogsReader(stream, "testFile.txt", processingNotificationsCollector);
                var results = reader.ReadLines().ToList();
                results.Should().BeEquivalentTo(expectedResults);
            }

            processingNotificationsCollector.TotalErrorsReported.Should().Be(6);
        }

        [Fact]
        public void VerifyDifferentJsonStrings()
        {
            var processingNotificationsCollector = new ProcessingNotificationsCollector(10);
            using (var stream = TestLogFiles.OpenTestFileWithJsonData())
            {
                var reader = new NativeJsonLogsReader(stream, "testFile.txt", processingNotificationsCollector);
                var results = reader.ReadLines().ToList();
                results.Should().BeEquivalentTo(ExpectedResults);

                // This extra steps are needed to confirm JToken equality (BeEquivalentTo above cannot do it) 
                var actualPayload = ExtractPayloadAsStrings(results); 
                var expectedPayload = ExtractPayloadAsStrings(ExpectedResults);
                actualPayload.Should().BeEquivalentTo(expectedPayload);

                var actualArtData = ExtractArtDataAsStrings(results);
                var expectedArtData = ExtractArtDataAsStrings(ExpectedResults);
                actualArtData.Should().BeEquivalentTo(expectedArtData);
            }

            processingNotificationsCollector.TotalErrorsReported.Should().Be(3);
        }

        private static readonly List<ReadLogLineResult> ExpectedResults = new List<ReadLogLineResult>
        {
            new ReadLogLineResult(1, new NativeJsonLogsBaseEvent
            {
                Timestamp = new DateTime(2018, 8, 8, 14, 22, 51, 844),
                ProcessId = 22492,
                ThreadId = "7efd10c6f1c0",
                Severity = "info",
                RequestId = "-",
                SessionId = "-",
                EventType = "service-start",
                EventPayload = JToken.Parse("{\"msg\":\"Running as a service\"}"),
            }),
            new ReadLogLineResult(2, new NativeJsonLogsBaseEvent
            {
                Timestamp = new DateTime(2018, 8, 8, 14, 22, 51, 844),
                ProcessId = 22492,
                ThreadId = "7efd10c6f1c0",
                Severity = "info",
                RequestId = "-",
                SessionId = "-",
                EventType = "service-start",
                EventPayload = JToken.FromObject("Running as a service"),
            }),
            new ReadLogLineResult(3, null), // Null value turned into empty string by JsonConvert 
            new ReadLogLineResult(4, null), // Not a JSON line
            new ReadLogLineResult(5, new NativeJsonLogsBaseEvent
            {
                ArtData = JToken.Parse("{\"depth\":3,\"elapsed\":0.003,\"id\":\"P////+v7k0GP/////RtykS\",\"name\":\"create-protocol\",\"res\":{\"alloc\":{\"e\":2.72e+04,\"i\":2.72e+04,\"ne\":290,\"ni\":290},\"free\":{\"e\":1.72e+04,\"i\":1.72e+04,\"ne\":180,\"ni\":180},\"kcpu\":{\"e\":0,\"i\":0},\"ntid\":2,\"ucpu\":{\"e\":1,\"i\":1}},\"rk\":\"ok\",\"rv\":{},\"sponsor\":\"PkvJ6EX4EatJyj2iUSNusL\",\"type\":\"end\",\"vw\":\"Summary Information\",\"wb\":\"Test workbook\"}"),
                Timestamp = new DateTime(2018, 7, 11, 11, 02, 22, 621),
                ProcessId = 18376,
                ThreadId = "1024",
                Severity = "info",
                Site = "Default",
                RequestId = "W0ZGLuNWFihxZCr6rTgQvQAAA84",
                SessionId = "F71D8A10BE67431F85AC66F6E6932497-0:0",
                Username = "Tableau",
                EventType = "end-create-protocol",
                EventPayload = JToken.Parse("{\"class\":\"hyper\",\"elapsed\":0.003,\"protocol-class\":\"hyper\"}"),
            }),
            new ReadLogLineResult(6, null), // Corrupt JSON line
        };

        private static IEnumerable<string> ExtractPayloadAsStrings(IEnumerable<ReadLogLineResult> results)
        {
            return results
                .Select(line => line.LineContent as NativeJsonLogsBaseEvent)
                .Select(@event => @event?.EventPayload?.ToString());
        }
        
        private static IEnumerable<string> ExtractArtDataAsStrings(IEnumerable<ReadLogLineResult> results)
                {
                    return results
                        .Select(line => line.LineContent as NativeJsonLogsBaseEvent)
                        .Select(@event => @event?.ArtData?.ToString());
                }
    }
}