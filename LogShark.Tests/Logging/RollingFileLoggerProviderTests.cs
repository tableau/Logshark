using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using AwesomeAssertions;
using LogShark.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Xunit;

namespace LogShark.Tests.Logging
{
    public class RollingFileLoggerProviderTests : InvariantCultureTestsBase, IDisposable
    {
        private const string TestDir = "RollingFileLoggerProviderTest";

        public RollingFileLoggerProviderTests()
        {
            DeleteTestDir();
        }

        public void Dispose()
        {
            DeleteTestDir();
        }

        [Fact]
        public void BaseFileNameIncludesPrefixAndLogsetName()
        {
            using var provider = CreateProvider(logSetLocation: @"C:\logs\SaturdayOutage.zip");
            provider.CreateLogger("TestCategory").LogInformation("hello");

            var files = Directory.GetFiles(TestDir);
            files.Should().ContainSingle();
            Path.GetFileName(files.Single()).Should().MatchRegex(@"^LogShark-\d{14}-SaturdayOutage\.log$");
        }

        [Fact]
        public void FallsBackToUnspecifiedWhenLogSetLocationIsBlank()
        {
            using var provider = CreateProvider(logSetLocation: null);
            provider.CreateLogger("TestCategory").LogInformation("hello");

            var files = Directory.GetFiles(TestDir);
            Path.GetFileName(files.Single()).Should().MatchRegex(@"^LogShark-\d{14}-unspecified\.log$");
        }

        [Fact]
        public void RollsOverToNumberedFileWhenSizeLimitExceeded()
        {
            using var provider = CreateProvider(fileSizeLimitBytes: 200, retainedFileCountLimit: 100);
            var logger = provider.CreateLogger("TestCategory");

            for (var i = 0; i < 50; i++)
            {
                logger.LogInformation("This is test log line number {i} with some padding to grow the file", i);
            }

            var fileNames = Directory.GetFiles(TestDir).Select(Path.GetFileName).OrderBy(name => name).ToList();

            fileNames.Should().Contain(name => name.EndsWith("_001.log"));
            fileNames.Count.Should().BeGreaterThan(1);
        }

        [Fact]
        public void RetainedFileCountLimitPrunesOldestRolledFiles()
        {
            using var provider = CreateProvider(fileSizeLimitBytes: 200, retainedFileCountLimit: 3);
            var logger = provider.CreateLogger("TestCategory");

            for (var i = 0; i < 100; i++)
            {
                logger.LogInformation("This is test log line number {i} with some padding to grow the file", i);
            }

            Directory.GetFiles(TestDir).Length.Should().Be(3);
        }

        [Fact]
        public void FiltersOutLogsBelowConfiguredMinimumLevel()
        {
            string logFile;
            using (var provider = CreateProvider(defaultLogLevel: "Warning"))
            {
                var logger = provider.CreateLogger("TestCategory");

                logger.LogInformation("this should not appear");
                logger.LogWarning("this should appear");

                logFile = Directory.GetFiles(TestDir).Single();
            }

            // Read only after disposing the provider: the writer holds the file open for writing, and
            // File.ReadAllText requests FileShare.Read, which cannot coexist with that write handle on Windows.
            var content = File.ReadAllText(logFile);
            content.Should().NotContain("this should not appear");
            content.Should().Contain("this should appear");
        }

        private static RollingFileLoggerProvider CreateProvider(
            string logSetLocation = "mylogset.zip",
            long fileSizeLimitBytes = 10 * 1024 * 1024,
            int retainedFileCountLimit = 5,
            string defaultLogLevel = "Debug")
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["Directory"] = TestDir,
                    ["FileNamePrefix"] = "LogShark",
                    ["FileSizeLimitBytes"] = fileSizeLimitBytes.ToString(),
                    ["RetainedFileCountLimit"] = retainedFileCountLimit.ToString(),
                    ["LogLevel:Default"] = defaultLogLevel,
                })
                .Build();

            return new RollingFileLoggerProvider(config, logSetLocation);
        }

        private static void DeleteTestDir()
        {
            Thread.Sleep(100); // Giving it a moment as sometimes folders are still locked when we arrive here

            if (Directory.Exists(TestDir))
            {
                Directory.Delete(TestDir, true);
            }
        }
    }
}
