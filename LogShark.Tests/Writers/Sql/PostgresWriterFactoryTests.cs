using System.Collections.Generic;
using System.Reflection;
using LogShark.Tests.Plugins.Helpers;
using LogShark.Writers.Sql;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LogShark.Tests.Writers.Sql
{
    public class PostgresWriterFactoryTests
    {
        public class TestRunSummary
        {
            public string Name { get; set; }
        }

        [Fact]
        public void PortFromRawConnectionStringIsNotOverwrittenByDefault()
        {
            // Regression test: a `Port=` value supplied through a raw connection string used to get
            // silently clobbered back to the 5432 default unless the port was also embedded in the host.
            var parameters = new LogSharkCommandLineParameters
            {
                DatabaseConnectionString = "User ID=postgres;Password=secret;Host=mypostgresbox;Port=5434;Database=logshark;Pooling=true;",
                DatabaseUsername = "postgres",
                DatabasePassword = "secret"
            };

            var (host, port) = BuildHostAndPort(parameters, new Dictionary<string, string>());

            Assert.Equal("mypostgresbox", host);
            Assert.Equal("5434", port);
        }

        [Fact]
        public void PortEmbeddedInHostIsStillHonored()
        {
            var parameters = new LogSharkCommandLineParameters
            {
                DatabaseHost = "mypostgresbox:5434",
                DatabaseName = "logshark",
                DatabaseUsername = "postgres",
                DatabasePassword = "secret"
            };

            var (host, port) = BuildHostAndPort(parameters, new Dictionary<string, string>());

            Assert.Equal("mypostgresbox", host);
            Assert.Equal("5434", port);
        }

        [Fact]
        public void ExplicitPortParameterIsHonoredWhenHostHasNoEmbeddedPort()
        {
            var parameters = new LogSharkCommandLineParameters
            {
                DatabaseHost = "mypostgresbox",
                DatabasePort = "5434",
                DatabaseName = "logshark",
                DatabaseUsername = "postgres",
                DatabasePassword = "secret"
            };

            var (host, port) = BuildHostAndPort(parameters, new Dictionary<string, string>());

            Assert.Equal("mypostgresbox", host);
            Assert.Equal("5434", port);
        }

        [Fact]
        public void PortDefaultsTo5432WhenNotSpecifiedAnywhere()
        {
            var parameters = new LogSharkCommandLineParameters
            {
                DatabaseHost = "mypostgresbox",
                DatabaseName = "logshark",
                DatabaseUsername = "postgres",
                DatabasePassword = "secret"
            };

            var (host, port) = BuildHostAndPort(parameters, new Dictionary<string, string>());

            Assert.Equal("mypostgresbox", host);
            Assert.Equal("5432", port);
        }

        private static (string Host, string Port) BuildHostAndPort(LogSharkCommandLineParameters parameters, Dictionary<string, string> configOverrides)
        {
            var config = new LogSharkConfiguration(parameters, ConfigGenerator.GetConfigFromDictionary(configOverrides), new NullLoggerFactory());
            var factory = new PostgresWriterFactory<TestRunSummary>("runId", config, new NullLoggerFactory(), new TestRunSummary(), "id");

            var factoryType = typeof(PostgresWriterFactory<TestRunSummary>);
            var host = (string)factoryType.GetField("_dbHost", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(factory);
            var port = (string)factoryType.GetField("_dbPort", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(factory);
            return (host, port);
        }
    }
}
