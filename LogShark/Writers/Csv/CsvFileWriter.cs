using System.Collections.Generic;
using System.Globalization;
using System.IO;
using CsvHelper;
using CsvHelper.Configuration;
using LogShark.Containers;
using Microsoft.Extensions.Logging;

namespace LogShark.Writers.Csv
{
    public class CsvFileWriter<T> : BaseWriter<T>
    {
        private readonly string _filename;
        private readonly TextWriter _textWriter;
        private readonly CsvWriter _csvWriter;

        private const int BatchSize = 1000;
        private readonly List<object> _toWrite = new List<object>(BatchSize);

        public CsvFileWriter(DataSetInfo dataSetInfo, string filename, bool appending, ILogger logger)
        : base(dataSetInfo, logger, nameof(CsvFileWriter<T>))
        {
            _filename = filename;
            _textWriter = new StreamWriter(filename, appending);
            _csvWriter = new CsvWriter(_textWriter, new CsvHelper.Configuration.Configuration(CultureInfo.InvariantCulture));
            
            Logger.LogDebug("{writerType} created for {outputFileName}", nameof(CsvFileWriter<T>), filename);
        }

        protected override void InsertNonNullLineLogic(T objectToWrite)
        {
            _toWrite.Add(objectToWrite);
            if (_toWrite.Count == BatchSize)
            {
                Flush();
            }
        }

        protected override void CloseLogic()
        {
            Flush();
            _csvWriter.Flush();
        }

        public override void Dispose()
        {
            base.Dispose();

            _csvWriter?.Dispose();
            _textWriter?.Dispose();
        }
        
        private void Flush()
        {
            _csvWriter.WriteRecords(_toWrite);
            _toWrite.Clear();
        }
    }
}