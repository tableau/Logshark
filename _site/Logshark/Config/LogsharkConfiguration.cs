using Logshark.Connections;
using System;
using System.Collections.Generic;

namespace Logshark.Config
{
    /// <summary>
    /// Encapsulates all runtime options for Logshark.
    /// </summary>
    public class LogsharkConfiguration
    {
        public MongoConnectionInfo MongoConnectionInfo { get; set; }
        public PostgresConnectionInfo PostgresConnectionInfo { get; set; }
        public TableauConnectionInfo TableauConnectionInfo { get; set; }
        public LogsharkLocalMongoOptions LocalMongoOptions { get; set; }
        public LogsharkTuningOptions TuningOptions { get; set; }
        public ISet<string> DefaultPlugins { get; set; }

        public LogsharkConfiguration(LogsharkConfig config)
        {
            MongoConnectionInfo = new MongoConnectionInfo(config.MongoConnection);
            PostgresConnectionInfo = new PostgresConnectionInfo(config.PostgresConnection);
            TableauConnectionInfo = new TableauConnectionInfo(config.TableauConnection);
            LocalMongoOptions = new LogsharkLocalMongoOptions(config.RunOptions.LocalMongo);
            TuningOptions = new LogsharkTuningOptions(config.RunOptions.Tuning);

            DefaultPlugins = new HashSet<string>();
            foreach (Plugin plugin in config.PluginOptions.DefaultPlugins)
            {
                DefaultPlugins.Add(plugin.Name);
            }
        }

        public override string ToString()
        {
            return String.Format("[MongoConnectionInfo='{0}', PostgresConnectionInfo='{1}', TableauConnectionInfo='{2}', LocalMongoOptions='{3}', TuningOptions='{4}']",
                                  MongoConnectionInfo, PostgresConnectionInfo, TableauConnectionInfo, LocalMongoOptions, TuningOptions);
        }
    }
}