using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using LogShark.Shared.LogReading.Containers;
using LogShark.Shared.LogReading.Readers;
using Xunit;

namespace LogShark.Tests.LogParser
{
    [Collection("ILogReader File-based Tests")]
    public class MultilineJavaLogReaderTests : InvariantCultureTestsBase
    {
        [Fact]
        public void ReadEmptyTestFile()
        {
            using (var stream = TestLogFiles.OpenEmptyTestFile())
            {
                var results = new MultilineJavaLogReader(stream).ReadLines().ToList();
                results.Should().Equal(new List<ReadLogLineResult>());
            }
        }
        
        [Fact]
        public void ReadTestFileWithPlainLines()
        {
            using (var stream = TestLogFiles.OpenTestFileWithPlainLines())
            {
                var results = new MultilineJavaLogReader(stream).ReadLines().ToList();
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
            new ReadLogLineResult(1, "2018-08-08 14:23:30.574 \nline 2\nline 3"),
            new ReadLogLineResult(4, "2018-08-08 14:23:31.765 \nline 5\nline 6"),
        };
    }
}