using Logshark.Config;
using System;

namespace Logshark.RequestModel.Config
{
    /// <summary>
    /// Contains configuration information for various Logshark data retention options.
    /// </summary>
    public class LogsharkDataRetentionOptions
    {
        public int MaxRuns { get; protected set; }

        public LogsharkDataRetentionOptions(DataRetentionOptions dataRetentionOptions)
        {
            MaxRuns = dataRetentionOptions.MaxRuns;
        }

        public override string ToString()
        {
            return String.Format("MaxRuns:{0}", MaxRuns);
        }
    }
}