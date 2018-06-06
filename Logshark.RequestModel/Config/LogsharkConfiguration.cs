using Logshark.ConfigSection;
using Logshark.ConnectionModel.Mongo;
using Logshark.ConnectionModel.Postgres;
using Logshark.ConnectionModel.TableauServer;
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
        public PostgresConnectionInfo PostgresConnectionInfo { get; set; }
        public TableauServerConnectionInfo TableauConnectionInfo { get; set; }
        public LogsharkLocalMongoOptions LocalMongoOptions { get; set; }
        public LogsharkTuningOptions TuningOptions { get; set; }
        public LogsharkArtifactProcessorOptions ArtifactProcessorOptions { get; set; }
        public string ApplicationTempDirectory { get; set; }

        public LogsharkConfiguration(LogsharkConfig config)
        {
            MongoConnectionInfo = new MongoConnectionInfo(config.MongoConnection);
            PostgresConnectionInfo = new PostgresConnectionInfo(config.PostgresConnection);
            TableauConnectionInfo = new TableauServerConnectionInfo(config.TableauConnection);
            LocalMongoOptions = new LogsharkLocalMongoOptions(config.RunOptions.LocalMongo);
            TuningOptions = new LogsharkTuningOptions(config.RunOptions.Tuning);
            ArtifactProcessorOptions = new LogsharkArtifactProcessorOptions(config.ArtifactProcessorOptions);
            ApplicationTempDirectory = GetApplicationTempDirectory();
        }

        public override string ToString()
        {
            return String.Format("[MongoConnectionInfo='{0}', PostgresConnectionInfo='{1}', TableauServerConnectionInfo='{2}', LocalMongoOptions='{3}', TuningOptions='{4}', ArtifactProcessorOptions='{5}', ApplicationTempDirectory='{6}]",
                                  MongoConnectionInfo, PostgresConnectionInfo, TableauConnectionInfo, LocalMongoOptions, TuningOptions, ArtifactProcessorOptions, ApplicationTempDirectory);
        }

        /// <summary>
        /// Retrieves an application temp directory local to the current executing assembly.
        /// </summary>
        /// <returns>Path to temp directory.</returns>
        protected string GetApplicationTempDirectory()
        {
            try
            {
                return Path.Combine(Path.GetTempPath(), "Logshark");
            }
            catch (SecurityException)
            {
                // Default to trying to use a subfolder in the Logshark application directory.
                return Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Temp");
            }
        }
    }
}