using Logshark.ConfigSection;
using Logshark.ConnectionModel.Mongo;
using Logshark.ConnectionModel.Postgres;
using Logshark.ConnectionModel.TableauServer;
using Optional;
using System;
using System.IO;
using System.Reflection;
using System.Security;

namespace Logshark.RequestModel.Config
{
    /// <summary>
    /// Encapsulates all runtime options for Logshark.
    /// </summary>
    public class LogsharkConfiguration
    {
        public MongoConnectionInfo MongoConnectionInfo { get; set; }
        public Option<PostgresConnectionInfo> PostgresConnectionInfo { get; }
        public TableauServerConnectionInfo TableauConnectionInfo { get; }
        public LogsharkDataRetentionOptions DataRetentionOptions { get; }
        public LogsharkLocalMongoOptions LocalMongoOptions { get; }
        public LogsharkTuningOptions TuningOptions { get; }
        public LogsharkArtifactProcessorOptions ArtifactProcessorOptions { get; }
        public string ApplicationTempDirectory { get; }
        public string ApplicationOutputDirectory { get; }

        public LogsharkConfiguration(LogsharkConfig config)
        {
            MongoConnectionInfo = new MongoConnectionInfo(config.MongoConnection);
            if (!string.IsNullOrWhiteSpace(config.PostgresConnection.Server.Server) && !config.PostgresConnection.Server.Server.Equals("unspecified", StringComparison.OrdinalIgnoreCase))
            {
                PostgresConnectionInfo = Option.Some(new PostgresConnectionInfo(config.PostgresConnection));
            }
            TableauConnectionInfo = new TableauServerConnectionInfo(config.TableauConnection);
            DataRetentionOptions = new LogsharkDataRetentionOptions(config.RunOptions.DataRetention);
            LocalMongoOptions = new LogsharkLocalMongoOptions(config.RunOptions.LocalMongo);
            TuningOptions = new LogsharkTuningOptions(config.RunOptions.Tuning);
            ArtifactProcessorOptions = new LogsharkArtifactProcessorOptions(config.ArtifactProcessorOptions);
            ApplicationTempDirectory = GetApplicationTempDirectory(config);
            ApplicationOutputDirectory = GetApplicationOutputDirectory();
        }

        public override string ToString()
        {
            var postgresConnectionInfo = PostgresConnectionInfo.Match(connection => connection.ToString(), () => "Unspecified");
            return $"[MongoConnectionInfo='{MongoConnectionInfo}', PostgresConnectionInfo='{postgresConnectionInfo}', " +
                   $"TableauServerConnectionInfo='{TableauConnectionInfo}', DataRetentionOptions='{DataRetentionOptions}', " +
                   $"LocalMongoOptions='{LocalMongoOptions}', TuningOptions='{TuningOptions}', " +
                   $"ArtifactProcessorOptions='{ArtifactProcessorOptions}', ApplicationTempDirectory='{ApplicationTempDirectory}']";
        }

        /// <summary>
        /// Retrieves an application temp directory from config or defaults to the current user's Windows temp folder
        /// </summary>
        /// <returns>Path to temp directory.</returns>
        private static string GetApplicationTempDirectory(LogsharkConfig config)
        {
            try
            {
                return string.IsNullOrWhiteSpace(config.RunOptions.TempFolder.Path)
                    ? Path.Combine(Path.GetTempPath(), "Logshark")
                    : Path.Combine(config.RunOptions.TempFolder.Path, "Logshark");
            }
            catch (SecurityException)
            {
                // Default to trying to use a subfolder in the Logshark application directory.
                return Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Temp");
            }
        }

        /// <summary>
        /// Retrieves an application output directory local to the current executing assembly.
        /// </summary>
        /// <returns>Absolute path to the directory where Logshark output should be stored.</returns>
        private static string GetApplicationOutputDirectory()
        {
            return Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Output");
        }
    }
}