using System;
using System.IO;
using AwesomeAssertions;
using LogShark.HostExtensions;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace LogShark.Tests.HostExtensions
{
    public class ConfigurationLayeringTests : IDisposable
    {
        private readonly string _dir = Path.Combine(Path.GetTempPath(), "ConfigurationLayeringTests_" + Guid.NewGuid().ToString("N"));

        public ConfigurationLayeringTests()
        {
            Directory.CreateDirectory(_dir);
            File.WriteAllText(Path.Combine(_dir, "main.json"), "{ \"Shared\": { \"Key\": \"main\" }, \"MainOnly\": \"m\" }");
            File.WriteAllText(Path.Combine(_dir, "overlay.json"), "{ \"Shared\": { \"Key\": \"overlay\" }, \"OverlayOnly\": \"o\" }");
        }

        public void Dispose()
        {
            Directory.Delete(_dir, true);
        }

        [Fact]
        public void WithoutOverlayOnlyMainConfigIsLoaded()
        {
            var config = LogSharkConfigurationLoader.Build(_dir, "main.json", null);

            config["Shared:Key"].Should().Be("main");
            config["MainOnly"].Should().Be("m");
            config["OverlayOnly"].Should().BeNull();
        }

        [Fact]
        public void OverlayAddsKeysButMainConfigWinsOnConflict()
        {
            var config = LogSharkConfigurationLoader.Build(_dir, "main.json", Path.Combine(_dir, "overlay.json"));

            config["Shared:Key"].Should().Be("main");
            config["MainOnly"].Should().Be("m");
            config["OverlayOnly"].Should().Be("o");
        }

        [Fact]
        public void MissingOverlayFileFailsHardNamingTheFile()
        {
            var missing = Path.Combine(_dir, "nope.json");

            Action build = () => LogSharkConfigurationLoader.Build(_dir, "main.json", missing);

            build.Should().Throw<HostExtensionLoadException>().Where(e => e.Message.Contains(missing));
        }

        [Fact]
        public void RelativeOverlayResolvesFromBasePath()
        {
            var config = LogSharkConfigurationLoader.Build(_dir, "main.json", "overlay.json");

            config["OverlayOnly"].Should().Be("o");
        }

        [Fact]
        public void MissingMainConfigThrows()
        {
            Action build = () => LogSharkConfigurationLoader.Build(_dir, "nope.json", null);

            build.Should().Throw<FileNotFoundException>();
        }
    }
}
