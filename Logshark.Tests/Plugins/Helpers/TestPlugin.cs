using System;
using System.Collections.Generic;
using LogShark.Containers;
using LogShark.Plugins;
using LogShark.Shared;
using LogShark.Shared.LogReading.Containers;
using LogShark.Writers;
using LogShark.Writers.Containers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LogShark.Tests.Plugins.Helpers
{
    public class TestPlugin : IPlugin
    {
        public IList<LogType> ConsumedLogTypes { get; }
        public string Name { get; }

        public HashSet<LogLine> ReceivedLines { get; }

        public TestPlugin()
        {
            ReceivedLines = new HashSet<LogLine>();
        }

        public void Configure(IWriterFactory writerFactory, IConfiguration pluginConfig,
            IProcessingNotificationsCollector processingNotificationsCollector, ILoggerFactory loggerFactory)
        {
            throw new NotImplementedException();
        }

        public void ProcessLogLine(LogLine logLine, LogType logType)
        {
            ReceivedLines.Add(logLine);
        }

        public SinglePluginExecutionResults CompleteProcessing()
        {
            return new SinglePluginExecutionResults(new List<WriterLineCounts>());
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}