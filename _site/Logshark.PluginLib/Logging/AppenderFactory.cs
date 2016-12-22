using log4net.Appender;
using log4net.Core;
using log4net.Filter;
using Logshark.PluginLib.Helpers;
using System;

namespace Logshark.PluginLib.Logging
{
    internal static class AppenderFactory
    {
        internal static IAppender CreateConsoleAppender(string name)
        {
            ConsoleAppender appender = new ConsoleAppender
            {
                Name = name + "ConsoleAppender",
                Layout = LogPatternHelper.GetConsolePatternLayout(),
            };

            // Filter to only allow INFO, ERROR and FATAL events to log to console.
            LevelRangeFilter filter = new LevelRangeFilter
            {
                LevelMin = Level.Info,
                LevelMax = Level.Fatal
            };
            appender.AddFilter(filter);

            appender.ActivateOptions();
            return appender;
        }

        internal static IAppender CreateRollingFileAppender(string name, string fileName)
        {
            RollingFileAppender appender = new RollingFileAppender
            {
                Name = name + "RollingFileAppender",
                File = fileName,
                AppendToFile = true,
                MaxSizeRollBackups = LoggingConstants.MaxFileRollBackups,
                RollingStyle = RollingFileAppender.RollingMode.Size,
                MaximumFileSize = String.Format("{0}MB", LoggingConstants.MaxFileSizeMb),
                Layout = LogPatternHelper.GetFilePatternLayout(),
                LockingModel = new FileAppender.MinimalLock(),
                StaticLogFileName = true
            };
            appender.ActivateOptions();
            return appender;
        }
    }
}