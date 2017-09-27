using Logshark.ConfigSection;
using Logshark.ConnectionModel.Mongo;
using Logshark.ConnectionModel.Postgres;
using Logshark.ConnectionModel.TableauServer;
using System;

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

        public LogsharkConfiguration(LogsharkConfig config)
        {
            MongoConnectionInfo = new MongoConnectionInfo(config.MongoConnection);
            PostgresConnectionInfo = new PostgresConnectionInfo(config.PostgresConnection);
            TableauConnectionInfo = new TableauServerConnectionInfo(config.TableauConnection);
            LocalMongoOptions = new LogsharkLocalMongoOptions(config.RunOptions.LocalMongo);
            TuningOptions = new LogsharkTuningOptions(config.RunOptions.Tuning);
            ArtifactProcessorOptions = new LogsharkArtifactProcessorOptions(config.ArtifactProcessorOptions);
        }

        public override string ToString()
        {
            return String.Format("[MongoConnectionInfo='{0}', PostgresConnectionInfo='{1}', TableauServerConnectionInfo='{2}', LocalMongoOptions='{3}', TuningOptions='{4}', ArtifactProcessorOptions='{5}']",
                                  MongoConnectionInfo, PostgresConnectionInfo, TableauConnectionInfo, LocalMongoOptions, TuningOptions, ArtifactProcessorOptions);
        }
    }
}