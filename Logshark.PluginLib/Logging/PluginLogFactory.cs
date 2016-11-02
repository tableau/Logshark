using log4net;
using log4net.Core;
using log4net.Repository;
using log4net.Repository.Hierarchy;
using Logshark.PluginLib.Helpers;
using System;
using System.Reflection;

namespace Logshark.PluginLib.Logging
{
    /// <summary>
    /// Returns a Log object specific to the invoking plugin.
    /// </summary>
    public static class PluginLogFactory
    {
        public static ILog GetLogger(Assembly pluginAssembly, MethodBase classMethodBase)
        {
            string pluginName = pluginAssembly.GetName().Name;
            string className = classMethodBase.DeclaringType.FullName;

            return ConstructLogger(pluginName, className);
        }

        public static ILog GetLogger(Type classType)
        {
            string pluginName = classType.Assembly.GetName().Name;
            string className = classType.FullName;

            return ConstructLogger(pluginName, className);
        }

        private static ILog ConstructLogger(string pluginName, string className)
        {
            // Get the repository specific to this plugin, or create it if it doesn't exist.
            ILoggerRepository repository = LogRepositoryHelper.GetOrCreateRepository(pluginName);
            var hierarchy = (Hierarchy)repository;

            // Configure logger.
            string fileName = LogFileHelper.GetLogFileName(pluginName);
            var logger = hierarchy.LoggerFactory.CreateLogger(repository, className);
            logger.Additivity = false;
            logger.Hierarchy = hierarchy;
            logger.AddAppender(AppenderFactory.CreateConsoleAppender(pluginName));
            logger.AddAppender(AppenderFactory.CreateRollingFileAppender(pluginName, fileName));
            logger.Repository.Configured = true;
            logger.Level = Level.All;

            return new LogImpl(logger);
        }
    }
}