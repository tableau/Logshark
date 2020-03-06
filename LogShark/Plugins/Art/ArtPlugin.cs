using System.Collections.Generic;
using LogShark.Containers;
using LogShark.Plugins.Art.Model;
using LogShark.Plugins.Shared;
using LogShark.Writers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace LogShark.Plugins.Art
{
    public class ArtPlugin : IPlugin
    {
        private static readonly DataSetInfo OutputInfo = new DataSetInfo("Art", "Art");
        private static readonly List<LogType> ConsumedLogTypesStatic = new List<LogType> { LogType.VizqlserverCpp };

        public IList<LogType> ConsumedLogTypes => ConsumedLogTypesStatic;
        public string Name => "Art";

        private IProcessingNotificationsCollector _processingNotificationsCollector;
        private IWriter<FlattenedArtEvent> _writer;
        private JsonSerializer _jsonSerializer;

        public void Configure(IWriterFactory writerFactory, IConfiguration pluginConfig, IProcessingNotificationsCollector processingNotificationsCollector, ILoggerFactory loggerFactory)
        {
            _processingNotificationsCollector = processingNotificationsCollector;
            _writer = writerFactory.GetWriter<FlattenedArtEvent>(OutputInfo);
            _jsonSerializer = JsonSerializer.Create();
        }

        public void ProcessLogLine(LogLine logLine, LogType logType)
        {
            if (!(logLine.LineContents is NativeJsonLogsBaseEvent baseEvent))
            {
                var errorMessage = $"Was not able to cast line contents as {nameof(NativeJsonLogsBaseEvent)}";
                _processingNotificationsCollector.ReportError(errorMessage, logLine, nameof(ArtPlugin));
                return;
            }

            if (baseEvent.ArtData == null)
            {
                return; // This is normal - only small subset of log lines has ART data
            }

            ArtData artData;
            try
            {
                artData = baseEvent.ArtData.ToObject<ArtData>(_jsonSerializer);
            }
            catch (JsonException ex)
            {
                _processingNotificationsCollector.ReportError(ex.Message, logLine, nameof(ArtPlugin));
                return;
            }

            var @event = new FlattenedArtEvent(artData, baseEvent, logLine);
            _writer.AddLine(@event);
        }

        public SinglePluginExecutionResults CompleteProcessing()
        {
            var writerStatistics = _writer.Close();
            return new SinglePluginExecutionResults(writerStatistics);
        }
        
        public void Dispose()
        {
            _writer?.Dispose();
        }
    }
}