using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using LogShark.LogParser;
using LogShark.LogParser.Containers;
using LogShark.LogParser.LogReaders;
using Xunit;

namespace LogShark.Tests.LogParser
{
    [Collection("ILogReader File-based Tests")]
    public class SimpleLinePerLineReaderTests : InvariantCultureTestsBase
    {
        [Fact]
        public void ReadEmptyTestFile()
        {
            using (var stream = TestLogFiles.OpenEmptyTestFile())
            {
                var results = new SimpleLinePerLineReader(stream, null, null).ReadLines().ToList();
                results.Should().Equal(new List<ReadLogLineResult>());
            }
        }
        
        [Fact]
        public void ReadTestFileWithPlainLines()
        {
            using (var stream = TestLogFiles.OpenTestFileWithPlainLines())
            {
                var results = new SimpleLinePerLineReader(stream, null, null).ReadLines().ToList();
                results.Should().BeEquivalentTo(ExpectedResults);
            }
        }
        
        [Fact] // This test helps to ensure that reader doesn't keep any state and can be reused safely for multiple files
        public void ReadTestFileWithPlainLinesTwice()
        {
            ReadTestFileWithPlainLines();
            ReadTestFileWithPlainLines();
        }
        
        private static readonly IList<ReadLogLineResult> ExpectedResults = new List<ReadLogLineResult>
        {
            new ReadLogLineResult(1, "2018-08-08 14:23:30.574 "),
            new ReadLogLineResult(2, "line 2"),
            new ReadLogLineResult(3, "line 3"),
            new ReadLogLineResult(4, "2018-08-08 14:23:31.765 "),
            new ReadLogLineResult(5, "line 5"),
            new ReadLogLineResult(6, "line 6"),
        };
    }
}