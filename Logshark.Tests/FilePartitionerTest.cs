using FluentAssertions;
using LogParsers.Base;
using Logshark.Core.Controller.Parsing.Preprocessing;
using Logshark.Tests.Helpers;
using NUnit.Framework;

namespace Logshark.Tests
{
    [TestFixture]
    internal class FilePartitionerTest
    {
        [Test]
        [TestCase("largeTextFile.txt")]
        public void PartitionFile(string logfileName)
        {
            long partitionSize = 1024 * 1024;

            var logPath = TestDataHelper.GetResourcePath(logfileName);
            string rootLogDirectory = TestDataHelper.GetDataDirectory();

            LogFileContext context = new LogFileContext(logPath, rootLogDirectory);

            FilePartitioner partitioner = new FilePartitioner(context, partitionSize);
            var partitions = partitioner.PartitionFile();

            partitions.Count.Should().Be(5);
        }
    }
}