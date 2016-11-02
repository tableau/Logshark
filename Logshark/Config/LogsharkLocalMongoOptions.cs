using System;

namespace Logshark.Config
{
    /// <summary>
    /// Contains configuration information for various Logshark Local MongoDB options.
    /// </summary>
    public class LogsharkLocalMongoOptions
    {
        public bool AlwaysUseLocalMongo { get; protected set; }

        public bool PurgeLocalMongoOnStartup { get; protected set; }

        public LogsharkLocalMongoOptions(LocalMongoOptions configLocalMongoOptions)
        {
            AlwaysUseLocalMongo = configLocalMongoOptions.UseAlways;
            PurgeLocalMongoOnStartup = configLocalMongoOptions.PurgeOnStartup;
        }

        public override string ToString()
        {
            return String.Format("AlwaysUseLocalMongo:{0}, PurgeLocalMongoOnStartup:{1}",
                                  AlwaysUseLocalMongo, PurgeLocalMongoOnStartup);
        }
    }
}