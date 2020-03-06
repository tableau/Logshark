using System.Collections.Generic;
using LogShark.Containers;
using LogShark.Plugins.Shared;
using LogShark.Writers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LogShark.Plugins.VizqlDesktop
{
    public class VizqlDesktopPlugin : IPlugin
    {
        private const int DefaultMaxQueryLength = 10000;
        
        public IList<LogType> ConsumedLogTypes => new List<LogType>{ LogType.VizqlDesktop };
        public string Name => "VizqlDesktop";

        private VizqlDesktopEventProcessor _eventProcessor;
        private IProcessingNotificationsCollector _processingNotificationsCollector;
        
        public void Configure(IWriterFactory writerFactory, IConfiguration pluginConfig, IProcessingNotificationsCollector processingNotificationsCollector, ILoggerFactory loggerFactory)
        {
            var section = pluginConfig?.GetSection("MaxQueryLength");
            var sectionExists = section != null && section.Exists();
            var parsedMaxQueryLength = 0;
            var parsedSuccessfully = sectionExists && int.TryParse(section.Value, out parsedMaxQueryLength);
            if (!parsedSuccessfully)
            {
                var logger = loggerFactory.CreateLogger<VizqlDesktopPlugin>();
                logger.LogWarning("{pluginName} config was missing from Plugins Configuration or contained incorrect value. Using default value of {defaultValue} for {missingParameterName} parameter", nameof(VizqlDesktopPlugin), DefaultMaxQueryLength, "MaxQueryLength");
            }
            var maxQueryLength = parsedSuccessfully ? parsedMaxQueryLength : DefaultMaxQueryLength;

            _eventProcessor = new VizqlDesktopEventProcessor(writerFactory, maxQueryLength);
            _processingNotificationsCollector = processingNotificationsCollector;
        }

        public void ProcessLogLine(LogLine logLine, LogType logType)
        {
            if (!(logLine.LineContents is NativeJsonLogsBaseEvent baseEvent))
            {
                var errorMessage = $"Was not able to cast line contents as {nameof(NativeJsonLogsBaseEvent)}";
                _processingNotificationsCollector.ReportError(errorMessage, logLine, nameof(VizqlDesktopPlugin));
                return;
            }
            
            _eventProcessor.ProcessEvent(baseEvent, logLine);
        }

        public SinglePluginExecutionResults CompleteProcessing()
        {
            var writersLineCounts = _eventProcessor.CompleteProcessing();
            return new SinglePluginExecutionResults(writersLineCounts);
        }
        
        public void Dispose()
        {
            _eventProcessor.Dispose();
        }
    }
}