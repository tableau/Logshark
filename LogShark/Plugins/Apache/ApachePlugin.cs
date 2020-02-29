using LogShark.Containers;
using LogShark.Plugins.Shared;
using LogShark.Writers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace LogShark.Plugins.Apache
{
    public class ApachePlugin : IPlugin
    {
        private const bool DefaultIncludeGatewayHealthChecksValue = false;

        private static readonly DataSetInfo OutputInfo = new DataSetInfo("Apache", "ApacheRequests");
        private static readonly List<LogType> ConsumedLogTypesStatic = new List<LogType> { LogType.Apache };

        public IList<LogType> ConsumedLogTypes => ConsumedLogTypesStatic;
        public string Name => "Apache";

        private IWriter<ApacheEvent> _writer;
        private IProcessingNotificationsCollector _processingNotificationsCollector;
        private bool _includeGatewayHealthChecks;

        public void Configure(IWriterFactory writerFactory, IConfiguration pluginConfig, IProcessingNotificationsCollector processingNotificationsCollector, ILoggerFactory loggerFactory)
        {
            _writer = writerFactory.GetWriter<ApacheEvent>(OutputInfo);
            var section = pluginConfig?.GetSection("IncludeGatewayChecks");
            var sectionExists = section != null && section.Exists();
            if (!sectionExists)
            {
                var logger = loggerFactory.CreateLogger<ApachePlugin>();
                logger.LogWarning("{pluginName} config was missing from Plugins Configuration. Using default value of {defaultValue} for {missingParameterName} parameter", nameof(ApachePlugin), DefaultIncludeGatewayHealthChecksValue, "IncludeGatewayChecks");
            }

            _includeGatewayHealthChecks = sectionExists ? section.Get<bool>() : DefaultIncludeGatewayHealthChecksValue;
            _processingNotificationsCollector = processingNotificationsCollector;
        }

        public void ProcessLogLine(LogLine logLine, LogType logType)
        {
            var @event = ApacheEventParser.ParseEvent(logLine);

            if (@event == null)
            {
                _processingNotificationsCollector.ReportError("Failed to parse Apache event from log line", logLine, nameof(ApachePlugin));
                return;
            }

            if (_includeGatewayHealthChecks || !IsHealthCheckRequest(@event))
            {
                _writer.AddLine(@event);
            }
        }

        public SinglePluginExecutionResults CompleteProcessing()
        {
            var writerStatistics = _writer.Close();
            return new SinglePluginExecutionResults(writerStatistics);
        }

        public void Dispose()
        {
            _writer.Dispose();
        }

        private static bool IsHealthCheckRequest(ApacheEvent ev)
        {
            return ev?.RequestBody == "/favicon.ico";
        }
    }
}