using System;
using System.Collections.Generic;
using System.IO;
using LogShark.LogParser.Containers;

namespace LogShark.LogParser.LogReaders
{
    public class SimpleLinePerLineReader : ILogReader
    {
        private readonly Stream _stream;
        private readonly string _filePath;
        private readonly IProcessingNotificationsCollector _processingNotificationsCollector;

        public SimpleLinePerLineReader(Stream stream, string filePath, IProcessingNotificationsCollector processingNotificationsCollector)
        {
            _stream = stream;
            _filePath = filePath;
            _processingNotificationsCollector = processingNotificationsCollector;
        }

        public IEnumerable<ReadLogLineResult> ReadLines()
        {
            using (var reader = new StreamReader(_stream))
            {
                var lineNumber = 0;
                string line;
                while (!reader.EndOfStream)
                {
                    try
                    {
                        line = reader.ReadLine();
                    } catch (OutOfMemoryException ex)
                    {
                        line = null;
                        _processingNotificationsCollector.ReportError("Failed to read line: " + ex.Message, _filePath, lineNumber, nameof(SimpleLinePerLineReader));
                    }

                    ++lineNumber;
                    if (line != null)
                    {
                        yield return new ReadLogLineResult(lineNumber, line);
                    }
                }
            }
        }
    }
}