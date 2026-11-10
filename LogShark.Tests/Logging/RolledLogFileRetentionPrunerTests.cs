using System;
using System.IO;
using System.Threading;
using AwesomeAssertions;
using LogShark.Logging;
using Xunit;

namespace LogShark.Tests.Logging
{
    public class RolledLogFileRetentionPrunerTests : InvariantCultureTestsBase, IDisposable
    {
        private const string TestDir = "RolledLogFileRetentionPrunerTest";
        private const string BaseFileName = "LogShark-20260101000000-mylogset";
        private const int NumberOfRolledFiles = 5;

        public RolledLogFileRetentionPrunerTests()
        {
            DeleteTestDir();

            Directory.CreateDirectory(TestDir);
            File.WriteAllText(Path.Combine(TestDir, $"{BaseFileName}.log"), "base file");
            Thread.Sleep(20);
            for (var i = 1; i <= NumberOfRolledFiles; i++)
            {
                File.WriteAllText(Path.Combine(TestDir, $"{BaseFileName}_{i:000}.log"), $"rolled file {i}");
                Thread.Sleep(20);
            }
        }

        public void Dispose()
        {
            DeleteTestDir();
        }

        [Fact]
        public void NoMaxSet()
        {
            RolledLogFileRetentionPruner.PruneOldRolledFiles(TestDir, BaseFileName, ".log", 0);

            File.Exists(Path.Combine(TestDir, $"{BaseFileName}.log")).Should().Be(true);
            for (var i = 1; i <= NumberOfRolledFiles; i++)
            {
                File.Exists(Path.Combine(TestDir, $"{BaseFileName}_{i:000}.log")).Should().Be(true);
            }
        }

        [Fact]
        public void BelowMax()
        {
            RolledLogFileRetentionPruner.PruneOldRolledFiles(TestDir, BaseFileName, ".log", 10);

            File.Exists(Path.Combine(TestDir, $"{BaseFileName}.log")).Should().Be(true);
            for (var i = 1; i <= NumberOfRolledFiles; i++)
            {
                File.Exists(Path.Combine(TestDir, $"{BaseFileName}_{i:000}.log")).Should().Be(true);
            }
        }

        [Fact]
        public void TrimsOldestFilesBeyondLimit()
        {
            RolledLogFileRetentionPruner.PruneOldRolledFiles(TestDir, BaseFileName, ".log", 3);

            // Base file and the two earliest rolled files are the oldest four - only the three newest survive
            File.Exists(Path.Combine(TestDir, $"{BaseFileName}.log")).Should().Be(false);
            File.Exists(Path.Combine(TestDir, $"{BaseFileName}_001.log")).Should().Be(false);
            File.Exists(Path.Combine(TestDir, $"{BaseFileName}_002.log")).Should().Be(false);
            File.Exists(Path.Combine(TestDir, $"{BaseFileName}_003.log")).Should().Be(true);
            File.Exists(Path.Combine(TestDir, $"{BaseFileName}_004.log")).Should().Be(true);
            File.Exists(Path.Combine(TestDir, $"{BaseFileName}_005.log")).Should().Be(true);
        }

        [Fact]
        public void DoesNotTouchFilesFromOtherRuns()
        {
            File.WriteAllText(Path.Combine(TestDir, "LogShark-20260101000000-someotherlogset.log"), "unrelated run");

            RolledLogFileRetentionPruner.PruneOldRolledFiles(TestDir, BaseFileName, ".log", 1);

            File.Exists(Path.Combine(TestDir, "LogShark-20260101000000-someotherlogset.log")).Should().Be(true);
        }

        [Fact]
        public void DirDoesNotExist()
        {
            const string dirWhichDoesNotExist = "ThereIsNoWayIShouldExist";
            Directory.Exists(dirWhichDoesNotExist).Should().Be(false);

            Action testAction = () => RolledLogFileRetentionPruner.PruneOldRolledFiles(dirWhichDoesNotExist, BaseFileName, ".log", 10);

            testAction.Should().NotThrow();
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
