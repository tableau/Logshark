using log4net;
using Logshark.ConfigSection;
using System.Configuration;
using System.Reflection;

namespace Logshark.RequestModel.Config
{
    /// <summary>
    /// Handles tasks related to reading the custom Logshark.config file.
    /// </summary>
    public static class LogsharkConfigReader
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Load Logshark.config and parse it to create an instance of LogsharkConfiguration.
        /// </summary>
        public static LogsharkConfiguration LoadConfiguration()
        {
            Log.Info("Loading Logshark user configuration..");

            // Open up & parse application config.
            try
            {
                var appConfig = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                var logsharkSection = appConfig.Sections["LogsharkConfig"];
                var config = (LogsharkConfig)logsharkSection;

                return new LogsharkConfiguration(config);
            }
            catch (ConfigurationErrorsException ex)
            {
                Log.FatalFormat("Error parsing Logshark.config: {0})", ex.Message);
                throw;
            }
        }
    }
}