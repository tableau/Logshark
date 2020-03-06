using CsvHelper.Configuration.Attributes;
using FluentAssertions;
using LogShark.LogParser;
using LogShark.LogParser.Containers;
using LogShark.LogParser.LogReaders;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace LogShark.Tests.LogParser
{
    [Collection("ILogReader File-based Tests")]
    public class CsvLogReaderTests : InvariantCultureTestsBase
    {
        [Fact]
        public void ReadyEmptyTestFile()
        {
            using (var stream = TestLogFiles.OpenEmptyTestFile())
            {
                var results = new CsvLogReader<TestEvent>(stream, null, null).ReadLines().ToList();
                results.Should().Equal(new List<TestEvent>());
            }
        }

        [Fact]
        public void ReadTestFileWithCsvData()
        {
            using (var stream = TestLogFiles.OpenTestFileWithCsvData())
            {
                var results = new CsvLogReader<TestEvent>(stream, null, null).ReadLines().ToList();
                results.Should().BeEquivalentTo(ExpectedResults);
            }
        }

        [Fact] // This test helps to ensure that reader doesn't keep any state and can be reused safely for multiple files
        public void VerifyRunningReaderTwice()
        {
            ReadTestFileWithCsvData();
            ReadTestFileWithCsvData();
        }

        private static readonly List<ReadLogLineResult> ExpectedResults = new List<ReadLogLineResult>()
        {
            new ReadLogLineResult(1, new TestEvent()
            {
                Date = DateTime.Parse("2018-08-08 14:23:31.574"),
                Id = 1,
                Message = @"Hello",
                Level = TestEventLevel.DEBUG
            }),
            new ReadLogLineResult(2, new TestEvent()
            {
                Date = DateTime.Parse("2018-08-08 14:23:32.574"),
                Id = 2,
                Message = @"World",
                Level = null
            }),
            new ReadLogLineResult(3, new TestEvent()
            {
                Date = DateTime.Parse("2018-08-08 14:23:33.574"),
                Id = 3,
                Message = @"Fizz Buzz",
                Level = TestEventLevel.INFO
            }),
            new ReadLogLineResult(4, new TestEvent()
            {
                Date = DateTime.Parse("2018-08-08 14:23:34.574"),
                Id = 4,
                Message = @"Fizz Buzz",
                Level = TestEventLevel.WARN
            }),
            new ReadLogLineResult(5, new TestEvent()
            {
                Date = DateTime.Parse("2018-08-08 14:23:35.574"),
                Id = 5,
                Message = @"This message, has a comma",
                Level = TestEventLevel.ERROR
            }),
        };

        private class TestEvent
        {
            [Index(0)]
            public DateTime Date { get; set; }

            [Index(1)]
            public int Id { get; set; }

            [Index(2)]
            public string Message { get; set; }

            [Index(3)]
            public TestEventLevel? Level { get; set; }
        }

        private enum TestEventLevel
        {
            DEBUG,
            INFO,
            WARN,
            ERROR
        }
    }
}
