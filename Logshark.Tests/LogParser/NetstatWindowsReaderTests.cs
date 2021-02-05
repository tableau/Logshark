using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using LogShark.Shared.LogReading.Containers;
using LogShark.Shared.LogReading.Readers;
using Xunit;

namespace LogShark.Tests.LogParser
{
    [Collection("ILogReader File-based Tests")]
    public class NetstatWindowsReaderTests : InvariantCultureTestsBase
    {
        [Fact]
        public void ReadTestFileWithNetstatData()
        {
            var firstGroup = new Stack<(string line, int lineNumber)>(new[] {
                   ("  TCP    0.0.0.0:80             0.0.0.0:0              LISTENING", 4),
                   (" [httpd.exe]", 5)});

            var secondGroup = new Stack<(string line, int lineNumber)>(new[] {
                   ("  TCP    0.0.0.0:135            0.0.0.0:0              LISTENING", 6),
                   ("  RpcSs", 7),
                   (" [svchost.exe]", 8)});

            var expected = new List<ReadLogLineResult> {
                new ReadLogLineResult(4, firstGroup),
                new ReadLogLineResult(6, secondGroup),
            };

            using (var stream = TestLogFiles.OpenTestFileWithWindowsNetstatData())
            {
                var results = new NetstatWindowsReader(stream, "netstat.txt", null).ReadLines().ToList();
                results.Should().BeEquivalentTo(expected);
            }
        }

        [Fact]
        public void ReadTestFileWithLocalizedNetstatData()
        {
            var firstGroup = new Stack<(string line, int lineNumber)>(new[] {
                   ("  TCP         0.0.0.0:135            0.0.0.0:0              LISTENING", 9),
                   ("  RpcSs", 11),
                   (" [svchost.exe]", 13)});

            var secondGroup = new Stack<(string line, int lineNumber)>(new[] {
                   ("  TCP         0.0.0.0:445            0.0.0.0:0              LISTENING", 15),
                   (" ���L�ҏ����擾�ł��܂���", 17)});

            var thirdGroup = new Stack<(string line, int lineNumber)>(new[] {
                   ("  TCP         0.0.0.0:3389           0.0.0.0:0              LISTENING", 19),
                   ("  TermService", 21),
                   (" [svchost.exe]", 23)});
        
            var expected = new List<ReadLogLineResult> {
                new ReadLogLineResult(8, firstGroup), // since there's whitespace the sections start before the first line with data
                new ReadLogLineResult(14, secondGroup), 
                new ReadLogLineResult(18, thirdGroup),
            };

            var processingNotificationsCollector = new ProcessingNotificationsCollector(10);
            Action testAction = () =>
            {
                using (var stream = TestLogFiles.OpenTestFileWithLocalizedWindowsNetstatData())
                {
                    var results = new NetstatWindowsReader(stream, "netstat.txt", processingNotificationsCollector).ReadLines().ToList();
                    results.Should().BeEquivalentTo(expected);
                }
            };

            testAction.Should().NotThrow();
            processingNotificationsCollector.TotalErrorsReported.Should().Be(0);
        }
    }
}
