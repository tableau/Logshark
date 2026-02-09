using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Castle.Core.Logging;
using FluentAssertions;
using LogShark.Containers;
using LogShark.Metrics;
using LogShark.Tests.Common;
using LogShark.Tests.Plugins.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Xunit;

namespace LogShark.Tests.E2E
{
    public class EndToEndTest
    {
        private readonly Microsoft.Extensions.Logging.ILoggerFactory _loggerFactory = new LoggerFactory(new ILoggerProvider[] { new TestLoggerProvider() }, new LoggerFilterOptions());

        private readonly LogSharkConfiguration _config;
        private readonly IDictionary<string, int> _expectedProcessingErrorsCount;
        private readonly string _expectedResultsLocation;

        public EndToEndTest(LogSharkCommandLineParameters clParameters, string expectedResultsLocation, IDictionary<string, int> expectedProcessingErrorsCount = null, IDictionary<string,string> configOverrides = null)
        {
            var configValues = new Dictionary<string, string>()
            {
                ["EnvironmentConfig:NumberOfErrorDetailsToKeep"] = "10",
                ["EnvironmentConfig:OutputDir"] = "Output",
                ["EnvironmentConfig:OutputDirMaxResults"] = "1",
                ["EnvironmentConfig:TempDir"] = "Temp",
                ["EnvironmentConfig:WorkbookTemplatesDir"] = "Workbooks",
                ["PluginsConfiguration:Apache:IncludeGatewayChecks"] = "true",
                ["PluginsConfiguration:VizqlDesktop:MaxQueryLength"] = "10000",
                ["PluginsConfiguration:DefaultPluginSet:PluginsToExcludeFromDefaultSet"] = "Replayer"
            };

            if (configOverrides != null)
            {
                configValues = configOverrides.Concat(configValues.Where(entry => !configOverrides.Keys.Contains(entry.Key)))
                    .ToDictionary(x=>x.Key, x=>x.Value);
            }

            var configuration = ConfigGenerator.GetConfigFromDictionary(configValues);
            _config = new LogSharkConfiguration(clParameters, configuration, _loggerFactory);

            _expectedProcessingErrorsCount = expectedProcessingErrorsCount ?? new Dictionary<string, int>();
            _expectedResultsLocation = expectedResultsLocation;
        }

        public async Task<RunSummary> RunAndValidate(bool expectSuccess, int? expectedNumberOfRecordsWritten = null)
        {
            var runSummary = await Run();
            AssertOutputMatch(_expectedResultsLocation, $"./Output/{runSummary.RunId}");
            AssertOtherParameters(runSummary, expectSuccess, expectedNumberOfRecordsWritten);

            return runSummary;
        }

        private async Task<RunSummary> Run()
        {
            var runner = new LogSharkRunner(_config, new MetricsModule(null, null), _loggerFactory);
            var runSummary = await runner.Run();
            return runSummary;
        }

        private static readonly List<string> ExcludedExtensions = new List<string>{ ".dblog" };
        private static void AssertOutputMatch(string expectedFileLocation, string actualFileLocation)
        {
            if (expectedFileLocation == null) // For tests without output
            {
                Directory.Exists(actualFileLocation).Should().Be(false);
                return;
            }
            
            var actualFiles = Directory.GetFiles(actualFileLocation, "*", SearchOption.AllDirectories)
                .Select(f => new FileInfo(f))
                .Where(f => !ExcludedExtensions.Contains(f.Extension.ToLower()))
                .ToDictionary(fi => Path.GetRelativePath(actualFileLocation, fi.FullName));

            var expectedFiles = Directory.GetFiles(expectedFileLocation, "*", SearchOption.AllDirectories)
                .Select(f => (relativePath: Path.GetRelativePath(expectedFileLocation, f), fileInfo: new FileInfo(f)));

            foreach (var expectedFile in expectedFiles)
            {
                Assert.True(actualFiles.ContainsKey(expectedFile.relativePath), $"Actual output missing expected file {expectedFile.relativePath}");

                switch (expectedFile.fileInfo.Extension)
                {
                    case ".csv":
                        AssertTextFilesHaveSameDataWithoutOrder(expectedFile.fileInfo.FullName, actualFiles[expectedFile.relativePath].FullName);
                        break;
                    case ".hyper":
                        AssertHyperFilesHaveSameData(expectedFile.fileInfo, actualFiles[expectedFile.relativePath]);
                        break;
                }

                actualFiles.Remove(expectedFile.relativePath);
            }

            Assert.True(actualFiles.Count == 0, $"Actual output contained more files than expected output:\n {string.Join("\n", actualFiles.Keys)}");
        }

        private static void AssertTextFilesHaveSameDataWithoutOrder(string expectedPath, string actualPath)
        {
            var allExpectedLines = File.ReadAllLines(expectedPath);
            var allActualLines = File.ReadAllLines(actualPath);

            var expectedRelativePath = new Uri(expectedPath).GetRelativePathFromCurrentDirectory();

            // This check is needed to detect duplicate lines which will be combined when pushing them to set
            allActualLines.Length.Should().Be(allExpectedLines.Length, $"expected file {expectedRelativePath} contains that many lines");
            
            var expectedSet = allExpectedLines.ToHashSet(); // Turning to sets, so order of the items is not compared. We do not provide guarantee on the order of processing
            var actualSet = allActualLines.ToHashSet();
            
            // Using this weird pattern instead of .Should().BeEquivalentTo() because the latter takes forever in CI and times out eventually 
            var actualNotInExpected = actualSet.Except(expectedSet).ToList();
            actualNotInExpected.Should().BeEquivalentTo(Enumerable.Empty<string>(), $"expected file {expectedRelativePath} should match actual");

            var expectedNotInActual = expectedSet.Except(actualSet).ToList();
            expectedNotInActual.Should().BeEquivalentTo(Enumerable.Empty<string>(), $"expected file {expectedRelativePath} should match actual");
        }

        private static void AssertHyperFilesHaveSameData(FileInfo expected, FileInfo actual)
        {
            // todo: verify hyper files are equivalent for LogShark's purposes once we can do this
        }

        private void AssertOtherParameters(RunSummary runSummary, bool expectSuccess, int? expectedNumberOfRecordsWritten)
        {
            runSummary.ProcessingNotificationsCollector.ErrorCountByReporter.Should().BeEquivalentTo(_expectedProcessingErrorsCount);
            runSummary.IsSuccess.Should().Be(expectSuccess);

            if (expectSuccess && expectedNumberOfRecordsWritten.HasValue)
            {
                var writersStatistics = runSummary.ProcessLogSetResult.PluginsExecutionResults.GetWritersStatistics();
                writersStatistics.DataSets.Values.Sum(stat => stat.LinesPersisted).Should().Be(expectedNumberOfRecordsWritten);
            }
        }
    }
}
