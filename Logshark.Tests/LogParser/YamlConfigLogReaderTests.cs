using System.Collections.Generic;
using System.Linq;
using System.Threading;
using FluentAssertions;
using LogShark.LogParser;
using LogShark.LogParser.Containers;
using LogShark.LogParser.LogReaders;
using Xunit;

namespace LogShark.Tests.LogParser
{
    [Collection("ILogReader File-based Tests")]
    public class YamlConfigLogReaderTests : InvariantCultureTestsBase
    {
        [Fact]
        public void ReadEmptyTestFile()
        {
            var expectedResult = new List<ReadLogLineResult> {new ReadLogLineResult(0, null)};
            using (var stream = TestLogFiles.OpenEmptyTestFile())
            {
                var results = new YamlConfigLogReader(stream, null, null).ReadLines().ToList();
                results.Should().BeEquivalentTo(expectedResult);
            }
        }
        
        [Fact]
        public void ReadTestFileWithPlainLines()
        {
            var expectedResult = new List<ReadLogLineResult> {new ReadLogLineResult(0, null)};
            using (var stream = TestLogFiles.OpenTestFileWithPlainLines())
            {
                var results = new YamlConfigLogReader(stream, null, null).ReadLines().ToList();
                results.Should().BeEquivalentTo(expectedResult);
            }
        }
        
        [Fact]
        public void ReadTestFileWithYamlData()
        {
            using (var stream = TestLogFiles.OpenTestFileWithYamlData())
            {
                var results = new YamlConfigLogReader(stream, null, null).ReadLines().ToList();
                results.Should().BeEquivalentTo(ExpectedResults);
            }
        }
        
        [Fact] // This test helps to ensure that reader doesn't keep any state and can be reused safely for multiple files
        public void ReadTestFileWithYamlDataTwice()
        {
            ReadTestFileWithYamlData();
            ReadTestFileWithYamlData();
        }
        
        private static readonly IList<ReadLogLineResult> ExpectedResults = new List<ReadLogLineResult>
        {
            new ReadLogLineResult(0, new Dictionary<string, string>
            {
                {"param1", "value1"},
                {"emptyParam", null},
                {"multiLineParam", "line1 'quotedThing' line2"},
                {"param4", "value4"},
            })
        };
    }
}