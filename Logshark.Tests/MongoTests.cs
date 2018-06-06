using FluentAssertions;
using Logshark.Core.Mongo;
using NUnit.Framework;

namespace Logshark.Tests
{
    [TestFixture]
    public class MongoTests
    {
        [Test]
        public void StartAndStopLocalMongo()
        {
            var mongoProcessManager = new LocalMongoProcessManager();
            mongoProcessManager.StartMongoProcess();
            mongoProcessManager.IsMongoRunning().Should().Be(true);

            mongoProcessManager.KillAllMongoProcesses();
            mongoProcessManager.IsMongoRunning().Should().Be(false);
        }
    }
}