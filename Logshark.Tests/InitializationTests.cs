using FluentAssertions;
using Logshark.Common.Helpers;
using Logshark.Core.Controller.Initialization;
using Logshark.Core.Controller.Initialization.Archive;
using Logshark.RequestModel;
using Logshark.Tests.Helpers;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace Logshark.Tests
{
    [TestFixture]
    internal class InitializationTests
    {
        [Test]
        [TestCase("EmptyLog.zip")]
        public void InitializeFile(string archiveName)
        {
            var target = new LogsharkRequestTarget(TestDataHelper.GetResourcePath(archiveName));
            var runId = "test_" + DateTime.Now.ToString("yyMMddHHmmssff");

            var config = ControllerEntityHelper.GetMockConfiguration();
            var initializationRequest = new RunInitializationRequest(target, runId, new HashSet<string> { "default" }, false, config.ArtifactProcessorOptions);

            var archiveRunInitializer = new ArchiveRunInitializer(config.ApplicationTempDirectory);
            var initializationResult = archiveRunInitializer.Initialize(initializationRequest);

            initializationResult.Target.Should().NotBeNull();
            PathHelper.IsDirectory(initializationResult.Target).Should().BeTrue();

            initializationResult.ArtifactProcessor.Should().NotBeNull();
            initializationResult.ArtifactProcessor.ArtifactType.Should().Be("Desktop", "Input is a fake Desktop logset");
            initializationResult.ArtifactProcessorVersion.Should().NotBeNull();

            initializationResult.LogsetHash.Should().NotBeNull();
        }
    }
}