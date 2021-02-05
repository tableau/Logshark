using System;
using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using LogShark.Shared.LogReading;
using LogShark.Shared.LogReading.Containers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LogShark.Tests.LogParser
{
    [Collection("Tests accessing files in TestData dir")]
    public class TableauLogsExtractorTests : InvariantCultureTestsBase, IDisposable
    {
        private const string EmptyZip = "TestData/Empty.zip";
        public const string InvalidZip = "TestData/NotAZip.zip";
        private const string NonExistingTestSet = "IDoNotExist.zip";
        private const string UnzippedTestSet = "TestData/TableauLogsExtractorTest";
        public const string ZippedTestSet = "TestData/TableauLogsExtractorTest.zip";
        public const string ZipWithEmptyLog = "TestData/ZipWithEmptyLog.zip";
        
        private const string TempDir = "TestTemp";

        private readonly ILogger _logger = new NullLoggerFactory().CreateLogger<TableauLogsExtractor>();
        private readonly ProcessingNotificationsCollector _processingNotificationsCollector;

        public TableauLogsExtractorTests()
        {
            DeleteTempDir();
            _processingNotificationsCollector = new ProcessingNotificationsCollector(10);
        }

        [Fact]
        public void EvaluateUnzipped()
        {
            Directory.Exists(TempDir).Should().Be(false);

            using (var extractor = new TableauLogsExtractor(UnzippedTestSet, TempDir, _processingNotificationsCollector, _logger))
            {
                var expectedParts = new HashSet<LogSetInfo>
                {
                    new LogSetInfo(UnzippedTestSet, string.Empty, false, UnzippedTestSet),
                    new LogSetInfo($"{UnzippedTestSet}/worker1.zip", "worker1", true, UnzippedTestSet),
                    new LogSetInfo($"{UnzippedTestSet}/localhost/tabadminagent_0.20181.18.0404.16052600117725665315795.zip", "localhost/tabadminagent_0.20181.18.0404.16052600117725665315795", true, UnzippedTestSet)
                };

                extractor.LogSetParts.Should().BeEquivalentTo(expectedParts);
                Directory.Exists(TempDir).Should().Be(false);
            }
            _processingNotificationsCollector.TotalErrorsReported.Should().Be(0);
        }

        [Fact]
        public void EvaluateZipped()
        {
            Directory.Exists(TempDir).Should().Be(false);
            
            using (var extractor = new TableauLogsExtractor(ZippedTestSet, TempDir, _processingNotificationsCollector, _logger))
            {
                var expectedParts = new HashSet<LogSetInfo>
                {
                    new LogSetInfo(ZippedTestSet, string.Empty, true, ZippedTestSet),
                    new LogSetInfo($"{TempDir}/NestedZipFiles/worker1.zip", "worker1", true, ZippedTestSet),
                    new LogSetInfo($"{TempDir}/NestedZipFiles/tabadminagent_0.20181.18.0404.16052600117725665315795.zip", "localhost/tabadminagent_0.20181.18.0404.16052600117725665315795", true, ZippedTestSet)
                };

                extractor.LogSetParts.Should().BeEquivalentTo(expectedParts);
                Directory.Exists(TempDir).Should().Be(true);
                
                File.Exists($"{TempDir}/NestedZipFiles/worker1.zip").Should().Be(true);
                File.Exists($"{TempDir}/NestedZipFiles/tabadminagent_0.20181.18.0404.16052600117725665315795.zip").Should().Be(true);
            }

            Directory.Exists($"{TempDir}/NestedZipFiles").Should().Be(false);
            Directory.Exists(TempDir).Should().Be(true);
            _processingNotificationsCollector.TotalErrorsReported.Should().Be(0);
        }

        [Fact]
        public void FileOrDirDoNotExist()
        {
            File.Exists(NonExistingTestSet).Should().Be(false);
            
            Action testAction = () => new TableauLogsExtractor(NonExistingTestSet, TempDir, _processingNotificationsCollector, _logger);

            testAction.Should().Throw<ArgumentException>();
            _processingNotificationsCollector.TotalErrorsReported.Should().Be(0);
        }

        [Theory]
        [InlineData(EmptyZip, false)]
        [InlineData(NonExistingTestSet, false)]
        [InlineData(InvalidZip, false)]
        [InlineData(UnzippedTestSet, true)]
        [InlineData(ZippedTestSet, true)]
        [InlineData(ZipWithEmptyLog, true)]
        public void FileCanBeOpenedTests(string testPath, bool expectSuccess)
        {
            var result = TableauLogsExtractor.FileCanBeOpened(testPath, new NullLogger<TableauLogsExtractor>());
            result.FileCanBeOpened.Should().Be(expectSuccess);
            string.IsNullOrWhiteSpace(result.ErrorMessage).Should().Be(expectSuccess);
        }
        
        [Theory]
        [InlineData(EmptyZip, false, false)]
        [InlineData(NonExistingTestSet, false, false)]
        [InlineData(InvalidZip, false, false)]
        [InlineData(UnzippedTestSet, false, false)]
        [InlineData(ZippedTestSet, true, false)]
        [InlineData(ZipWithEmptyLog, true, true)]
        public void FileIsAZipWithLogsTests(string testPath, bool validZip, bool containsLogs)
        {
            var result = TableauLogsExtractor.FileIsAZipWithLogs(testPath, new NullLogger<TableauLogsExtractor>());
            result.ValidZip.Should().Be(validZip);
            result.ContainsLogFiles.Should().Be(containsLogs);
            string.IsNullOrWhiteSpace(result.ErrorMessage).Should().Be(validZip);
        }
        
        public void Dispose()
        {
            DeleteTempDir();
        }

        private static void DeleteTempDir()
        {
            if (Directory.Exists(TempDir))
            {
                Directory.Delete(TempDir, true);
            }
        }
    }
}