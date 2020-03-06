using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using FluentAssertions;
using LogShark.Containers;
using LogShark.LogParser;
using LogShark.LogParser.Containers;
using LogShark.LogParser.LogReaders;
using LogShark.Plugins;
using LogShark.Tests.Plugins.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LogShark.Tests.LogParser
{
    public class TableauLogsReaderTests : InvariantCultureTestsBase
    {
        private const string UnZippedTestSet = "TestData/TableauLogsReaderTest";
        private const string ZippedTestSet = "TestData/TableauLogsReaderTest.zip";
        
        private readonly ILogger _logger = new NullLoggerFactory().CreateLogger<TableauLogsReader>();
        
        private static readonly LogTypeInfo TestLogTypeInfo = new LogTypeInfo(LogType.Apache, (stream, _) => new SimpleLinePerLineReader(stream, null, null), new List<Regex>
        {
            new Regex(@"correctLog\d\.log", RegexOptions.Compiled),
            new Regex(@"correctMultiLineLog\.log", RegexOptions.Compiled)
        });
        
        [Fact]
        public void ProcessFolder()
        {
            var reader = new TableauLogsReader(_logger);
            var testPlugin1 = new TestPlugin();
            var testPlugin2 = new TestPlugin();
            
            var logSets = new List<LogSetInfo>
            {
                new LogSetInfo(UnZippedTestSet, string.Empty, false, UnZippedTestSet)
            };
            
            var results = reader.ProcessLogs(logSets, TestLogTypeInfo, new List<IPlugin> { testPlugin1, testPlugin2 });
           results.FilesProcessed.Should().Be(4);
           results.LinesProcessed.Should().Be(6);
           testPlugin1.ReceivedLines.Should().BeEquivalentTo(testPlugin2.ReceivedLines);           
           testPlugin1.ReceivedLines.Should().BeEquivalentTo(_expectedLinesForUnzipped);
        }
        
        [Fact]
        public void ProcessZip()
        {
            var reader = new TableauLogsReader(_logger);
            var testPlugin1 = new TestPlugin();
            var testPlugin2 = new TestPlugin();
            
            var logSets = new List<LogSetInfo>
            {
                new LogSetInfo(ZippedTestSet, string.Empty, true, ZippedTestSet)
            };
            
            var results = reader.ProcessLogs(logSets, TestLogTypeInfo, new List<IPlugin> { testPlugin1, testPlugin2 });
            results.FilesProcessed.Should().Be(4);
            results.LinesProcessed.Should().Be(6);
            testPlugin1.ReceivedLines.Should().BeEquivalentTo(testPlugin2.ReceivedLines);           
            testPlugin1.ReceivedLines.Should().BeEquivalentTo(_expectedLinesForZipped, options =>
            {
                options.Using<DateTime>(ctx => ctx.Subject.Should().BeAfter(new DateTime(2019, 3, 18), "Test files were created on those dates")).WhenTypeIs<DateTime>();
                return options;
            });
        }
        
        [Fact]
        public void ProcessBoth()
        {
            var reader = new TableauLogsReader(_logger);
            var testPlugin1 = new TestPlugin();
            var testPlugin2 = new TestPlugin();
            
            var logSets = new List<LogSetInfo>
            {
                new LogSetInfo(UnZippedTestSet, string.Empty, false, UnZippedTestSet),
                new LogSetInfo(ZippedTestSet, string.Empty, true, ZippedTestSet)
            };
            
            var results = reader.ProcessLogs(logSets, TestLogTypeInfo, new List<IPlugin> { testPlugin1, testPlugin2 });

            var expectedResults = _expectedLinesForZipped;
            expectedResults.UnionWith(_expectedLinesForUnzipped);
            results.FilesProcessed.Should().Be(8);
            results.LinesProcessed.Should().Be(12);
            testPlugin1.ReceivedLines.Should().BeEquivalentTo(testPlugin2.ReceivedLines);           
            testPlugin1.ReceivedLines.Should().BeEquivalentTo(expectedResults, options =>
            {
                options.Using<DateTime>(ctx => ctx.Subject.Should().BeAfter(new DateTime(2019, 3, 18), "Test files were created on those dates")).WhenTypeIs<DateTime>();
                return options;
            });
        }

        private static LogLine GetLogLine(string fileName, string pathToFile, int lineNumber, string @string)
        {
            var filePath = pathToFile == null
                ? fileName
                : $"{pathToFile}/{fileName}";
            var lastModifiedUtc = GetLastModifiedUtc(pathToFile, fileName);
            var logLineWithLineNumber = new ReadLogLineResult(lineNumber, @string);
            var logFileInfo = new LogFileInfo(fileName, filePath, "worker0", lastModifiedUtc);
            return new LogLine(logLineWithLineNumber, logFileInfo);
        }

        private static DateTime GetLastModifiedUtc(string pathToFile, string fileName)
        {
            var fullPath = Path.Combine(UnZippedTestSet, pathToFile ?? string.Empty, fileName);
            var fileInfo = new FileInfo(fullPath);
            return new DateTimeOffset(fileInfo.LastWriteTime.Ticks, TimeSpan.Zero).UtcDateTime;
        }
        
        private readonly HashSet<LogLine> _expectedLinesForUnzipped = new HashSet<LogLine>
        {
            GetLogLine("correctLog1.log", null, 1, "Expected line 1"),
            GetLogLine("correctLog2.log", null, 1, "Expected line 2"),
            GetLogLine("correctLog3.log", "folder", 1, "Expected line 3"),
            GetLogLine("correctMultiLineLog.log", null, 1, "Expected multiline log line 1"),
            GetLogLine("correctMultiLineLog.log", null, 2, "Expected multiline log line 2"),
            GetLogLine("correctMultiLineLog.log", null, 3, "Expected multiline log line 3"),
        };
        
        private readonly HashSet<LogLine> _expectedLinesForZipped = new HashSet<LogLine>
        {
            GetLogLine("correctLog1.log", null, 1, "Expected line 1"),
            GetLogLine("correctLog2.log", null, 1, "Expected line 2"),
            GetLogLine("correctLog3.log", "folder", 1, "Expected line 3"),
            GetLogLine("correctMultiLineLog.log", null, 1, "Expected multiline log line 1"),
            GetLogLine("correctMultiLineLog.log", null, 2, "Expected multiline log line 2"),
            GetLogLine("correctMultiLineLog.log", null, 3, "Expected multiline log line 3"),
        };
    }
}