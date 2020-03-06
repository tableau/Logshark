using System.IO;
using LogShark.Containers;
using LogShark.Writers.Containers;
using LogShark.Writers.Shared;
using Microsoft.Extensions.Logging;

namespace LogShark.Writers.Csv
{
    public class CsvWriterFactory : IWriterFactory
    {
        private readonly LogSharkConfiguration _config;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger _logger;
        private readonly string _outputDirectory;
        private readonly bool _appending;

        public CsvWriterFactory(string runId, LogSharkConfiguration config, ILoggerFactory loggerFactory)
        {
            _config = config;
            (_outputDirectory, _) = OutputDirInitializer.InitDirs(_config.OutputDir, runId, _config.AppendTo, "csv", loggerFactory, _config.ThrowIfOutputDirectoryExists);
            _appending = !string.IsNullOrWhiteSpace(_config.AppendTo);

            _loggerFactory = loggerFactory;
            _logger = _loggerFactory.CreateLogger<CsvWriterFactory>();
        }

        public IWriter<T> GetWriter<T>(DataSetInfo dataSetInfo)
        {
            _logger.LogDebug("Creating writer for {dataSetInfo}", dataSetInfo);
            var path = Path.Combine(_outputDirectory, $"{dataSetInfo.Group}_{dataSetInfo.Name}.csv");
            return new CsvFileWriter<T>(dataSetInfo, path, _appending, _loggerFactory.CreateLogger<CsvFileWriter<T>>());
        }

        public IWorkbookGenerator GetWorkbookGenerator()
        {
            return new CsvWorkbookGenerator(_loggerFactory.CreateLogger<CsvWorkbookGenerator>());
        }
         
        public IWorkbookPublisher GetWorkbookPublisher(PublisherSettings publisherSettings)
        {
            return new CsvBasedWorkbookPublisher(publisherSettings, _loggerFactory.CreateLogger<CsvBasedWorkbookPublisher>());
        }

        public void Dispose()
        {
            // Don't care, do nothing
        }
    }
}