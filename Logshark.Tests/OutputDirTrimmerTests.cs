using System;
using System.IO;
using System.Threading;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LogShark.Tests
{
    public class OutputDirTrimmerTests : InvariantCultureTestsBase
    {
        private const string TestOutputDir = "OutputDirTrimmerTest";
        private const int NumberOfFolders = 5;
        
        private readonly ILogger _logger = new NullLoggerFactory().CreateLogger(nameof(OutputDirTrimmer));

        public OutputDirTrimmerTests()
        {
            DeleteTestOutputDir();

            Directory.CreateDirectory(TestOutputDir);
            for (var i = 1; i <= NumberOfFolders; i++)
            {
                Directory.CreateDirectory(Path.Combine(TestOutputDir, $"Result{i}"));
                Thread.Sleep(200);
            }
        }
        
        public void Dispose()
        {
            DeleteTestOutputDir();
        }

        [Fact]
        public void NoMaxSet()
        {
            OutputDirTrimmer.TrimOldResults(TestOutputDir, 0, _logger);

            for (var i = 1; i < NumberOfFolders; i++)
            {
                Directory.Exists(Path.Combine(TestOutputDir, $"Result{i}")).Should().Be(true);
            }
        }
        
        [Fact]
        public void BelowMax()
        {
            OutputDirTrimmer.TrimOldResults(TestOutputDir, 10, _logger);

            for (var i = 1; i < NumberOfFolders; i++)
            {
                Directory.Exists(Path.Combine(TestOutputDir, $"Result{i}")).Should().Be(true);
            }
        }
        
        [Fact]
        public void TrimNone() // Actually one will be removed for the current run
        {
            OutputDirTrimmer.TrimOldResults(TestOutputDir, NumberOfFolders, _logger);

            Directory.Exists(Path.Combine(TestOutputDir, "Result1")).Should().Be(false);
            Directory.Exists(Path.Combine(TestOutputDir, "Result2")).Should().Be(true);
            Directory.Exists(Path.Combine(TestOutputDir, "Result3")).Should().Be(true);
            Directory.Exists(Path.Combine(TestOutputDir, "Result4")).Should().Be(true);
            Directory.Exists(Path.Combine(TestOutputDir, "Result5")).Should().Be(true);
        }
        
        [Fact]
        public void TrimOne() // Two will be removed, as we clean up extra stop for our current run
        {
            OutputDirTrimmer.TrimOldResults(TestOutputDir, NumberOfFolders - 1, _logger);

            Directory.Exists(Path.Combine(TestOutputDir, "Result1")).Should().Be(false);
            Directory.Exists(Path.Combine(TestOutputDir, "Result2")).Should().Be(false);
            Directory.Exists(Path.Combine(TestOutputDir, "Result3")).Should().Be(true);
            Directory.Exists(Path.Combine(TestOutputDir, "Result4")).Should().Be(true);
            Directory.Exists(Path.Combine(TestOutputDir, "Result5")).Should().Be(true);
        }

        [Fact]
        public void DirDoesNotExist()
        {
            const string dirWhichDoesNotExist = "ThereIsNoWayIShouldExist";
            Directory.Exists(dirWhichDoesNotExist).Should().Be(false);
            
            Action testAction = () => OutputDirTrimmer.TrimOldResults(dirWhichDoesNotExist, 10, _logger);
            
            testAction.Should().NotThrow();
        }

        private static void DeleteTestOutputDir()
        {
            Thread.Sleep(100); // Giving it a moment as sometimes folders are still locked when we arrive here
            
            if (Directory.Exists(TestOutputDir))
            {
                Directory.Delete(TestOutputDir, true);
            }
        }
    }
}