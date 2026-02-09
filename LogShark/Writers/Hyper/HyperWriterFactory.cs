using LogShark.Containers;
using LogShark.Shared;
using LogShark.Writers.Containers;
using LogShark.Writers.Shared;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using Tableau.HyperAPI;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace LogShark.Writers.Hyper
{
    public class HyperWriterFactory : IWriterFactory
    {
        private readonly LogSharkConfiguration _config;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger _logger;
        private readonly HyperProcess _server;
        private readonly string _outputDirectory;
        private readonly string _workbooksDirectory;
        private readonly IProcessingNotificationsCollector _processingNotificationsCollector;

        public HyperWriterFactory(
            string runId,
            LogSharkConfiguration config,
            ILoggerFactory loggerFactory,
            IProcessingNotificationsCollector processingNotificationsCollector = null)
        {
            _config = config;
            _processingNotificationsCollector = processingNotificationsCollector;

            Directory.CreateDirectory(_config.HyperLogDir);
            Directory.CreateDirectory(_config.TempDir);
            _server = new HyperProcess(Telemetry.DoNotSendUsageDataToTableau, null, new Dictionary<string, string> {
                { "log_dir", _config.HyperLogDir },
                { "hyper_temp_directory_override", _config.TempDir },
                { "external_stream_timeout",_config.ExternalStreamTimeout}
            });

            (_outputDirectory, _workbooksDirectory) = OutputDirInitializer.InitDirs(_config.OutputDir, runId, _config.AppendTo, "hyper", loggerFactory, _config.ThrowIfOutputDirectoryExists);
            _loggerFactory = loggerFactory;
            _logger = _loggerFactory.CreateLogger<HyperWriterFactory>();
        }

        public IWorkbookGenerator GetWorkbookGenerator()
        {
            return new HyperWorkbookGenerator(
                _config, 
                _loggerFactory.CreateLogger<HyperWorkbookGenerator>(), 
                _outputDirectory, 
                _workbooksDirectory);
        }

        public IWorkbookPublisher GetWorkbookPublisher(PublisherSettings publisherSettings)
        {
            return new WorkbookPublisher(_config, publisherSettings, _loggerFactory);
        }

        public IWriter<T> GetWriter<T>(DataSetInfo dataSetInfo)
        {
            _logger.LogDebug("Creating writer for {dataSetInfo}", dataSetInfo);
            return new HyperWriter<T>(dataSetInfo, _server.Endpoint, _outputDirectory, dataSetInfo.Name, _loggerFactory.CreateLogger<HyperWriter<T>>(), _config.ExternalStreamTimeout, _processingNotificationsCollector);
        }

        public void Dispose()
        {
            // Get rid of our server now that we are done with this factory
            _server.Dispose();
        }
    }
}
