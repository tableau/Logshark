using System;
using System.Collections.Generic;
using LogShark.Containers;
using LogShark.Shared;
using LogShark.Shared.LogReading.Containers;
using LogShark.Writers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LogShark.Plugins
{
    public interface IPlugin : IDisposable
    {
        IList<LogType> ConsumedLogTypes { get; }
        string Name { get; }

        void Configure(IPluginWriterProvider writerProvider, IConfiguration pluginConfig, IProcessingNotificationsCollector processingNotificationsCollector, ILoggerFactory loggerFactory);

        void ProcessLogLine(LogLine logLine, LogType logType);

        SinglePluginExecutionResults CompleteProcessing();
    }
}
