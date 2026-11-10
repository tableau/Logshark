using System.IO;
using AwesomeAssertions;
using LogShark.Metrics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LogShark.Tests.HostExtensions
{
    public class DefaultConfigTests
    {
        private static IConfigurationRoot LoadShippedConfig()
        {
            return new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile(Path.Combine("Config", "LogSharkConfig.json"), optional: false)
                .Build();
        }

        [Fact]
        public void ShippedConfigHasNoTelemetryOrArtifactUploadSections()
        {
            var config = LoadShippedConfig();

            config.GetSection("Telemetry").Exists().Should().BeFalse();
            config.GetSection("Star" + "fish").Exists().Should().BeFalse();
        }

        [Fact]
        public void ConfigurationWorksWithoutTelemetrySection()
        {
            var config = new LogSharkConfiguration(new LogSharkCommandLineParameters(), LoadShippedConfig(), new NullLoggerFactory());

            config.TelemetryLevel.Should().Be(TelemetryLevel.None);
            config.TelemetryEndpoint.Should().BeNull();
            config.OutputDir.Should().Be("Output");
            config.NumberOfErrorDetailsToKeep.Should().Be(25);
        }
    }
}
