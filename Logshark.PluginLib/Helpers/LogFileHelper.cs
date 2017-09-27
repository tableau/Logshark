using log4net.Appender;
using log4net.Repository;
using System;
using System.IO;

namespace Logshark.PluginLib.Helpers
{
    internal static class LogFileHelper
    {
        internal static string GetLogDirectory()
        {
            // Attempt to use the same root log directory as an existing file appender in the default repository.
            ILoggerRepository defaultRepository = log4net.LogManager.GetRepository();
            foreach (IAppender appender in defaultRepository.GetAppenders())
            {
                if (appender is FileAppender)
                {
                    FileAppender fileAppender = (FileAppender) appender;
                    return Path.GetDirectoryName(fileAppender.File);
                }
            }

            // No existing file appender found; default to a local location.
            return Path.Combine(AssemblyHelper.GetAssemblyDirectory(), "Logs");
        }

        internal static string GetLogFileName(string pluginName)
        {
            string logDirectory = GetLogDirectory();
            return String.Format(@"{0}{1}Plugin.{2}.log.txt", logDirectory, Path.DirectorySeparatorChar, pluginName);
        }
    }
}