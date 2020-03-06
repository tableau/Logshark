using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using LogShark.LogParser.Containers;
using LogShark.Plugins.Postgres;

namespace LogShark.LogParser.LogReaders
{
    public class CsvLogReader<T> : ILogReader
    {
        private readonly IProcessingNotificationsCollector _processingNotificationsCollector;
        private readonly Stream _stream;
        private readonly string _filePath;

        public CsvLogReader(Stream stream, string filePath, IProcessingNotificationsCollector processingNotificationsCollector)
        {
            _stream = stream;
            _filePath = filePath;
            _processingNotificationsCollector = processingNotificationsCollector;
        }

        public IEnumerable<ReadLogLineResult> ReadLines()
        {
            using (var reader = new StreamReader(_stream))
            using (var csv = new CsvReader(reader, new Configuration() { BadDataFound = BadDataFound, MissingFieldFound = MissingFieldFound, HasHeaderRecord = false }))
            {
                var lineNumber = 1;
                while (csv.Read())
                {
                    ReadLogLineResult result = null;
                    try
                    {
                        result = new ReadLogLineResult(lineNumber, csv.GetRecord<T>());
                        lineNumber++;
                    }
                    catch(Exception ex) when (
                        ex is TypeConverterException ||
                        ex is ReaderException)
                    {
                        _processingNotificationsCollector?.ReportError($"Error reading CSV record.  {ex.Message}", _filePath, csv.Context.Row, nameof(CsvLogReader<T>));
                    }

                    if (result != null)
                    {
                        yield return result;
                    }
                }
            }
        }

        private void BadDataFound(ReadingContext context)
        {
            _processingNotificationsCollector?.ReportError("Error reading CSV record.", _filePath, context.Row, nameof(CsvLogReader<T>));
        }

        private void MissingFieldFound(string[] headers, int index, ReadingContext context)
        {
            _processingNotificationsCollector?.ReportError($"Error reading CSV record, missing field {index}.", _filePath, context.Row, nameof(CsvLogReader<T>));
        }
    }
}
