using System;
using System.Collections.Generic;
using LogShark.Containers;
using LogShark.Writers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LogShark.Plugins
{
    public interface IPlugin : IDisposable
    {
        IList<LogType> ConsumedLogTypes { get; }
        string Name { get; }

        /// <summary>
        /// This method called once right after plugin was created. We need this because we cannot call non-default constructor with Reflection 
        /// </summary>
        void Configure(IWriterFactory writerFactory, IConfiguration pluginConfig, IProcessingNotificationsCollector processingNotificationsCollector, ILoggerFactory loggerFactory);
        
        /// <summary>
        /// This method called many times. One for each LogLine produced by ILogReader associated with LogType
        /// </summary>
        void ProcessLogLine(LogLine logLine, LogType logType);
        
        /// <summary>
        /// This method called once after all log files were processed but before Plugin is disposed.
        /// This is needed for plugins who collect and keep some information from the logs and should process it only when all logs are processed  
        /// </summary>
        SinglePluginExecutionResults CompleteProcessing();
    }
}